using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
         ICloudStorageAccount storageAccount = Configuration.GetTestStorageAccount();

         _tableStorageProvider = new AzureTableStorageProvider( storageAccount );

         _client = new CloudTableClient( new Uri( storageAccount.TableEndpoint ), storageAccount.Credentials );

         _tableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );

         var table = _client.GetTableReference( _tableName );
         table.CreateAsync().Wait();
      }

      [TestCleanup]
      public void TestCleanup()
      {
         var table = _client.GetTableReference( _tableName );
         table.DeleteAsync().Wait();
      }

      [ClassCleanup]
      public static void ClassCleanup()
      {
         var storageAccountProvider = Configuration.GetTestStorageAccount();

         var client = new CloudTableClient( new Uri( storageAccountProvider.TableEndpoint ), storageAccountProvider.Credentials );

         TableContinuationToken token = new TableContinuationToken();
         do
         {
            var orphanedTables = client.ListTablesSegmentedAsync( _baseTableName, token ).Result;
            token = orphanedTables.ContinuationToken;
            foreach ( CloudTable orphanedTableName in orphanedTables.Results )
            {
               client.GetTableReference( orphanedTableName.Name ).DeleteIfExistsAsync().Wait();
            }
         }
         while ( token != null );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_TooManyOperationsForEGT_ThrowsInvalidOperationException()
      {
         string partitionKey = "123";
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = partitionKey, Name = "200" } );
         await _tableStorageProvider.SaveAsync();

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
            await _tableStorageProvider.SaveAsync( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidOperationException )
         {
         }

         Assert.AreEqual( 1, ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( partitionKey ).Async() ).Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_OperationsInDifferentPartitions_ThrowsInvalidOperationException()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Ed" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "345", Name = "Eve" } );

         try
         {
            await _tableStorageProvider.SaveAsync( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidOperationException )
         {
         }
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_OperationsWithSamePartitionKeyInDifferentTables_ThrowsInvalidOperationException()
      {
         var newTableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );
         await _client.GetTableReference( newTableName ).CreateAsync();
         try
         {
            _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Ed" } );
            _tableStorageProvider.Add( newTableName, new DecoratedItem { Id = "123", Name = "Eve" } );

            try
            {
               await _tableStorageProvider.SaveAsync( Execute.Atomically );
               Assert.Fail( "Should have thrown exception" );
            }
            catch ( InvalidOperationException )
            {
            }
         }
         finally
         {
            await _client.GetTableReference( newTableName ).DeleteIfExistsAsync();
         }
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_MultipleOperationTypesOnSamePartitionAndNoConflicts_OperationsSucceed()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Eve", Age = 34 } );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Ed", Age = 7 } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItem { Id = "123", Name = "Eve", Age = 42 } );
         await _tableStorageProvider.SaveAsync( Execute.Atomically );

         Assert.AreEqual( 7, ( await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "Ed" ) ).Age );
         Assert.AreEqual( 42, ( await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "Eve" ) ).Age );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_TableStorageReturnsBadRequest_ThrowsInvalidOperationException()
      {
         // Inserting the same row twice in the same EGT causes Table Storage to return 400 Bad Request.
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc" } );

         await AsyncAssert.ThrowsAsync<InvalidOperationException>( () => _tableStorageProvider.SaveAsync( Execute.Atomically ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task Inserts_TwoRowsInPartitionAndOneAlreadyExists_NeitherRowInserted()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jake", Age = 34 } );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jane" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jake", Age = 42 } );

         try
         {
            await _tableStorageProvider.SaveAsync( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         try
         {
            await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "Jane" );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         Assert.AreEqual( 34, ( await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "Jake" ) ).Age );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_InsertingTwoRowsInPartitionAndOneAlreadyExists_NeitherRowInserted()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jake", Age = 34 } );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jane" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "Jake", Age = 42 } );

         try
         {
            await _tableStorageProvider.SaveAsync( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         try
         {
            await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "Jane" );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         Assert.AreEqual( 34, ( await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "Jake" ) ).Age );
      }
   }
}
