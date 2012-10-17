using System;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.StorageClient;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.IntegrationTest
{
   [TestClass]
   public class DynamicTest
   {
      readonly ICloudStorageAccount _storageAccount = new ConnectionStringCloudStorageAccount( ConfigurationManager.AppSettings["storageConnectionString"] );
      private static readonly string _baseTableName = "DynamicIntegrationTest";

      private TableStorageProvider _tableStorageProvider;
      private string _tableName;

      [TestInitialize]
      public void TestInitialize()
      {
         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );

         _tableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );

         var client = new CloudTableClient( _storageAccount.TableEndpoint,
                                         _storageAccount.Credentials );
         client.CreateTable( _tableName );
      }

      [ClassCleanup]
      public static void ClassCleanup()
      {
         var storageAccountProvider = new ConnectionStringCloudStorageAccount( ConfigurationManager.AppSettings["storageConnectionString"] );

         var client = new CloudTableClient( storageAccountProvider.TableEndpoint,
                                         storageAccountProvider.Credentials );

         var orphanedTables = client.ListTables( _baseTableName );
         foreach ( var orphanedTableName in orphanedTables )
         {
            client.DeleteTableIfExist( orphanedTableName );
         }
      }

      [TestMethod, TestCategory( "Integration" )]
      public void Get_ObjectInsertedIsInheritsDynamicObject_RetrievedProperly()
      {
         dynamic item = new DynamicPropertyBag();
         item.Foo = "test";
         item.Bar = 1;

         string partitionKey = "partitionKey";
         string rowKey = "rowKey";
         _tableStorageProvider.Add( _tableName, item, partitionKey, rowKey );
         _tableStorageProvider.Save();

         dynamic result = _tableStorageProvider.Get( _tableName, partitionKey, rowKey );

         Assert.AreEqual( item.Foo, result.Foo );
         Assert.AreEqual( item.Bar, result.Bar );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void Get_RetrievingObjectViaDynamic_ShouldHydrateEntityWithAllProperties()
      {
         var simpleEntity = new DecoratedItem
         {
            Id = string.Format( "Dynamic{0}", DateTime.Now.Ticks ),
            Name = "Test",
            Age = 1
         };

         _tableStorageProvider.Add( _tableName, simpleEntity );
         _tableStorageProvider.Save();

         var retrievedObject = _tableStorageProvider.Get( _tableName, simpleEntity.Id, simpleEntity.Name );

         Assert.AreEqual( simpleEntity.Age, (int) retrievedObject.Age );
         Assert.AreEqual( simpleEntity.Id, retrievedObject.PartitionKey );
         Assert.AreEqual( simpleEntity.Name, retrievedObject.RowKey );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void GetRange_RetrievingObjectViaDynamic_ShouldHydrateEntitiesWithAllProperties()
      {
         Enumerable.Range( 0, 10 ).ToList().ForEach( i =>
         {
            var simpleEntity = new DecoratedItem
            {
               Id = string.Format( "Dynamic_{0}", i ),
               Name = "Test",
               Age = 1
            };

            _tableStorageProvider.Add( _tableName, simpleEntity );
            _tableStorageProvider.Save();
         } );

         var result = _tableStorageProvider.GetRangeByPartitionKey( _tableName, "Dynamic_2", "Dynamic_6" );

         Assert.AreEqual( 5, result.Count() );
         Assert.AreEqual( 1, (int) result.First().Age );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void Get_AddAndGetDynamic_DynamicIsReturnedWithAllProperties()
      {
         dynamic dyn = new ExpandoObject();

         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.Add( _tableName, dyn, "pk", "rk" );

         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, "pk", "rk" );

         Assert.AreEqual( "this is the first item.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void UpsertDynamic_ItemDoesNotExist_DynamicIsInserted()
      {
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.Upsert( _tableName, dyn, "pk", "rk" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, "pk", "rk" );
         Assert.AreEqual( "this is the first item.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void UpsertDynamic_ItemExistsAndNeedsToBeUpdated_DynamicIsUpdated()
      {
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.Add( _tableName, dyn, "pk", "rk" );
         _tableStorageProvider.Save();
         dyn.FirstItem = "this text is changed.";
         _tableStorageProvider.Upsert( _tableName, dyn, "pk", "rk" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, "pk", "rk" );
         Assert.AreEqual( "this text is changed.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }
   }
}
