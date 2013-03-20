using System;
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
      public void CreateQuery_ItemAddedButSaveNotCalled_EntityNotReturnedByQuery()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem { Id = "abc", Name = "123", Age = 50 } ) );
         Assert.IsNull( _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault() );
      }

      [TestMethod]
      public void Save_ItemAdded_QueryReturnsTheItem()
      {
         var item = new DecoratedItem { Id = "abc", Name = "123", Age = 50 };
         var entity = TableItem.Create( item );
         _context.AddNewItem( "table", entity );
         _context.Save( Execute.Individually );

         var returnedItem = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( item.Id, returnedItem.Id );
         Assert.AreEqual( item.Name, returnedItem.Name );
         Assert.AreEqual( item.Age, returnedItem.Age );
      }

      [TestMethod]
      public void CreateQuery_ItemAddedAndSavedWithDifferentContext_ReturnsItem()
      {
         var addedItem = new DecoratedItem { Id = "abc", Name = "123", Age = 50 };
         var entity = TableItem.Create( addedItem );
         _context.AddNewItem( "table", entity );
         _context.Save( Execute.Individually );

         var returnedItem = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( addedItem.Id, returnedItem.Id );
         Assert.AreEqual( addedItem.Name, returnedItem.Name );
         Assert.AreEqual( addedItem.Age, returnedItem.Age );
      }

      [TestMethod]
      public void CreateQuery_ItemAddedToDifferentTable_QueryDoesNotReturnEntity()
      {
         var addedItem = new DecoratedItem { Id = "abc", Name = "123", Age = 50 };
         var entity = TableItem.Create( addedItem );
         _context.AddNewItem( "table", entity );
         _context.Save( Execute.Individually );

         var returnedItem = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNotNull( returnedItem );
      }

      [TestMethod]
      public void AddNewItem_ItemAlreadyExistsAndSaveIsNotCalled_NoExceptionThrown()
      {
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123" } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123" } ) );
      }

      [TestMethod]
      public void AddNewItem_ItemAlreadyExistsAndSaveIsCalled_ThrowsEntityAlreadyExistsException()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem { Id = "abc", Name = "123" } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table",TableItem.Create( new DecoratedItem { Id = "abc", Name = "123" } ) );
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
         _context.Update( "table",TableItem.Create( new DecoratedItem { Id = "abc", Name = "123", Age = 12 } ) );

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
      public void QueryOnPartitionKey_NoItems_ReturnsEmptyEnumermation()
      {
         Assert.AreEqual( 0, _context.CreateQuery<DecoratedItem>( "table").PartitionKeyEquals( "empty partition" ).Count() );
      }

      [TestMethod]
      public void QueryOnPartitionKey_ItemsInMultiplePartitions_ItemsInSpecifiedPartitionReturned()
      {
         var items = new[]
                     {
                        new DecoratedItem { Id = "abc", Name = "123", Age = 42 },
                        new DecoratedItem { Id = "abc", Name = "456", Age = 43 },
                        new DecoratedItem { Id = "bcd", Name = "456", Age = 44 },
                     };
         foreach ( var item in items )
         {
            _context.AddNewItem( "table", TableItem.Create( item ) );
         }
         _context.Save( Execute.Individually );

         var result = _context.CreateQuery<DecoratedItem>( "table").PartitionKeyEquals( "abc" ).ToList();
         Assert.AreEqual( 2, result.Count );
         Assert.AreEqual( 1, result.Count( i => i.Name == items[0].Name && i.Id == items[0].Id && i.Age == items[0].Age ) );
         Assert.AreEqual( 1, result.Count( i => i.Name == items[1].Name && i.Id == items[1].Id && i.Age == items[1].Age ) );
      }

      [TestMethod]
      public void QueryByPartitionKeyAndRowKeyRange_ItemsInRange_ReturnsItems()
      {
         var items = new[]
                     {
                        new DecoratedItem { Id = "abc", Name = "123", Age = 42 },
                        new DecoratedItem { Id = "abc", Name = "456", Age = 43 },
                        new DecoratedItem { Id = "bcd", Name = "456", Age = 44 },
                     };
         foreach ( var item in items )
         {
            _context.AddNewItem( "table", TableItem.Create( item ) );
         }
         _context.Save( Execute.Individually );

         var tsp = new InMemoryTableStorageProvider();
         var result = _context.CreateQuery( "table" ).PartitionKeyEquals( "abc" )
                              .RowKeyFrom( tsp.MinimumKeyValue ).Inclusive()
                              .RowKeyTo( tsp.MaximumKeyValue ).Inclusive();

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      public void QueryByPartitionKeyRange_ItemsInRange_ReturnsItems()
      {
         var items = new[]
                     {
                        new DecoratedItem { Id = "abc", Name = "123", Age = 42 },
                        new DecoratedItem { Id = "abd", Name = "456", Age = 43 },
                        new DecoratedItem { Id = "bcd", Name = "556", Age = 44 },
                     };
         foreach ( var item in items )
         {
            _context.AddNewItem( "table", TableItem.Create( item ) );
         }
         _context.Save( Execute.Individually );

         var tsp = new InMemoryTableStorageProvider();
         var result = _context.CreateQuery( "table" )
                              .PartitionKeyFrom( tsp.MinimumKeyValue ).Inclusive()
                              .PartitionKeyTo( "bcc").Inclusive();

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      public void Upsert_EntityDoesNotExist_EntityCreatedOnSave()
      {
         _context.Upsert( "table",TableItem.Create( new DecoratedItem { Id = "abc", Name = "123", Age = 42 } ) );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( 42, item.Age );
      }

      [TestMethod]
      public void Upsert_EntityExists_EntityUpdatedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 42 } ) );
         _context.Save( Execute.Individually );

         _context.Upsert( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 36 } ) );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( 36, item.Age );
      }

      [TestMethod]
      public void Delete_EntityExists_EntityDeletedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 42 } ) );
         _context.Save( Execute.Individually );

         _context.DeleteItem( "table", "abc", "123" );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
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
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 42 } ) );
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "789", Age = 42 } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "456", Age = 42 } ) );
         _context.Update( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 34 } ) ); 
         _context.DeleteItem( "table", "abc", "789" );

         try
         {
            _context.Save( Execute.Atomically );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         var items = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).ToList();
         Assert.AreEqual( 2, items.Count );
         Assert.AreEqual( 1, items.Count( i => i.Id == "abc" && i.Name == "123" && i.Age == 34 ) );
         Assert.AreEqual( 1, items.Count( i => i.Id == "abc" && i.Name == "456" && i.Age == 42 ) );
      }

      [TestMethod]
      public void SaveAtomically_ManyOperationsAndOneFails_NoOperationsPersist()
      {
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 42 } ) );
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "789", Age = 42 } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "456", Age = 42 } ) );
         _context.Update( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 34 } ) ); 
         _context.Update( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "not found", Age = 42 } ) ); // should fail
         _context.DeleteItem( "table", "abc", "789" );

         try
         {
            _context.Save( Execute.Atomically );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityDoesNotExistException )
         {
         }

         var items = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).ToList();
         Assert.AreEqual( 2, items.Count );
         Assert.AreEqual( 1, items.Count( i => i.Id == "abc" && i.Name == "123" && i.Age == 42 ) );
         Assert.AreEqual( 1, items.Count( i => i.Id == "abc" && i.Name == "789" && i.Age == 42 ) );
      }

      [TestMethod]
      public void SaveAtomically_TwoOperationsOnSameEntity_ThrowsInvalidOperationException()
      {
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 42 } ) );
         _context.Update( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "123", Age = 36 } ) );
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
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "foo" } ) );
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "bcd", Name = "foo" } ) );
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
            _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "foo" + i } ) );
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
         _context.AddNewItem( "table", TableItem.Create(new DecoratedItem { Id = "abc", Name = "foo" } ) );
         _context.AddNewItem( "table2", TableItem.Create(new DecoratedItem { Id = "abc", Name = "bar" } ) );

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
