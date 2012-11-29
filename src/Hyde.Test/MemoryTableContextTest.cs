using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table;
using TechSmith.Hyde.Table.Memory;

namespace TechSmith.Hyde.Test
{
   [TestClass]
   public class MemoryTableContextTest
   {
      private MemoryTableContext _context;

      [TestInitialize]
      public void Initialize()
      {
         MemoryTableContext.ResetAllTables();
         _context = new MemoryTableContext();
      }

      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void GetItem_ItemNotFound_ThrowsEntityNotFoundException()
      {
         _context.GetItem<DecoratedItem>( "foo", "pk", "rk" );
      }

      [TestMethod]
      public void GetItem_ItemAddedButSaveNotCalled_ThrowsEntityDoesNotExistException()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 50 }, "abc", "123" );
         try
         {
            _context.GetItem<DecoratedItem>( "table", "abc", "123" );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityDoesNotExistException )
         {
         }
      }

      [TestMethod]
      public void Save_ItemAdded_GetItemReturnsTheItem()
      {
         var addedItem = new DecoratedItem { Id = "abc", Name = "123", Age = 50 };
         _context.AddNewItem( "table", addedItem, "abc", "123" );
         _context.Save( Execute.Individually );

         var returnedItem = _context.GetItem<DecoratedItem>( "table", "abc", "123" );
         Assert.AreEqual( addedItem.Id, returnedItem.Id );
         Assert.AreEqual( addedItem.Name, returnedItem.Name );
         Assert.AreEqual( addedItem.Age, returnedItem.Age );
      }

      [TestMethod]
      public void GetItem_ItemAddedAndSavedWithDifferentContext_ReturnsItem()
      {
         var addedItem = new DecoratedItem { Id = "abc", Name = "123", Age = 50 };
         _context.AddNewItem( "table", addedItem, "abc", "123" );
         _context.Save( Execute.Individually );

         var returnedItem = new MemoryTableContext().GetItem<DecoratedItem>( "table", "abc", "123" );
         Assert.AreEqual( addedItem.Id, returnedItem.Id );
         Assert.AreEqual( addedItem.Name, returnedItem.Name );
         Assert.AreEqual( addedItem.Age, returnedItem.Age );
      }

      [TestMethod]
      public void GetItem_ItemAddedToDifferentTable_ThrowsEntityDoesNotExistException()
      {
         var addedItem = new DecoratedItem { Id = "abc", Name = "123", Age = 50 };
         _context.AddNewItem( "table", addedItem, "abc", "123" );
         _context.Save( Execute.Individually );

         try
         {
            _context.GetItem<DecoratedItem>( "diffTable", "abc", "123" );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityDoesNotExistException )
         {
         }
      }

      [TestMethod]
      public void AddNewItem_ItemAlreadyExistsAndSaveIsNotCalled_NoExceptionThrown()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123" }, "abc", "123" );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123" }, "abc", "123" );
      }

      [TestMethod]
      public void AddNewItem_ItemAlreadyExistsAndSaveIsCalled_ThrowsEntityAlreadyExistsException()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123" }, "abc", "123" );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123" }, "abc", "123" );
         try
         {
            _context.Save( Execute.Individually );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }
      }

      [TestMethod]
      public void Update_EntityDoesNotExist_ThrowsExceptionOnSave()
      {
         _context.Update( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 12 }, "abc", "123" );

         try
         {
            _context.Save( Execute.Individually );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityDoesNotExistException )
         {
         }
      }

      [TestMethod]
      public void GetCollectionWithPartitionKey_NoItems_ReturnsEmptyEnumermation()
      {
         Assert.AreEqual( 0, _context.GetCollection<DecoratedItem>( "table", "empty partition" ).Count() );
      }

      [TestMethod]
      public void GetCollectionWithPartitionKey_ItemsInMultiplePartitions_ItemsInSpecifiedPartitionReturned()
      {
         var items = new[]
                     {
                        new DecoratedItem { Id = "abc", Name = "123", Age = 42 },
                        new DecoratedItem { Id = "abc", Name = "456", Age = 43 },
                        new DecoratedItem { Id = "bcd", Name = "456", Age = 44 },
                     };
         foreach ( var item in items )
         {
            _context.AddNewItem( "table", item, item.Id, item.Name );
         }
         _context.Save( Execute.Individually );

         var result = _context.GetCollection<DecoratedItem>( "table", "abc" ).ToList();
         Assert.AreEqual( 2, result.Count );
         Assert.AreEqual( 1, result.Count( i => i.Name == items[0].Name && i.Id == items[0].Id && i.Age == items[0].Age ) );
         Assert.AreEqual( 1, result.Count( i => i.Name == items[1].Name && i.Id == items[1].Id && i.Age == items[1].Age ) );
      }

      [TestMethod]
      public void Upsert_EntityDoesNotExist_EntityCreatedOnSave()
      {
         _context.Upsert( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 42 }, "abc", "123" );
         _context.Save( Execute.Individually );

         var item = _context.GetItem<DecoratedItem>( "table", "abc", "123" );
         Assert.AreEqual( 42, item.Age );
      }

      [TestMethod]
      public void Upsert_EntityExists_EntityUpdatedOnSave()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 42 }, "abc", "123" );
         _context.Save( Execute.Individually );

         _context.Upsert( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 36 }, "abc", "123" );
         _context.Save( Execute.Individually );

         var item = _context.GetItem<DecoratedItem>( "table", "abc", "123" );
         Assert.AreEqual( 36, item.Age );
      }

      [TestMethod]
      public void Delete_EntityExists_EntityDeletedOnSave()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 42 }, "abc", "123" );
         _context.Save( Execute.Individually );

         _context.DeleteItem( "table", "abc", "123" );
         _context.Save( Execute.Individually );

         try
         {
            _context.GetItem<DecoratedItem>( "table", "abc", "123" );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityDoesNotExistException )
         {
         }
      }

      [TestMethod]
      public void Delete_EntityDoesNotExist_NoActionOnSave()
      {
         _context.DeleteItem( "table", "abc", "123" );
         _context.Save( Execute.Individually );
      }

      [TestMethod]
      public void SaveAtomically_ManyOperations_AllOperationsPersist()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 42 }, "abc", "123" );
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "789", Age = 42 }, "abc", "789" );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "456", Age = 42 }, "abc", "456" );
         _context.Update( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 34 }, "abc", "123" ); 
         _context.DeleteItem( "table", "abc", "789" );

         try
         {
            _context.Save( Execute.Atomically );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         var items = _context.GetCollection<DecoratedItem>( "table", "abc" ).ToList();
         Assert.AreEqual( 2, items.Count );
         Assert.AreEqual( 1, items.Count( i => i.Id == "abc" && i.Name == "123" && i.Age == 34 ) );
         Assert.AreEqual( 1, items.Count( i => i.Id == "abc" && i.Name == "456" && i.Age == 42 ) );
      }

      [TestMethod]
      public void SaveAtomically_ManyOperationsAndOneFails_NoOperationsPersist()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 42 }, "abc", "123" );
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "789", Age = 42 }, "abc", "789" );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "456", Age = 42 }, "abc", "456" );
         _context.Update( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 34 }, "abc", "123" ); 
         _context.Update( "table", new DecoratedItem { Id = "abc", Name = "not found", Age = 42 }, "abc", "not found" ); // should fail
         _context.DeleteItem( "table", "abc", "789" );

         try
         {
            _context.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         var items = _context.GetCollection<DecoratedItem>( "table", "abc" ).ToList();
         Assert.AreEqual( 2, items.Count );
         Assert.AreEqual( 1, items.Count( i => i.Id == "abc" && i.Name == "123" && i.Age == 42 ) );
         Assert.AreEqual( 1, items.Count( i => i.Id == "abc" && i.Name == "789" && i.Age == 42 ) );
      }

      [TestMethod]
      public void SaveAtomically_TwoOperationsOnSameEntity_ThrowsInvalidOperationException()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 42 }, "abc", "123" );
         _context.Update( "table", new DecoratedItem { Id = "abc", Name = "123", Age = 36 }, "abc", "123" );
         try
         {
            _context.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidOperationException )
         {
         }
      }

      [TestMethod]
      public void SaveAtomically_OperationsOnDifferentPartitions_ThrowsInvalidOperationException()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "foo" }, "abc", "foo" );
         _context.AddNewItem( "table", new DecoratedItem { Id = "bcd", Name = "foo" }, "bcd", "foo" );
         try
         {
            _context.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidOperationException )
         {
         }
      }

      [TestMethod]
      public void SaveAtomically_OperationsOnTooManyEntities_ThrowsInvalidOperationException()
      {
         for ( int i=0 ; i < 101; ++i )
         {
            _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "foo" + i }, "abc", "foo" + i );
         }

         try
         {
            _context.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidOperationException )
         {
         }
      }

      [TestMethod]
      public void SaveAtomically_OperationsOnDifferentTables_ThrowsInvalidOperationException()
      {
         _context.AddNewItem( "table", new DecoratedItem { Id = "abc", Name = "foo" }, "abc", "foo" );
         _context.AddNewItem( "table2", new DecoratedItem { Id = "abc", Name = "bar" }, "abc", "bar" );

         try
         {
            _context.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidOperationException )
         {
         }
      }
   }
}
