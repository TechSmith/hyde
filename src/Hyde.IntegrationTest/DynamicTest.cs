using System;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.IntegrationTest
{
   [TestClass]
   public class DynamicTest
   {
      private readonly ICloudStorageAccount _storageAccount = Configuration.GetTestStorageAccount();
      private static readonly string _baseTableName = "DynamicIntegrationTest";

      private TableStorageProvider _tableStorageProvider;
      private string _tableName;

      [TestInitialize]
      public void TestInitialize()
      {
         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );

         _tableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );

         var client = new CloudTableClient( new Uri( _storageAccount.TableEndpoint ),
                                         _storageAccount.Credentials );
         client.GetTableReference( _tableName ).CreateAsync().Wait();
      }

      [ClassCleanup]
      public static void ClassCleanup()
      {
         var storageAccountProvider = Configuration.GetTestStorageAccount();

         var client = new CloudTableClient( new Uri( storageAccountProvider.TableEndpoint ),
                                         storageAccountProvider.Credentials );

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

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetAsync_ObjectInsertedIsInheritsDynamicObject_RetrievedProperly()
      {
         dynamic item = new DynamicPropertyBag();
         item.Foo = "test";
         item.Bar = 1;

         string partitionKey = "partitionKey";
         string rowKey = "rowKey";
         _tableStorageProvider.Add( _tableName, item, partitionKey, rowKey );
         await _tableStorageProvider.SaveAsync();

         dynamic result = await _tableStorageProvider.GetAsync( _tableName, partitionKey, rowKey );

         Assert.AreEqual( item.Foo, result.Foo );
         Assert.AreEqual( item.Bar, result.Bar );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetAsync_ObjectInsertedContainsDateTimeOutOfEdmRange_DateTimePropretyIsRetrievedDynamicallyAsDateTime()
      {
         var item = new DecoratedItemWithDateTime()
         {
            CreationDate = new DateTime( 1000, 1, 1, 1, 1, 1, 1, DateTimeKind.Local ),
            Id = "pk",
            Name = "rk",
         };

         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var retrievedObject = await _tableStorageProvider.GetAsync( _tableName, "pk", "rk" );
         Assert.AreEqual( new DateTime( 1000, 1, 1, 1, 1, 1, 1, DateTimeKind.Local ), retrievedObject.CreationDate.ToLocalTime() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetAsync_RetrievingObjectViaDynamic_ShouldHydrateEntityWithAllProperties()
      {
         var simpleEntity = new DecoratedItem
         {
            Id = string.Format( "Dynamic{0}", DateTime.Now.Ticks ),
            Name = "Test",
            Age = 1
         };

         _tableStorageProvider.Add( _tableName, simpleEntity );
         await _tableStorageProvider.SaveAsync();

         var retrievedObject = await _tableStorageProvider.GetAsync( _tableName, simpleEntity.Id, simpleEntity.Name );

         Assert.AreEqual( simpleEntity.Age, (int) retrievedObject.Age );
         Assert.AreEqual( simpleEntity.Id, retrievedObject.PartitionKey );
         Assert.AreEqual( simpleEntity.Name, retrievedObject.RowKey );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetRange_RetrievingObjectViaDynamic_ShouldHydrateEntitiesWithAllProperties()
      {
         foreach ( int i in Enumerable.Range( 0, 10 ) )
         {
            var simpleEntity = new DecoratedItem
            {
               Id = string.Format( "Dynamic_{0}", i ),
               Name = "Test",
               Age = 1
            };

            _tableStorageProvider.Add( _tableName, simpleEntity );
            await _tableStorageProvider.SaveAsync();
         }

         var result = await _tableStorageProvider.CreateQuery( _tableName ).PartitionKeyFrom( "Dynamic_2" ).Inclusive().PartitionKeyTo( "Dynamic_6" ).Inclusive().Async();

         Assert.AreEqual( 5, result.Count() );
         Assert.AreEqual( 1, (int) result.First().Age );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetAsync_AddAndGetDynamic_DynamicIsReturnedWithAllProperties()
      {
         dynamic dyn = new ExpandoObject();

         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.Add( _tableName, dyn, "pk", "rk" );

         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync( _tableName, "pk", "rk" );

         Assert.AreEqual( "this is the first item.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetAsync_ObjectDoesNotExist_ThrowsEntityDoesNotExistException()
      {
         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.GetAsync( _tableName, "not", "found" ) );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task UpsertDynamic_ItemDoesNotExist_DynamicIsInserted()
      {
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.Upsert( _tableName, dyn, "pk", "rk" );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync( _tableName, "pk", "rk" );
         Assert.AreEqual( "this is the first item.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task UpsertDynamic_ItemExistsAndNeedsToBeUpdated_DynamicIsUpdated()
      {
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.Add( _tableName, dyn, "pk", "rk" );
         await _tableStorageProvider.SaveAsync();
         dyn.FirstItem = "this text is changed.";
         _tableStorageProvider.Upsert( _tableName, dyn, "pk", "rk" );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync( _tableName, "pk", "rk" );
         Assert.AreEqual( "this text is changed.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void Add_ItemHasTimeStampPropertyAndThrowForReservedNamesIsOn_ExceptionIsThrown()
      {
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = true;
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.Timestamp = new DateTimeOffset( DateTime.UtcNow );

         Assert.ThrowsException<InvalidEntityException>( (Action) ( () => _tableStorageProvider.Add( _tableName, dyn, "pk", "rk" ) ) );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task Add_ItemHasTimeStampPropertyAndThrowForReservedNamesIsOff_PropertyIsIgnored()
      {
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         var manualTimestampValue = new DateTimeOffset( DateTime.UtcNow );
         dyn.Timestamp = manualTimestampValue;

         _tableStorageProvider.Add( _tableName, dyn, "pk", "rk" );
         await _tableStorageProvider.SaveAsync();


         var result = await _tableStorageProvider.GetAsync( _tableName, "pk", "rk" );
         Assert.IsTrue( result.Timestamp != manualTimestampValue, "Timestamp value should've been overwritten with accurate timestamp" );
      }
   }
}
