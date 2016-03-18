﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         } ) );
         Assert.IsNull( _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault() );
      }

      [TestMethod]
      public void Save_ItemAdded_QueryReturnsTheItem()
      {
         var item = new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         };
         var entity = TableItem.Create( item );
         _context.AddNewItem( "table", entity );
         _context.Save( Execute.Individually );

         var returnedItem = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( item.Id, returnedItem.Id );
         Assert.AreEqual( item.Name, returnedItem.Name );
         Assert.AreEqual( item.Age, returnedItem.Age );
      }

      [TestMethod]
      public void ResetAllTables_ContextCreatedBeforeCallingReset_ExistingContextStillWorks()
      {
         var localContext = new MemoryTableContext();
         MemoryTableContext.ResetAllTables();
         _context = new MemoryTableContext();

         var item = new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         };
         var entity = TableItem.Create( item );
         _context.AddNewItem( "table", entity );
         _context.Save( Execute.Individually );


         var returnedItem = localContext.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( item.Id, returnedItem.Id );
         Assert.AreEqual( item.Name, returnedItem.Name );
         Assert.AreEqual( item.Age, returnedItem.Age );
      }

      [TestMethod]
      public void CreateQuery_ItemAddedAndSavedWithDifferentContext_ReturnsItem()
      {
         var addedItem = new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         };
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
         var addedItem = new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         };
         var entity = TableItem.Create( addedItem );
         _context.AddNewItem( "table", entity );
         _context.Save( Execute.Individually );

         var returnedItem = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNotNull( returnedItem );
      }

      [TestMethod]
      public void AddNewItem_ItemAlreadyExistsAndSaveIsNotCalled_NoExceptionThrown()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123"
         } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123"
         } ) );
      }

      [TestMethod]
      public void AddNewItem_ItemAlreadyExistsAndSaveIsCalled_ThrowsEntityAlreadyExistsException()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123"
         } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123"
         } ) );
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
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 12
         } ), ConflictHandling.Throw );

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
      public void Update_EntityExists_EntityUpdatedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 36
         } ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( 36, item.Age );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityHasBeenChangedException ) )]
      public void Update_EntityHasAnOldETag_ThrowsEntityHasBeenChangedException()
      {
         var decoratedItemWithETag = new DecoratedItemWithETag
         {
            Age = 42,
            Id = "id",
            Name = "name"
         };
         _context.AddNewItem( "table", TableItem.Create( decoratedItemWithETag ) );
         _context.Save( Execute.Individually );

         var entity = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "id" ).RowKeyEquals( "name" ).Single();

         entity.Age = 19;
         _context.Update( "table", TableItem.Create( entity ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         entity.Age = 21;
         _context.Update( "table", TableItem.Create( entity ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         Assert.Fail( "Should have thrown an EntityHasBeenChangedException" );
      }

      [TestMethod]
      public void Update_EntityHasAnOldETagConflictHandlingOverwrite_EntityUpdatedOnSave()
      {
         var decoratedItemWithETag = new DecoratedItemWithETag
         {
            Age = 42,
            Id = "id",
            Name = "name"
         };
         _context.AddNewItem( "table", TableItem.Create( decoratedItemWithETag ) );
         _context.Save( Execute.Individually );

         var entity = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "id" ).RowKeyEquals( "name" ).Single();

         entity.Age = 19;
         _context.Update( "table", TableItem.Create( entity ), ConflictHandling.Overwrite );
         _context.Save( Execute.Individually );

         entity.Age = 21;
         _context.Update( "table", TableItem.Create( entity ), ConflictHandling.Overwrite );
         _context.Save( Execute.Individually );

         var retrievedEntity = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "id" ).RowKeyEquals( "name" ).Single();
         Assert.AreEqual( 21, retrievedEntity.Age );
      }

      [TestMethod]
      public void QueryForDynamic_RowContainsNullValues_ResultingDynamicHasNullPropertiesRemoved()
      {
         var item = new DecoratedItemWithNullableProperty()
         {
            Id = "abc",
            Name = "Hello"
         };
         _context.AddNewItem( "table", TableItem.Create( item ) );
         _context.Save( Execute.Individually );

         var result = _context.CreateQuery( "table", false ).PartitionKeyEquals( "abc" ).RowKeyEquals( "Hello" );
         var asDict = (IDictionary<string, object>) result.First();

         Assert.AreEqual( 3, asDict.Count() );
         Assert.IsTrue( asDict.ContainsKey( "PartitionKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "RowKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "Timestamp" ) );
         Assert.IsFalse( asDict.ContainsKey( "Description" ) );
         Assert.IsFalse( asDict.ContainsKey( "ETag" ) );
      }

      [TestMethod]
      public void QueryForDynamic_ShouldIncludeETag_ResultingDynamicHasETag()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar"
         };
         _context.AddNewItem( "table", TableItem.Create( item ) );
         _context.Save( Execute.Individually );

         var result = _context.CreateQuery( "table", true ).PartitionKeyEquals( "foo" ).RowKeyEquals( "bar" );
         var asDict = (IDictionary<string, object>) result.First();

         Assert.IsTrue( asDict.ContainsKey( "PartitionKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "RowKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "ETag" ) );
      }

      [TestMethod]
      public void QueryOnPartitionKey_NoItems_ReturnsEmptyEnumermation()
      {
         Assert.AreEqual( 0, _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "empty partition" ).Count() );
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

         var result = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).ToList();
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
         var result = _context.CreateQuery( "table", false ).PartitionKeyEquals( "abc" )
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
         var result = _context.CreateQuery( "table", false )
                              .PartitionKeyFrom( tsp.MinimumKeyValue ).Inclusive()
                              .PartitionKeyTo( "bcc" ).Inclusive();

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      public void QueryByPartitionKeyRangeWithTop_ItemsInRange_ReturnsTopItem()
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
         var result = _context.CreateQuery( "table", false )
                              .PartitionKeyFrom( tsp.MinimumKeyValue ).Inclusive()
                              .PartitionKeyTo( "bcc" ).Inclusive()
                              .Top( 1 );

         Assert.AreEqual( 1, result.Count() );
         string actualName = result.Single().RowKey;
         Assert.AreEqual( "123", actualName );
      }

      [TestMethod]
      public void QueryAsyncByPartitionKeyRangeWithTop_ItemsInRange_ReturnsTopItem()
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
         var result = _context.CreateQuery( "table", false )
                              .PartitionKeyFrom( tsp.MinimumKeyValue ).Inclusive()
                              .PartitionKeyTo( "bcc" ).Inclusive()
                              .Top( 1 )
                              .Async().Result;

         Assert.AreEqual( 1, result.Count() );
         string actualName = result.Single().RowKey;
         Assert.AreEqual( "123", actualName );
      }

      [TestMethod]
      public void Upsert_EntityDoesNotExist_EntityCreatedOnSave()
      {
         _context.Upsert( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( 42, item.Age );
      }

      [TestMethod]
      public void Upsert_EntityExists_EntityUpdatedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         _context.Upsert( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 36
         } ) );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( 36, item.Age );
      }

      [TestMethod]
      public void Delete_EntityExists_EntityDeletedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
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
      public void Delete_TableItemWithETag_EntityDeletedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItemWithETag
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var storedItem = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();

         _context.DeleteItem( "table", TableItem.Create( storedItem ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
      }

      [TestMethod]
      public void Delete_TableItemWithoutETag_EntityDeletedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var storedItem = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();

         _context.DeleteItem( "table", TableItem.Create( storedItem ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityHasBeenChangedException ) )]
      public void Delete_TableItemWithETagHasChanged_ThrowsEntityHasBeenDeletedException()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItemWithETag
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var storedItem = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();

         _context.Update( "table", TableItem.Create( storedItem ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         _context.DeleteItem( "table", TableItem.Create( storedItem ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
      }

      [TestMethod]
      public void Delete_TableItemWithETagHasChangedConflictHandlingOverwrite_DeletesEntity()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItemWithETag
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var storedItem = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();

         _context.Update( "table", TableItem.Create( storedItem ), ConflictHandling.Overwrite );
         _context.Save( Execute.Individually );

         _context.DeleteItem( "table", TableItem.Create( storedItem ), ConflictHandling.Overwrite );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
      }

      [TestMethod]
      public void SaveAtomically_ManyOperations_AllOperationsPersist()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "789",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "456",
            Age = 42
         } ) );
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 34
         } ), ConflictHandling.Throw );
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "789",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "456",
            Age = 42
         } ) );
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 34
         } ), ConflictHandling.Throw );
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "not found",
            Age = 42
         } ), ConflictHandling.Throw ); // should fail
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 36
         } ), ConflictHandling.Throw );
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "foo"
         } ) );
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "bcd",
            Name = "foo"
         } ) );
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
         for ( int i = 0; i < 101; ++i )
         {
            _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
            {
               Id = "abc",
               Name = "foo" + i
            } ) );
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "foo"
         } ) );
         _context.AddNewItem( "table2", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "bar"
         } ) );

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

   [TestClass]
   public class MemoryTableContextWithInstanceAccountTest
   {
      private MemoryTableContext _context;

      [TestInitialize]
      public void Initialize()
      {
         MemoryTableContext.ResetAllTables();
         _context = new MemoryTableContext( new MemoryStorageAccount() );
      }

      [TestMethod]
      public void CreateQuery_ItemAddedButSaveNotCalled_EntityNotReturnedByQuery()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         } ) );
         Assert.IsNull( _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault() );
      }

      [TestMethod]
      public void Save_ItemAdded_QueryReturnsTheItem()
      {
         var item = new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         };
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
         var addedItem = new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         };
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
         var addedItem = new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 50
         };
         var entity = TableItem.Create( addedItem );
         _context.AddNewItem( "table", entity );
         _context.Save( Execute.Individually );

         var returnedItem = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNotNull( returnedItem );
      }

      [TestMethod]
      public void AddNewItem_ItemAlreadyExistsAndSaveIsNotCalled_NoExceptionThrown()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123"
         } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123"
         } ) );
      }

      [TestMethod]
      public void AddNewItem_ItemAlreadyExistsAndSaveIsCalled_ThrowsEntityAlreadyExistsException()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123"
         } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123"
         } ) );
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
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 12
         } ), ConflictHandling.Throw );

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
      public void Update_EntityExists_EntityUpdatedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 36
         } ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( 36, item.Age );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityHasBeenChangedException ) )]
      public void Update_EntityHasAnOldETag_ThrowsEntityHasBeenChangedException()
      {
         var decoratedItemWithETag = new DecoratedItemWithETag
         {
            Age = 42,
            Id = "id",
            Name = "name"
         };
         _context.AddNewItem( "table", TableItem.Create( decoratedItemWithETag ) );
         _context.Save( Execute.Individually );

         var entity = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "id" ).RowKeyEquals( "name" ).Single();

         entity.Age = 19;
         _context.Update( "table", TableItem.Create( entity ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         entity.Age = 21;
         _context.Update( "table", TableItem.Create( entity ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         Assert.Fail( "Should have thrown an EntityHasBeenChangedException" );
      }

      [TestMethod]
      public void Update_EntityHasAnOldETagConflictHandlingOverwrite_EntityUpdatedOnSave()
      {
         var decoratedItemWithETag = new DecoratedItemWithETag
         {
            Age = 42,
            Id = "id",
            Name = "name"
         };
         _context.AddNewItem( "table", TableItem.Create( decoratedItemWithETag ) );
         _context.Save( Execute.Individually );

         var entity = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "id" ).RowKeyEquals( "name" ).Single();

         entity.Age = 19;
         _context.Update( "table", TableItem.Create( entity ), ConflictHandling.Overwrite );
         _context.Save( Execute.Individually );

         entity.Age = 21;
         _context.Update( "table", TableItem.Create( entity ), ConflictHandling.Overwrite );
         _context.Save( Execute.Individually );

         var retrievedEntity = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "id" ).RowKeyEquals( "name" ).Single();
         Assert.AreEqual( 21, retrievedEntity.Age );
      }

      [TestMethod]
      public void QueryForDynamic_RowContainsNullValues_ResultingDynamicHasNullPropertiesRemoved()
      {
         var item = new DecoratedItemWithNullableProperty()
         {
            Id = "abc",
            Name = "Hello"
         };
         _context.AddNewItem( "table", TableItem.Create( item ) );
         _context.Save( Execute.Individually );

         var result = _context.CreateQuery( "table", false ).PartitionKeyEquals( "abc" ).RowKeyEquals( "Hello" );
         var asDict = (IDictionary<string, object>) result.First();

         Assert.AreEqual( 3, asDict.Count() );
         Assert.IsTrue( asDict.ContainsKey( "PartitionKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "RowKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "Timestamp" ) );
         Assert.IsFalse( asDict.ContainsKey( "Description" ) );
         Assert.IsFalse( asDict.ContainsKey( "ETag" ) );
      }

      [TestMethod]
      public void QueryForDynamic_ShouldIncludeETag_ResultingDynamicHasETag()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar"
         };
         _context.AddNewItem( "table", TableItem.Create( item ) );
         _context.Save( Execute.Individually );

         var result = _context.CreateQuery( "table", true ).PartitionKeyEquals( "foo" ).RowKeyEquals( "bar" );
         var asDict = (IDictionary<string, object>) result.First();

         Assert.IsTrue( asDict.ContainsKey( "PartitionKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "RowKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "ETag" ) );
      }

      [TestMethod]
      public void QueryOnPartitionKey_NoItems_ReturnsEmptyEnumermation()
      {
         Assert.AreEqual( 0, _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "empty partition" ).Count() );
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

         var result = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).ToList();
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
         var result = _context.CreateQuery( "table", false ).PartitionKeyEquals( "abc" )
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
         var result = _context.CreateQuery( "table", false )
                              .PartitionKeyFrom( tsp.MinimumKeyValue ).Inclusive()
                              .PartitionKeyTo( "bcc" ).Inclusive();

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      public void Upsert_EntityDoesNotExist_EntityCreatedOnSave()
      {
         _context.Upsert( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( 42, item.Age );
      }

      [TestMethod]
      public void Upsert_EntityExists_EntityUpdatedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         _context.Upsert( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 36
         } ) );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();
         Assert.AreEqual( 36, item.Age );
      }

      [TestMethod]
      public void Delete_EntityExists_EntityDeletedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
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
      public void Delete_TableItemWithETag_EntityDeletedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItemWithETag
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var storedItem = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();

         _context.DeleteItem( "table", TableItem.Create( storedItem ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
      }

      [TestMethod]
      public void Delete_TableItemWithoutETag_EntityDeletedOnSave()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var storedItem = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();

         _context.DeleteItem( "table", TableItem.Create( storedItem ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItem>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityHasBeenChangedException ) )]
      public void Delete_TableItemWithETagHasChanged_ThrowsEntityHasBeenDeletedException()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItemWithETag
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var storedItem = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();

         _context.Update( "table", TableItem.Create( storedItem ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         _context.DeleteItem( "table", TableItem.Create( storedItem ), ConflictHandling.Throw );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
      }

      [TestMethod]
      public void Delete_TableItemWithETagHasChangedConflictHandlingOverwrite_DeletesEntity()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItemWithETag
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         var storedItem = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).Single();

         _context.Update( "table", TableItem.Create( storedItem ), ConflictHandling.Overwrite );
         _context.Save( Execute.Individually );

         _context.DeleteItem( "table", TableItem.Create( storedItem ), ConflictHandling.Overwrite );
         _context.Save( Execute.Individually );

         var item = _context.CreateQuery<DecoratedItemWithETag>( "table" ).PartitionKeyEquals( "abc" ).RowKeyEquals( "123" ).SingleOrDefault();
         Assert.IsNull( item );
      }

      [TestMethod]
      public void SaveAtomically_ManyOperations_AllOperationsPersist()
      {
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "789",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "456",
            Age = 42
         } ) );
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 34
         } ), ConflictHandling.Throw );
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "789",
            Age = 42
         } ) );
         _context.Save( Execute.Individually );

         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "456",
            Age = 42
         } ) );
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 34
         } ), ConflictHandling.Throw );
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "not found",
            Age = 42
         } ), ConflictHandling.Throw ); // should fail
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 42
         } ) );
         _context.Update( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "123",
            Age = 36
         } ), ConflictHandling.Throw );
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "foo"
         } ) );
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "bcd",
            Name = "foo"
         } ) );
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
         for ( int i = 0; i < 101; ++i )
         {
            _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
            {
               Id = "abc",
               Name = "foo" + i
            } ) );
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
         _context.AddNewItem( "table", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "foo"
         } ) );
         _context.AddNewItem( "table2", TableItem.Create( new DecoratedItem
         {
            Id = "abc",
            Name = "bar"
         } ) );

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
