using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.Test
{
   public static class SimpleDataItemExtensions
   {
      public static bool ComesBefore( this SimpleDataItem thisNode, IEnumerable<SimpleDataItem> listOfDataItems, SimpleDataItem laterNode )
      {
         int indexOfFirst = 0;
         int indexOfSecond = 0;

         int counter = 0;
         foreach ( var currentItemInIteration in listOfDataItems )
         {
            if ( currentItemInIteration.FirstType == thisNode.FirstType )
            {
               indexOfFirst = counter;
            }
            else if ( currentItemInIteration.FirstType == laterNode.FirstType )
            {
               indexOfSecond = counter;
            }
            counter++;
         }

         return indexOfFirst < indexOfSecond;
      }
   }

   [TestClass]
   public class TableStorageProviderTests
   {
      private readonly string _tableName = "doNotCare";
      private readonly string _partitionKey = "pk";
      private readonly string _rowKey = "a";
      private TableStorageProvider _tableStorageProvider;

      private readonly string _partitionKeyForRangeLow = "a";
      private readonly string _partitionKeyForRangeHigh = "z";

      [TestInitialize]
      public void SetUp()
      {
         InMemoryTableStorageProvider.ResetAllTables();
         _tableStorageProvider = new InMemoryTableStorageProvider();
      }

      [TestMethod]
      public void Add_ItemIsAddedAndNotSaved_SameContextCanReadUnsavedItem()
      {
         var expectedItem = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };
         _tableStorageProvider.Add( _tableName, expectedItem, _partitionKey, _rowKey );

         var item = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expectedItem.FirstType, item.FirstType );
         Assert.AreEqual( expectedItem.SecondType, item.SecondType );
      }

      [TestMethod]
      [ExpectedException( typeof( DataServiceRequestException ) )]
      public void Add_ItemWithPartitionKeyThatContainsInvalidCharacters_ThrowsDataServiceRequestException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string invalidPartitionKey = "/";
         _tableStorageProvider.Add( _tableName, item, invalidPartitionKey, _rowKey );
         _tableStorageProvider.Save();
      }

      [TestMethod]
      [ExpectedException( typeof( DataServiceRequestException ) )]
      public void Add_ItemWithPartitionKeyThatIsTooLong_ThrowsDataServiceRequestException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string partitionKeyThatIsLongerThan256Characters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
         _tableStorageProvider.Add( _tableName, item, partitionKeyThatIsLongerThan256Characters, _rowKey );
         _tableStorageProvider.Save();
      }

      [TestMethod]
      [ExpectedException( typeof( DataServiceRequestException ) )]
      public void Add_ItemWithRowKeyThatContainsInvalidCharacters_ThrowsDataServiceRequestException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string invalidRowKey = "/";
         _tableStorageProvider.Add( _tableName, item, _partitionKey, invalidRowKey );
         _tableStorageProvider.Save();
      }

      [TestMethod]
      [ExpectedException( typeof( DataServiceRequestException ) )]
      public void Add_ItemWithRowKeyThatIsTooLong_ThrowsDataServiceRequestException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string rowKeyThatIsLongerThan256Characters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
         _tableStorageProvider.Add( _tableName, item, _partitionKey, rowKeyThatIsLongerThan256Characters );
         _tableStorageProvider.Save();
      }

      [TestMethod]
      [ExpectedException( typeof( EntityAlreadyExistsException ) )]
      public void Add_ItemWithDuplicateRowAndPartitionKey_ThrowsEntityAlreadyExistsException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string rowKey = "rowkey";
         _tableStorageProvider.Add( _tableName, item, _partitionKey, rowKey );
         _tableStorageProvider.Add( _tableName, item, _partitionKey, rowKey );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Add_AddingToOneTableAndRetrievingFromAnother_ThrowsEntityDoesNotExistException()
      {
         var dataItem = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );

         _tableStorageProvider.Get<SimpleDataItem>( "OtherTableName", _partitionKey, _rowKey );
      }

      [TestMethod]
      public void Add_EntityHasPartitionAndRowKeyAttributes_PartitionAndRowKeysSetCorrectly()
      {
         var expected = new DecoratedItem { Id = "foo", Name = "bar" };
         _tableStorageProvider.Add( _tableName, expected );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get<DecoratedItem>( _tableName, "foo", "bar" );
         Assert.AreEqual( expected.Name, actual.Name );
         Assert.AreEqual( expected.Id, actual.Id );
      }

      [TestMethod]
      public void Delete_ItemInTable_ItemDeleted()
      {
         var dataItem = new SimpleDataItem();

         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var items = _tableStorageProvider.GetCollection<SimpleDataItem>( _tableName, _partitionKey );

         Assert.IsFalse( items.Any() );
      }

      [TestMethod]
      public void Delete_ManyItemsInTable_ItemsDeleted()
      {
         for ( var i = 0; i < 20; i++ )
         {
            var dataItem = new SimpleDataItem();

            _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey + i.ToString() );
            _tableStorageProvider.Save();
         }


         _tableStorageProvider.DeleteCollection( _tableName, _partitionKey );
         _tableStorageProvider.Save();

         var items = _tableStorageProvider.GetCollection<SimpleDataItem>( _tableName, _partitionKey );

         Assert.IsFalse( items.Any() );
         Assert.IsFalse( items.Any() );
      }

      [TestMethod]
      public void Delete_ItemIsNotInTable_NothingHappens()
      {
         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         _tableStorageProvider.Save();
      }

      [TestMethod]
      public void Delete_TableDoesNotExist_NothingHappens()
      {
         _tableStorageProvider.Delete( "table_that_doesnt_exist", _partitionKey, _rowKey );
         _tableStorageProvider.Save();
      }

      [TestMethod]
      public void Delete_ItemExistsInAnotherInstancesTempStore_ItemIsNotDeleted()
      {
         var dataItem = new SimpleDataItem();
         var secondTableStorageProvider = new InMemoryTableStorageProvider();
         secondTableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );

         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var instance = secondTableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
         Assert.IsNotNull( instance );
      }

      [TestMethod]
      public void Delete_ItemExistsAndTwoInstancesTryToDelete_ItemIsNotFoundInEitherCase()
      {
         var dataItem = new SimpleDataItem();
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var firstTableStorageProvider = new InMemoryTableStorageProvider();
         var secondTableStorageProvider = new InMemoryTableStorageProvider();

         firstTableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         secondTableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );


         bool instanceOneExisted = false;
         bool instanceTwoExisted = false;

         try
         {
            firstTableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
            instanceOneExisted = true;
         }
         catch ( EntityDoesNotExistException )
         {
         }

         try
         {
            secondTableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
            instanceTwoExisted = true;
         }
         catch ( EntityDoesNotExistException )
         {
         }

         Assert.IsFalse( instanceOneExisted );
         Assert.IsFalse( instanceTwoExisted );
      }

      [TestMethod]
      public void Delete_ItemExistsAndIsDeletedButNotSaved_ItemExistsInAnotherInstance()
      {
         var secondTableStorageProvider = new InMemoryTableStorageProvider();
         var dataItem = new SimpleDataItem();
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         var instance = secondTableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.IsNotNull( instance );
      }

      [TestMethod]
      public void Get_OneItemInStore_HydratedItemIsReturned()
      {
         var dataItem = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );

         Assert.AreEqual( dataItem.FirstType, _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey ).FirstType );
         Assert.AreEqual( dataItem.SecondType, _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey ).SecondType );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Get_NoItemsInStore_EntityDoesNotExistExceptionThrown()
      {
         _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
      }

      [TestMethod]
      public void Get_ManyItemsInStore_HydratedItemIsReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
                                   {
                                      FirstType = "a",
                                      SecondType = 1
                                   }, _partitionKey, "a" );
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
                                   {
                                      FirstType = "b",
                                      SecondType = 2
                                   }, _partitionKey, "b" );
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
                                   {
                                      FirstType = "c",
                                      SecondType = 3
                                   }, _partitionKey, "c" );

         var result = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, "b" );

         Assert.AreEqual( "b", result.FirstType );
      }

      [TestMethod]
      public void SaveChanges_ItemWasAdded_SaveIsSuccessful()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
                                          {
                                             FirstType = "a",
                                             SecondType = 1
                                          }, _partitionKey, _rowKey );

         _tableStorageProvider.Save();
      }

      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void AddItem_TwoMemoryContextsAndItemAddedButNotSavedInFirstContext_TheSecondContextWontSeeAddedItem()
      {
         InMemoryTableStorageProvider.ResetAllTables();

         var firstTableStorageProvider = new InMemoryTableStorageProvider();
         var secondTableStorageProvider = new InMemoryTableStorageProvider();

         firstTableStorageProvider.Add( _tableName, new SimpleDataItem
                                        {
                                           FirstType = "a",
                                           SecondType = 1
                                        }, _partitionKey, _rowKey );

         secondTableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
      }

      [TestMethod]
      public void AddItem_TwoMemoryContexts_TheSecondContextWillSeeAddedAndSavedItem()
      {
         InMemoryTableStorageProvider.ResetAllTables();
         var firstTableStorageProvider = new InMemoryTableStorageProvider();
         var secondTableStorageProvider = new InMemoryTableStorageProvider();

         var expectedItem = new SimpleDataItem
                              {
                                 FirstType = "a",
                                 SecondType = 1
                              };

         firstTableStorageProvider.Add( _tableName, expectedItem, _partitionKey, _rowKey );
         firstTableStorageProvider.Save();

         var item = secondTableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expectedItem.FirstType, item.FirstType );
         Assert.AreEqual( expectedItem.SecondType, item.SecondType );
      }

      [TestMethod]
      public void AddItem_TwoMemoryContexts_ThePrimaryContextsUncommitedStoreShouldBeUnchangedWhenAnotherIsCreated()
      {
         InMemoryTableStorageProvider.ResetAllTables();
         var firstContext = new InMemoryTableStorageProvider();

         var expectedItem = new SimpleDataItem
                              {
                                 FirstType = "a",
                                 SecondType = 1
                              };

         firstContext.Add( _tableName, expectedItem, _partitionKey, _rowKey );

         new InMemoryTableStorageProvider();

         var item = firstContext.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expectedItem.FirstType, item.FirstType );
         Assert.AreEqual( expectedItem.SecondType, item.SecondType );
      }

      [TestMethod]
      public void GetCollection_ZeroItemsInStore_EnumerableWithNoItemsReturned()
      {
         var result = _tableStorageProvider.GetCollection<SimpleDataItem>( _tableName, _partitionKey );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod]
      public void GetCollection_OneItemInStore_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
                                   {
                                      FirstType = "a",
                                      SecondType = 1
                                   }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();
         var result = _tableStorageProvider.GetCollection<SimpleDataItem>( _tableName, _partitionKey );

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod]
      public void GetCollection_ItemInAnotherInstance_EnumerableWithNoItemsReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, _partitionKey, _rowKey );
         var secondStorageProvider = new InMemoryTableStorageProvider();

         var result = secondStorageProvider.GetCollection<SimpleDataItem>( _tableName, _partitionKey );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod]
      public void GetRangeByPartitionKey_ZeroItemsInStore_EnumerableWithNoItemsReturned()
      {
         var result = _tableStorageProvider.GetRangeByPartitionKey<SimpleDataItem>( _tableName, _partitionKey, _partitionKey );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod]
      public void GetRangeByPartitionKey_OneItemsInStoreWithinRange_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, _partitionKeyForRangeLow, _rowKey );

         var result = _tableStorageProvider.GetRangeByPartitionKey<SimpleDataItem>( _tableName, _partitionKeyForRangeLow, _partitionKeyForRangeHigh );

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod]
      public void GetRangeByPartitionKey_TwoItemsInStoreWithinRange_EnumerableWithTwoItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, _partitionKeyForRangeLow, _rowKey );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, _partitionKeyForRangeHigh, _rowKey );

         var result = _tableStorageProvider.GetRangeByPartitionKey<SimpleDataItem>( _tableName, _partitionKeyForRangeLow, _partitionKeyForRangeHigh );

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      public void GetRangeByPartitionKey_OneItemInStoreWithinRangeOneItemOutsideRange_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "b", _rowKey );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "0", "b" );

         var result = _tableStorageProvider.GetRangeByPartitionKey<SimpleDataItem>( _tableName, _partitionKeyForRangeLow, _partitionKeyForRangeHigh );

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod]
      public void GetRangeByPartitionKey_OneItemInStoreWithinRangeTwoItemsOutsideRange_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "2012_01_10_11_26", _rowKey );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "2012_01_10_11_25", "b" );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "2012_01_10_11_28", "b" );

         var result = _tableStorageProvider.GetRangeByPartitionKey<SimpleDataItem>( _tableName, "2012_01_10_11_26", "2012_01_10_11_27" );

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod]
      public void GetRangeByPartitionKey_TwoItemInStoreWithinRangeTwoItemsOutsideRange_EnumerableWithTwoItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "2012_01_10_11_26", _rowKey );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "2012_01_10_11_26", "b" );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "2012_01_10_11_25", _rowKey );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, "2012_01_10_11_28", _rowKey );

         var result = _tableStorageProvider.GetRangeByPartitionKey<SimpleDataItem>( _tableName, "2012_01_10_11_26", "2012_01_10_11_27" );

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      public void Add_InsertingTypeWithNullableProperty_ShouldSucceed()
      {
         _tableStorageProvider.Add( _tableName, new NullableSimpleType
                                         {
                                            FirstNullableType = null,
                                            SecondNullableType = 2
                                         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();
      }

      [TestMethod]
      public void AddAndRetrieveNewItem_InsertingTypeWithUriProperty_ShouldSucceed()
      {
         var expectedValue = new Uri( "http://google.com" );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
                                         {
                                            FirstType = expectedValue,
                                         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var value = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expectedValue, value.FirstType );
      }

      [TestMethod]
      public void Get_InsertingTypeWithNullableProperty_ShouldSucceed()
      {
         var expected = new NullableSimpleType
                                  {
                                     FirstNullableType = null,
                                     SecondNullableType = 2
                                  };

         _tableStorageProvider.Add( _tableName, expected, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<NullableSimpleType>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( result.FirstNullableType, expected.FirstNullableType );
         Assert.AreEqual( result.SecondNullableType, expected.SecondNullableType );
      }

      [TestMethod]
      public void Update_ItemExistsAndUpdatedPropertyIsValid_ShouldItemTheItem()
      {
         EnsureOneItemInContext( _tableStorageProvider );

         var itemToUpdate = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
         string updatedFirstType = "Updated";
         itemToUpdate.FirstType = updatedFirstType;

         _tableStorageProvider.Update( _tableName, itemToUpdate, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var resultingItem = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( updatedFirstType, resultingItem.FirstType );
      }

      [TestMethod]
      public void Update_ExistingItemIsUpdatedInOneInstanceAndNotSaved_ShouldBeUnaffectedInOtherInstances()
      {
         var secondStorageProvider = new InMemoryTableStorageProvider();
         var item = new SimpleDataItem
                          {
                             FirstType = "first"
                          };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         item.FirstType = "second";
         _tableStorageProvider.Update( _tableName, item, _partitionKey, _rowKey );

         var result = secondStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( "first", result.FirstType );
      }

      [TestMethod]
      public void Get_AddingItemWithNotSerializedProperty_RetrievedItemMissingProperty()
      {
         var dataItem = new SimpleItemWithDontSerializeAttribute
                        {
                           SerializedString = "foo",
                           NotSerializedString = "bar"
                        };

         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var gotItem = _tableStorageProvider.Get<SimpleItemWithDontSerializeAttribute>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( null, gotItem.NotSerializedString );
         Assert.AreEqual( dataItem.SerializedString, gotItem.SerializedString );

      }



      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Update_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         var itemToUpdate = new SimpleDataItem
                            {
                               FirstType = "First",
                               SecondType = 2
                            };

         itemToUpdate.FirstType = "Do not care";

         _tableStorageProvider.Update( _tableName, itemToUpdate, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         Assert.Fail( "Should have thrown EntityDoesNotExistException" );
      }

      [TestMethod]
      public void Save_TwoTablesHaveBeenWrittenTo_ShouldSaveBoth()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         _tableStorageProvider.Add( "firstTable", simpleItem, _partitionKey, _rowKey );

         _tableStorageProvider.Add( "secondTable", simpleItem, _partitionKey, _rowKey );

         _tableStorageProvider.Save();

         var itemOne = _tableStorageProvider.Get<SimpleDataItem>( "firstTable", _partitionKey, _rowKey );
         var itemTwo = _tableStorageProvider.Get<SimpleDataItem>( "secondTable", _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, itemOne.FirstType );
         Assert.AreEqual( simpleItem.FirstType, itemTwo.FirstType );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Get_AddItemToOneTableAndReadFromAnother_ItemIsNotReturnedFromSecondTable()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };
         _tableStorageProvider.Add( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         string differentTableName = "hash";
         _tableStorageProvider.Get<SimpleDataItem>( differentTableName, _partitionKey, _rowKey );

         Assert.Fail( "Should have thrown EntityDoesNotExistException." );
      }

      [TestMethod]
      public void Upsert_MultipleUpserts_UpdatesItem()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "second";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "third";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "fourth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "fifth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "umpteenth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actualDataItem = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      public void Upsert_MultipleUpsertsAndCallingSaveAtTheEnd_UpdatesItem()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "second";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "third";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "fourth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "fifth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "umpteenth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         _tableStorageProvider.Save();

         var actualDataItem = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Upsert_MultipleUpsertsWithoutCallingSave_CallingGetThrowsEntityDoesNotExistException()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "second";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "third";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "fourth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "fifth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         simpleItem.FirstType = "umpteenth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );

         var actualDataItem = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      public void Upsert_MultipleItemsExist_UpdateSpecificItem()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "second";
         _tableStorageProvider.Upsert( _tableName, simpleItem, "DONTCARE1", "DONTCARE2" );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "third";
         _tableStorageProvider.Upsert( _tableName, simpleItem, "DONTCARE3", "DONTCARE4" );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "fourth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, "DONTCARE5", "DONTCARE6" );
         _tableStorageProvider.Save();

         simpleItem.FirstType = "fifth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actualDataItem = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      public void Upsert_UpsertAndCallingSaveAfterTryingToReadFromTheTable_ShouldActuallyInsert()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         try
         {
            _tableStorageProvider.Get<SimpleDataItem>( _tableName, "DoNotCare", "DoNotCare" );
         }
         catch ( EntityDoesNotExistException )
         {
         }


         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actualDataItem = new InMemoryTableStorageProvider().Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      public void Upsert_ExistingItemIsUpsertedInOneInstanceAndNotSaved_ShouldBeUnaffectedInOtherInstances()
      {
         var secondStorageProvider = new InMemoryTableStorageProvider();
         var item = new SimpleDataItem
                          {
                             FirstType = "first"
                          };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         item.FirstType = "second";
         _tableStorageProvider.Upsert( _tableName, item, _partitionKey, _rowKey );

         var result = secondStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( "first", result.FirstType );
      }

      [TestMethod]
      public void GetRangeByRowKey_ZeroItemsInStore_EnumerableWithNoItemsReturned()
      {
         var result = _tableStorageProvider.GetRangeByRowKey<SimpleDataItem>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod]
      public void GetRangeByRowKey_OneItemInStoreButDoesntMatchPredicate_EnumerableWithNoItemsReturned()
      {
         var item = new SimpleDataItem { FirstType = "a", SecondType = 1 };

         _tableStorageProvider.Add<SimpleDataItem>( _tableName, item, _partitionKey, "there" );
         var result = _tableStorageProvider.GetRangeByRowKey<SimpleDataItem>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod]
      public void GetRangeByRowKey_OneItemInStore_EnumerableWithNoItemsReturned()
      {
         var item = new SimpleDataItem { FirstType = "a", SecondType = 1 };

         _tableStorageProvider.Add<SimpleDataItem>( _tableName, item, _partitionKey, "hithere" );
         var result = _tableStorageProvider.GetRangeByRowKey<SimpleDataItem>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod]
      public void GetRangeByRowKey_ManyItemsInStore_EnumerableWithAppropriateItemsReturned()
      {
         var item1 = new SimpleDataItem { FirstType = "a", SecondType = 1 };
         var item2 = new SimpleDataItem { FirstType = "b", SecondType = 2 };
         var item3 = new SimpleDataItem { FirstType = "c", SecondType = 3 };
         var item4 = new SimpleDataItem { FirstType = "d", SecondType = 4 };

         _tableStorageProvider.Add<SimpleDataItem>( _tableName, item1, _partitionKey, "asdf" );
         _tableStorageProvider.Add<SimpleDataItem>( _tableName, item2, _partitionKey, "hithere" );
         _tableStorageProvider.Add<SimpleDataItem>( _tableName, item3, _partitionKey, "jklh" );
         _tableStorageProvider.Add<SimpleDataItem>( _tableName, item4, _partitionKey, "hi" );

         var result = _tableStorageProvider.GetRangeByRowKey<SimpleDataItem>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      public void Add_AddingItemWithPropertyWithInternalGetter_WillSerializeTheProperty()
      {
         var item = new TypeWithPropertyWithInternalGetter
         {
            FirstType = "a",
            PropertyWithInternalGetter = 1
         };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithPropertyWithInternalGetter>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( 1, result.PropertyWithInternalGetter );
      }

      [TestMethod]
      public void GetCollection_ManyItemsInStore_ShouldBeRetreivedInProperSortedOrder()
      {
         var dataItem1 = new SimpleDataItem { FirstType = "a", SecondType = 1 };
         var dataItem2 = new SimpleDataItem { FirstType = "b", SecondType = 2 };
         var dataItem3 = new SimpleDataItem { FirstType = "c", SecondType = 3 };
         var dataItem4 = new SimpleDataItem { FirstType = "d", SecondType = 4 };

         _tableStorageProvider.Add( _tableName, dataItem1, _partitionKey, "3" );
         _tableStorageProvider.Add( _tableName, dataItem2, _partitionKey, "2" );
         _tableStorageProvider.Add( _tableName, dataItem3, _partitionKey, "1" );
         _tableStorageProvider.Add( _tableName, dataItem4, _partitionKey, "4" );

         var listOfItems = _tableStorageProvider.GetCollection<SimpleDataItem>( _tableName, _partitionKey );

         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem1 ), "Making sure item 3 comes before item 1." );
         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem2 ), "Making sure item 3 comes before item 2." );
         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem4 ), "Making sure item 3 comes before item 4." );

         Assert.IsTrue( dataItem2.ComesBefore( listOfItems, dataItem1 ), "Making sure item 2 comes before item 1." );
         Assert.IsTrue( dataItem2.ComesBefore( listOfItems, dataItem4 ), "Making sure item 2 comes before item 4." );

         Assert.IsTrue( dataItem1.ComesBefore( listOfItems, dataItem4 ), "Making sure item 1 comes before item 4." );
      }

      private void EnsureOneItemInContext( TableStorageProvider tableStorageProvider )
      {
         var item = new SimpleDataItem
                    {
                       FirstType = "First",
                       SecondType = 2
                    };

         tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         tableStorageProvider.Save();
      }
   }
}
