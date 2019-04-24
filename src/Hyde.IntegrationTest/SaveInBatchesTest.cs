using System;
using System.Collections.Generic;
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
   public class SaveInBatchesTest
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
      public async Task Insert_101EntitiesInTheSamePartition_ShouldSucceed()
      {
         string partitionKey = "123";
         int expectedCount = 101;
         for ( int i = 0; i < expectedCount; i++ )
         {
            var item = new DecoratedItem
                       {
                          Id = partitionKey,
                          Name = i.ToString( CultureInfo.InvariantCulture )
                       };
            _tableStorageProvider.Add( _tableName, item );
         }


         await _tableStorageProvider.SaveAsync( Execute.InBatches );


         IEnumerable<DecoratedItem> items = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( partitionKey ).Async() );
         Assert.AreEqual( expectedCount, items.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task Update_101EntitiesInTheSamePartition_ShouldSucceed()
      {
         string partitionKey = "123";
         int expectedCount = 101;
         for ( int i = 0; i < expectedCount; i++ )
         {
            var item = new DecoratedItem
                       {
                          Id = partitionKey,
                          Name = i.ToString( CultureInfo.InvariantCulture )
                       };
            _tableStorageProvider.Add( _tableName, item );
         }
         await _tableStorageProvider.SaveAsync( Execute.InBatches );

         for ( int i = 0; i < expectedCount; i++ )
         {
            var item = await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, partitionKey, i.ToString( CultureInfo.InvariantCulture ) );
            item.Age = 101;
            _tableStorageProvider.Update( _tableName, item );
         }


         await _tableStorageProvider.SaveAsync( Execute.InBatches );


         IEnumerable<DecoratedItem> items = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( partitionKey ).Async() ).ToList();
         Assert.AreEqual( expectedCount, items.Count() );
         Assert.IsFalse( items.Any( i => i.Age != 101 ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task Upsert_101EntitiesInTheSamePartition_ShouldSucceed()
      {
         string partitionKey = "123";
         int expectedCount = 101;
         for ( int i = 0; i < expectedCount; i++ )
         {
            var item = new DecoratedItem
            {
               Id = partitionKey,
               Name = i.ToString( CultureInfo.InvariantCulture )
            };
            _tableStorageProvider.Upsert( _tableName, item );
         }


         await _tableStorageProvider.SaveAsync( Execute.InBatches );


         IEnumerable<DecoratedItem> items = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( partitionKey ).Async() ).ToList();
         Assert.AreEqual( expectedCount, items.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task Delete_101EntitiesInTheSamePartition_ShouldSucceed()
      {
         string partitionKey = "123";
         int expectedCount = 101;
         for ( int i = 0; i < expectedCount; i++ )
         {
            var item = new DecoratedItem
            {
               Id = partitionKey,
               Name = i.ToString( CultureInfo.InvariantCulture )
            };
            _tableStorageProvider.Add( _tableName, item );
         }
         await _tableStorageProvider.SaveAsync( Execute.InBatches );

         for ( int i = 0; i < expectedCount; i++ )
         {
            _tableStorageProvider.Delete( _tableName, partitionKey, i.ToString( CultureInfo.InvariantCulture ) );
         }


         await _tableStorageProvider.SaveAsync( Execute.InBatches );


         IEnumerable<DecoratedItem> items = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( partitionKey ).Async() );
         Assert.IsFalse( items.Any() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_AllInsertsOnSamePartition_ShouldExecuteInEntityGroupTransaction()
      {
         // We can't tell directly whether an EGT is used, but we can infer it by setting up
         // an EGT to fail, and verifying that no actions were committed.
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "one" } );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "zero" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "one" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "two" } );
         try
         {
            await _tableStorageProvider.SaveAsync( Execute.InBatches );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         Assert.AreEqual( 1, ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( "123" ).Async() ).Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_SameRowInsertedTwice_InsertsDoneInSeparateTransactions()
      {
         // Inserting the same row twice in the same EGT causes Table Storage to return 400 Bad Request.
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc" } );

         try
         {
            await _tableStorageProvider.SaveAsync( Execute.InBatches );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         Assert.AreEqual( 1, ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( "123" ).Async() ).Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_MultipleOperationTypesOnDifferentRowsInSamePartition_OperationsShouldSucceed()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc" } );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "foo" } );
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 42 } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItem { Id = "123", Name = "bar" } );

         await _tableStorageProvider.SaveAsync( Execute.InBatches );

         var items = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( "123" ).Async() ).ToList();
         Assert.AreEqual( 3, items.Count );
         Assert.AreEqual( 1, items.Count( i => i.Name == "foo" ) );
         Assert.AreEqual( 1, items.Count( i => i.Name == "bar" ) );
         Assert.AreEqual( 1, items.Count( i => i.Name == "abc" && i.Age == 42 ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_MultipleOperationTypesOnDifferentRowsInSamePartition_OperationsExecutedInSameEGT()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 9 } );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "foo" } );
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 42 } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItem { Id = "123", Name = "bar" } );

         // fail the EGT
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "not found" } );

         try
         {
            await _tableStorageProvider.SaveAsync( Execute.InBatches );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         var items = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( "123" ).Async() ).ToList();
         Assert.AreEqual( 1, items.Count );
         Assert.AreEqual( 1, items.Count( i => i.Name == "abc" && i.Age == 9 ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_SameRowUpdatedTwice_UpdatesDoneInDifferentTransactions()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 30 } );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 40 } );
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 50 } );
         await _tableStorageProvider.SaveAsync( Execute.InBatches );

         Assert.AreEqual( 50, ( await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "abc" ) ).Age );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_BatchDeletesRowThatDoesNotExist_SilentlyFails()
      {
         _tableStorageProvider.Delete( _tableName, "not", "found" );
         await _tableStorageProvider.SaveAsync( Execute.InBatches );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_DeletesPerformedIndividually()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 30 } );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "bcd", Age = 80 } );

         // First delete should succeed
         _tableStorageProvider.Delete( _tableName, new DecoratedItem { Id = "123", Name = "abc" } );

         // Second delete should fail silently
         _tableStorageProvider.Delete( _tableName, new DecoratedItem { Id = "123", Name = "nope" } );

         // This update should fail
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "not found", Age = 90 } );

         try
         {
            await _tableStorageProvider.SaveAsync( Execute.InBatches );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         var items = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( "123" ).Async() ).ToList();

         Assert.AreEqual( 1, items.Count );
         Assert.AreEqual( "bcd", items[0].Name );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_SameRowAddedAndUpdated_OperationsDoneInDifferentTransactions()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 30 } );
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "abc", Age = 40 } );
         // next operation will be done in same transaction with previous update will fail. If previous operations were
         // correctly done in different transactions, the add will have completed.
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "not found", Age = 40 } );
         try
         {
            await _tableStorageProvider.SaveAsync( Execute.InBatches );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         Assert.IsNotNull( await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "abc" ) );
      }
   }
}
