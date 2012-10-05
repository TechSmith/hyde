using System;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.Test
{
   [TestClass]
   public class DynamicTest
   {
      private TableStorageProvider _tableStorageProvider;
      private static readonly string _tableName = "DynamicTable";

      [TestInitialize]
      public void SetUp()
      {
         InMemoryTableStorageProvider.ResetAllTables();
         _tableStorageProvider = new InMemoryTableStorageProvider();
      }

      [TestMethod]
      public void Get_ObjectInsertedWithClassAndRetrievedViaDynamic_ShouldReturnFullyHydratedObject()
      {
         var simpleEntity = new DecoratedItem
         {
            Id = string.Format( "Dynamic{0}", DateTime.Now.Ticks ),
            Name = "Test",
            Age = 1
         };

         _tableStorageProvider.Add( _tableName, simpleEntity );

         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, simpleEntity.Id, simpleEntity.Name );

         Assert.AreEqual( simpleEntity.Age, result.Age );
         Assert.AreEqual( simpleEntity.Id, result.PartitionKey );
         Assert.AreEqual( simpleEntity.Name, result.RowKey );
      }

      [TestMethod]
      public void GetCollection_ObjectInsertedWithClassAndRetrievedViaDynamic_ShouldReturnFullyHydratedObjects()
      {
         string partitionKey = "Test";
         Enumerable.Range( 0, 10 ).ToList().ForEach( i =>
         {
            var simpleEntity = new DecoratedItem
            {
               Id = partitionKey,
               Name = string.Format( "Dynamic{0}_{1}", DateTime.Now.Ticks, i ),
               Age = 1
            };

            _tableStorageProvider.Add( _tableName, simpleEntity );

            _tableStorageProvider.Save();
         } );

         var result = _tableStorageProvider.GetCollection( _tableName, partitionKey );

         Assert.AreEqual( 10, result.Count() );
         Assert.AreEqual( 1, result.First().Age );
      }

      [TestMethod]
      public void Get_AddAndGetDynamic_DynamicIsReturnedWithAllProperties()
      {
         dynamic dyn = new ExpandoObject();

         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.AddDynamic( _tableName, dyn, "pk", "rk" );

         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, "pk", "rk" );

         Assert.AreEqual( "this is the first item.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod]
      public void UpsertDynamic_TheDynamicWasntInsertedYet_DynamicIsReturnedWithAllProperties()
      {
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.UpsertDynamic( _tableName, dyn, "pk", "rk" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, "pk", "rk" );
         Assert.AreEqual( "this is the first item.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod]
      public void UpsertDynamic_TheDynamicAlreadyInsertedAndNeedsToBeUpdated_DynamicIsReturnedWithAllProperties()
      {
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.AddDynamic( _tableName, dyn, "pk", "rk" );
         _tableStorageProvider.Save();
         dyn.FirstItem = "this text is changed.";
         _tableStorageProvider.UpsertDynamic( _tableName, dyn, "pk", "rk" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, "pk", "rk" );
         Assert.AreEqual( "this text is changed.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod]
      public void UpdateDynamic_TheDynamicAlreadyInsertedAndNeedsToBeUpdated_DynamicIsReturnedWithAllProperties()
      {
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.AddDynamic( _tableName, dyn, "pk", "rk" );
         _tableStorageProvider.Save();
         dyn.FirstItem = "this text is changed.";
         _tableStorageProvider.UpdateDynamic( _tableName, dyn, "pk", "rk" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, "pk", "rk" );
         Assert.AreEqual( "this text is changed.", result.FirstItem );
         Assert.AreEqual( 2, result.SecondItem );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void UpdateDynamic_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         dynamic dyn = new ExpandoObject();
         dyn.FirstItem = "this is the first item.";
         dyn.SecondItem = 2;

         _tableStorageProvider.UpdateDynamic( _tableName, dyn, "pk", "rk" );

         Assert.Fail( "Should have thrown EntityDoesNotExistException" );
      }
   }
}
