﻿using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Dynamic;
using System.Globalization;
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
         _tableStorageProvider.Save();


         _tableStorageProvider.Add( _tableName, item, _partitionKey, rowKey );
         _tableStorageProvider.Save();
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
      public void AddAndGet_AnonymousType_SerializesAndDeserializesProperly()
      {
         var dataItem = new { FirstType = "a", SecondType = 1 };

         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();


         dynamic result = _tableStorageProvider.Get( _tableName, _partitionKey, _rowKey );


         Assert.AreEqual( dataItem.FirstType, result.FirstType );
         Assert.AreEqual( dataItem.SecondType, result.SecondType );
      }

      [TestMethod]
      public void AddAndGet_AnonymousTypeWithPartionAndRowKeyProperties_ShouldBeInsertedWithThoseKeys()
      {
         var dataItem = new { PartitionKey = "test", RowKey = "key", NonKey = "foo" };

         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;
         _tableStorageProvider.Add( _tableName, dataItem );
         _tableStorageProvider.Save();


         dynamic result = _tableStorageProvider.Get( _tableName, dataItem.PartitionKey, dataItem.RowKey );


         Assert.AreEqual( dataItem.NonKey, result.NonKey );
      }

      [TestMethod]
      public void Add_EntityHasLocalDateTime_DateIsRetrievedAsUTCButIsEqual()
      {
         var theDate = new DateTime( 635055151618936589, DateTimeKind.Local );
         var item = new TypeWithDateTime
         {
            DateTimeProperty = theDate
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get<TypeWithDateTime>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( DateTimeKind.Utc, actual.DateTimeProperty.Kind );
         Assert.AreEqual( theDate.ToUniversalTime(), actual.DateTimeProperty );
      }

      [TestMethod]
      public void Add_EntityHasLocalDateTimeStoredInOffset_DateOffsetIsRetrieved()
      {
         var theDateTime = new DateTime( 635055151618936589, DateTimeKind.Local );
         var theDateTimeOffset = new DateTimeOffset( theDateTime );
         var item = new TypeWithDateTimeOffset
         {
            DateTimeOffsetProperty = theDateTimeOffset
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get<TypeWithDateTimeOffset>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( theDateTimeOffset, actual.DateTimeOffsetProperty );
         Assert.AreEqual( theDateTime, actual.DateTimeOffsetProperty.LocalDateTime );
      }

      [TestMethod]
      public void Add_EntityHasPartitionAndRowKeyAttributes_PartitionAndRowKeysSetCorrectly()
      {
         var expected = new DecoratedItem { Id = "foo", Name = "bar", Age = 1 };
         _tableStorageProvider.Add( _tableName, expected );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get<DecoratedItem>( _tableName, "foo", "bar" );
         Assert.AreEqual( expected.Name, actual.Name );
         Assert.AreEqual( expected.Id, actual.Id );
      }

      [TestMethod]
      public void Add_EntityHasEnumAttribute_IsSavedAndRetrievedProperly()
      {
         var expected = new TypeWithEnumProperty { EnumProperty = TypeWithEnumProperty.TheEnum.SecondItem };
         _tableStorageProvider.Add( _tableName, expected, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get<TypeWithEnumProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expected.EnumProperty, actual.EnumProperty );
      }

      [TestMethod]
      public void Add_EntityHasInvalidEnumValue_IsRetrievedAsDefaultEnumValue()
      {
         var expected = new TypeWithEnumProperty
         {
            EnumProperty = (TypeWithEnumProperty.TheEnum) 10
         };
         _tableStorageProvider.Add( _tableName, expected, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get<TypeWithEnumProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( TypeWithEnumProperty.TheEnum.FirstItem, actual.EnumProperty );
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

            _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey + i.ToString( CultureInfo.InvariantCulture ) );
            _tableStorageProvider.Save();
         }


         _tableStorageProvider.DeleteCollection( _tableName, _partitionKey );
         _tableStorageProvider.Save();

         var items = _tableStorageProvider.GetCollection<SimpleDataItem>( _tableName, _partitionKey );

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

         secondTableStorageProvider.Save();

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
         firstTableStorageProvider.Save();
         secondTableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         secondTableStorageProvider.Save();


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
      [ExpectedException( typeof( EntityHasBeenChangedException ) )]
      public void Delete_ItemWithETagHasBeenUpdated_ThrowsEntityHasBeenChangedException()
      {
         var decoratedItemWithETag = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 23
         };
         _tableStorageProvider.Add( _tableName, decoratedItemWithETag );
         _tableStorageProvider.Save();

         var storedItem = _tableStorageProvider.Get<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         storedItem.Age = 25;
         _tableStorageProvider.Update( _tableName, storedItem );
         _tableStorageProvider.Save();

         _tableStorageProvider.Delete( _tableName, storedItem );
         _tableStorageProvider.Save();
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
         _tableStorageProvider.Save();

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
         _tableStorageProvider.Save();

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
      public void AddItem_TwoMemoryContexts_TheSecondContextWillNotSeeAddedAndSavedItem_WithInstanceAccount()
      {
         InMemoryTableStorageProvider.ResetAllTables();
         var firstTableStorageProvider = new InMemoryTableStorageProvider( new MemoryStorageAccount() );
         var secondTableStorageProvider = new InMemoryTableStorageProvider( new MemoryStorageAccount() );

         var expectedItem = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         firstTableStorageProvider.Add( _tableName, expectedItem, _partitionKey, _rowKey );
         firstTableStorageProvider.Save();

         bool hasThrown = false;
         try
         {
            secondTableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
         }
         catch ( EntityDoesNotExistException )
         {
            hasThrown = true;
         }

         Assert.IsTrue( hasThrown );
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
         firstContext.Save();

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
         _tableStorageProvider.Save();

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
         _tableStorageProvider.Save();

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
         _tableStorageProvider.Save();

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
         _tableStorageProvider.Save();

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
         _tableStorageProvider.Save();

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
                                            UriTypeProperty = expectedValue,
                                         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var value = _tableStorageProvider.Get<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expectedValue, value.UriTypeProperty );
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
      public void Update_ItemExistsAndUpdatedPropertyIsValid_ShouldUpdateTheItem()
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

      // This test ensures retreiving and updating dynamic entities is 
      // backwards compatible with the optimistic concurrency update
      [TestMethod]
      public void Update_MultipleUpdatesFromSingleDynamicEntity_SucceedsRegardlessIfEntityHasBeenChanged()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 33
         };
         _tableStorageProvider.Add( _tableName, item );
         _tableStorageProvider.Save();

         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;

         var storedItem = _tableStorageProvider.Get( _tableName, "foo", "bar" );

         storedItem.Age = 44;
         _tableStorageProvider.Update( _tableName, storedItem );
         _tableStorageProvider.Save();

         storedItem.Age = 39;
         _tableStorageProvider.Update( _tableName, storedItem );
         _tableStorageProvider.Save();
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
      public void Get_ItemWithETagPropertyInStore_ItemReturnedWithETag()
      {
         var decoratedETagItem = new DecoratedItemWithETag
         {
            Id = "someId",
            Name = "someName",
            Age = 12,
         };

         _tableStorageProvider.Add( _tableName, decoratedETagItem );
         _tableStorageProvider.Save();

         var actualItem = _tableStorageProvider.Get<DecoratedItemWithETag>( _tableName, "someId", "someName" );
         Assert.IsNotNull( actualItem.ETag );
      }

      [TestMethod]
      public void Get_RetreiveAsDynamic_DynamicItemHasETagProperty()
      {
         var decoratedItem = new DecoratedItem
         {
            Id = "id",
            Name = "name",
            Age = 33
         };

         _tableStorageProvider.Add( _tableName, decoratedItem );
         _tableStorageProvider.Save();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = true;

         var actualItem = _tableStorageProvider.Get( _tableName, "id", "name" );
         var itemAsDict = actualItem as IDictionary<string, object>;
         Assert.IsTrue( itemAsDict.ContainsKey( "ETag" ) );
      }

      [TestMethod]
      public void Add_ClassWithPropertyOfTypeThatHasDontSerializeAttribute_DoesNotSerializeThatProperty()
      {
         var newItem = new SimpleClassContainingTypeWithDontSerializeAttribute
         {
            StringWithoutDontSerializeAttribute = "You should see this",
            ThingWithDontSerializeAttribute = new SimpleTypeWithDontSerializeAttribute
            {
               StringWithoutDontSerializeAttribute = "You shouldn't see this"
            }
         };

         _tableStorageProvider.Add( _tableName, newItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var resultItem = _tableStorageProvider.Get<SimpleClassContainingTypeWithDontSerializeAttribute>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( null, resultItem.ThingWithDontSerializeAttribute );
         Assert.AreEqual( newItem.StringWithoutDontSerializeAttribute, resultItem.StringWithoutDontSerializeAttribute );
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

      [TestCategory( "Integration" ), TestMethod]
      public void Upsert_ItemUpsertedTwiceAndNotAffectedByETag_ETagPropertyGetsUpdatedEachUpsert()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo2",
            Name = "bar2",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         _tableStorageProvider.Save();

         var retreivedItem = _tableStorageProvider.Get<DecoratedItemWithETag>( _tableName, "foo2", "bar2" );

         retreivedItem.Age = 39;
         _tableStorageProvider.Upsert( _tableName, retreivedItem );
         _tableStorageProvider.Save();

         var upsertedItem = _tableStorageProvider.Get<DecoratedItemWithETag>( _tableName, "foo2", "bar2" );
         Assert.AreNotEqual( retreivedItem.ETag, upsertedItem.ETag );

         retreivedItem.Age = 41;
         _tableStorageProvider.Upsert( _tableName, retreivedItem );
         _tableStorageProvider.Save();

         var upsertedItem2 = _tableStorageProvider.Get<DecoratedItemWithETag>( _tableName, "foo2", "bar2" );
         Assert.AreNotEqual( upsertedItem.ETag, upsertedItem2.ETag );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Merge_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         _tableStorageProvider.Merge( _tableName, new SimpleDataItem { FirstType = "first" }, "not", "found" );
         _tableStorageProvider.Save();
      }

      [TestMethod]
      public void Merge_ItemExistsAndOnePropertyOverwritten_WrittenPropertyHasNewValueAndUnwrittenPropertiesRetainValues()
      {
         dynamic item = new ExpandoObject();
         item.Height = 50;
         item.Name = "Bill";

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         dynamic update = new ExpandoObject();
         update.Height = 60;
         _tableStorageProvider.Merge( _tableName, update, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         dynamic updatedItem = _tableStorageProvider.Get( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( 60, updatedItem.Height );
         Assert.AreEqual( item.Name, updatedItem.Name );
      }

      [TestMethod]
      [ExpectedException( typeof( EntityHasBeenChangedException ) )]
      public void Merge_DynamicItemHasOutdatedETag_ThrowsEntityHasBeenChangedException()
      {
         dynamic item = new ExpandoObject();
         item.Height = 50;
         item.Name = "Bill";

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = true;
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;

         var retreivedItem = _tableStorageProvider.Get( _tableName, _partitionKey, _rowKey );
         retreivedItem.Height = 66;
         _tableStorageProvider.Merge( _tableName, retreivedItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var tmp = _tableStorageProvider.Get( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 66, tmp.Height );
         Assert.AreNotEqual( retreivedItem.ETag, tmp.ETag );

         retreivedItem.Height = 70;
         _tableStorageProvider.Merge( _tableName, retreivedItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         Assert.Fail( "Should have thrown an EntityHasBeenChangedException" );
      }

      [TestMethod]
      public void Merge_DynamicItemHasOutdatedETagConflictHandlingOverwrite_MergesItem()
      {
         dynamic item = new ExpandoObject();
         item.Height = 50;
         item.Name = "Bill";

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = true;
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;

         var retreivedItem = _tableStorageProvider.Get( _tableName, _partitionKey, _rowKey );
         retreivedItem.Height = 66;
         _tableStorageProvider.Merge( _tableName, retreivedItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var tmp = _tableStorageProvider.Get( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 66, tmp.Height );
         Assert.AreNotEqual( retreivedItem.ETag, tmp.ETag );

         retreivedItem.Height = 70;
         _tableStorageProvider.Merge( _tableName, retreivedItem, _partitionKey, _rowKey, ConflictHandling.Overwrite );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 70, actual.Height );
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

         _tableStorageProvider.Add( _tableName, item, _partitionKey, "there" );
         _tableStorageProvider.Save();
         var result = _tableStorageProvider.GetRangeByRowKey<SimpleDataItem>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod]
      public void GetRangeByRowKey_OneItemInStore_EnumerableWithNoItemsReturned()
      {
         var item = new SimpleDataItem { FirstType = "a", SecondType = 1 };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, "hithere" );
         _tableStorageProvider.Save();
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

         _tableStorageProvider.Add( _tableName, item1, _partitionKey, "asdf" );
         _tableStorageProvider.Add( _tableName, item2, _partitionKey, "hithere" );
         _tableStorageProvider.Add( _tableName, item3, _partitionKey, "jklh" );
         _tableStorageProvider.Add( _tableName, item4, _partitionKey, "hi" );
         _tableStorageProvider.Save();

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
         _tableStorageProvider.Save();

         var listOfItems = _tableStorageProvider.GetCollection<SimpleDataItem>( _tableName, _partitionKey ).ToArray();

         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem1 ), "Making sure item 3 comes before item 1." );
         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem2 ), "Making sure item 3 comes before item 2." );
         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem4 ), "Making sure item 3 comes before item 4." );

         Assert.IsTrue( dataItem2.ComesBefore( listOfItems, dataItem1 ), "Making sure item 2 comes before item 1." );
         Assert.IsTrue( dataItem2.ComesBefore( listOfItems, dataItem4 ), "Making sure item 2 comes before item 4." );

         Assert.IsTrue( dataItem1.ComesBefore( listOfItems, dataItem4 ), "Making sure item 1 comes before item 4." );
      }

      [TestMethod]
      public void GetCollection_ItemsInStoreRetrievedDynamically_ShouldBeRetreived()
      {
         int expectedCount = 5;
         EnsureItemsInContext( _tableStorageProvider, expectedCount );

         IEnumerable<dynamic> items = _tableStorageProvider.GetCollection( _tableName );

         Assert.AreEqual( expectedCount, items.Count() );
      }

      [TestMethod]
      public void WriteOperations_CSharpDateTimeNotCompatibleWithEdmDateTime_StillStoresDateTime()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 ) });
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 ) }); 
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 ) });
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 ) });
         _tableStorageProvider.Save();

         var retrievedItem = _tableStorageProvider.Get<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( ( DateTime.MinValue + TimeSpan.FromDays( 1000 ) ).Year, retrievedItem.CreationDate.Year );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void WriteOperations_CSharpDateTimeMinValue_DateTimeStoredSuccessfully()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue });
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue }); 
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue });
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue });
         _tableStorageProvider.Save();

         var retrievedItem = _tableStorageProvider.Get<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( DateTime.MinValue, retrievedItem.CreationDate );
      }

      [TestMethod]
      public void WriteOperations_CSharpDateTimeMaxValue_DateTimeStoredSuccessfully()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MaxValue } );
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MaxValue } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MaxValue } );
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MaxValue } );
         _tableStorageProvider.Save();

         var retrievedItem = _tableStorageProvider.Get<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( DateTime.MaxValue, retrievedItem.CreationDate );
      }

      private void EnsureItemsInContext( TableStorageProvider tableStorageProvider, int count )
      {
         for ( int i = 0; i < count; i++ )
         {
            var item = new SimpleDataItem
                       {
                          FirstType = i.ToString( CultureInfo.InvariantCulture ),
                          SecondType = i
                       };
            tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey + i );
         }
         tableStorageProvider.Save();
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
