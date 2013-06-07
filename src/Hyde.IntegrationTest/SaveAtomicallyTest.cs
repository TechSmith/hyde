using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.IntegrationTest
{
   [TestClass]
   public class SaveAtomicallyTest
   {
      private static readonly string _baseTableName = "BatchTestTable";
      private AzureTableStorageProvider _tableStorageProvider;
      private CloudTableClient _client;
      private string _tableName;

      [TestInitialize]
      public void TestInitialize()
      {
         ICloudStorageAccount storageAccount = new ConnectionStringCloudStorageAccount( ConfigurationManager.AppSettings["storageConnectionString"] );

         _tableStorageProvider = new AzureTableStorageProvider( storageAccount );

         _client = new CloudTableClient( new Uri( storageAccount.TableEndpoint ), storageAccount.Credentials );

         _tableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );

         var table = _client.GetTableReference( _tableName );
         table.Create();
      }

      [TestCleanup]
      public void TestCleanup()
      {
         var table = _client.GetTableReference( _tableName );
         table.Delete();
      }

      [ClassCleanup]
      public static void ClassCleanup()
      {
         var storageAccountProvider = new ConnectionStringCloudStorageAccount( ConfigurationManager.AppSettings["storageConnectionString"] );

         var client = new CloudTableClient( new Uri( storageAccountProvider.TableEndpoint ), storageAccountProvider.Credentials );

         var orphanedTables = client.ListTables( _baseTableName );
         foreach ( var orphanedTableName in orphanedTables )
         {
            var table = client.GetTableReference( orphanedTableName.Name );
            table.DeleteIfExists();
         }
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void Save_TooManyOperationsForEGT_ThrowsInvalidOperationException()
      {
         string partitionKey = "123";
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = partitionKey, Name = "200" } );
         _tableStorageProvider.Save();

         int expectedCount = 100;
         for ( int i = 0; i < expectedCount; i++ )
         {
            var item = new DecoratedItem { Id = partitionKey, Name = i.ToString( CultureInfo.InvariantCulture ) };
            _tableStorageProvider.Add( _tableName, item );
         }
         // this next insert should fail, canceling the whole transaction
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = partitionKey, Name = "200" } );

         try
         {
            _tableStorageProvider.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidOperationException ex )
         {
         }

         Assert.AreEqual( 1, _tableStorageProvider.GetCollection<DecoratedItem>( _tableName, partitionKey ).Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void Save_OperationsInDifferentPartitions_ThrowsInvalidOperationException()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Ed" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "345", Name = "Eve" } );

         try
         {
            _tableStorageProvider.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidOperationException )
         {
         }
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void Save_OperationsWithSamePartitionKeyInDifferentTables_ThrowsInvalidOperationException()
      {
         var newTableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );
         _client.GetTableReference( newTableName ).Create();
         try
         {
            _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Ed" } );
            _tableStorageProvider.Add( newTableName, new DecoratedItem { Id = "123", Name = "Eve" } );

            try
            {
               _tableStorageProvider.Save( Execute.Atomically );
               Assert.Fail( "Should have thrown exception" );
            }
            catch ( InvalidOperationException )
            {
            }
         }
         finally
         {
            _client.GetTableReference( newTableName ).DeleteIfExists();
         }
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void Save_MultipleOperationTypesOnSamePartitionAndNoConflicts_OperationsSucceed()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Eve", Age = 34 } );
         _tableStorageProvider.Save();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Ed", Age = 7 } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItem { Id = "123", Name = "Eve", Age = 42 } );
         _tableStorageProvider.Save( Execute.Atomically );

         Assert.AreEqual( 7, _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "Ed" ).Age );
         Assert.AreEqual( 42, _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "Eve" ).Age );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void SaveAsync_MultipleOperationTypesOnSamePartitionAndNoConflicts_OperationsSucceed()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Eve", Age = 34 } );
         _tableStorageProvider.Save();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Ed", Age = 7 } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItem { Id = "123", Name = "Eve", Age = 42 } );
         var task = _tableStorageProvider.SaveAsync( Execute.Atomically );

         task.Wait();

         Assert.AreEqual( 7, _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "Ed" ).Age );
         Assert.AreEqual( 42, _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "Eve" ).Age );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      [ExpectedException( typeof( InvalidOperationException ) )]
      public void Save_TableStorageReturnsBadRequest_ThrowsInvalidOperationException()
      {
         // Inserting the same row twice in the same EGT causes Table Storage to return 400 Bad Request.
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc" } );

         _tableStorageProvider.Save( Execute.Atomically );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void Inserts_TwoRowsInPartitionAndOneAlreadyExists_NeitherRowInserted()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jake", Age = 34 } );
         _tableStorageProvider.Save();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jane" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jake", Age = 42 } );

         try
         {
            _tableStorageProvider.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         try
         {
            _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "Jane" );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         Assert.AreEqual( 34, _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "Jake" ).Age );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void SaveAsync_InsertingTwoRowsInPartitionAndOneAlreadyExists_NeitherRowInserted()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jake", Age = 34 } );
         _tableStorageProvider.Save();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jane" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jake", Age = 42 } );

         var task = _tableStorageProvider.SaveAsync( Execute.Atomically );
         try
         {
            task.Wait();
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( AggregateException e )
         {
            Assert.IsTrue( e.Flatten().InnerException is EntityAlreadyExistsException );
         }

         try
         {
            _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "Jane" );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         Assert.AreEqual( 34, _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "Jake" ).Age );
      }
   }
}
