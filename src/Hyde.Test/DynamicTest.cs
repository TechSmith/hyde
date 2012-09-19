using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
   }
}
