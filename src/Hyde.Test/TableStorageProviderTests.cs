using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
      public async Task Add_ItemWithPartitionKeyThatContainsInvalidCharacters_ThrowsDataServiceRequestException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string invalidPartitionKey = "/";
         _tableStorageProvider.Add( _tableName, item, invalidPartitionKey, _rowKey );
         await AsyncAssert.ThrowsAsync<InvalidEntityException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task Add_ItemWithPartitionKeyThatIsTooLong_ThrowsDataServiceRequestException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string partitionKeyThatIsLongerThan256Characters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
         _tableStorageProvider.Add( _tableName, item, partitionKeyThatIsLongerThan256Characters, _rowKey );
         await AsyncAssert.ThrowsAsync<InvalidEntityException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task Add_ItemWithRowKeyThatContainsInvalidCharacters_ThrowsDataServiceRequestException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string invalidRowKey = "/";
         _tableStorageProvider.Add( _tableName, item, _partitionKey, invalidRowKey );
         await AsyncAssert.ThrowsAsync<InvalidEntityException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task Add_ItemWithRowKeyThatIsTooLong_ThrowsDataServiceRequestException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string rowKeyThatIsLongerThan256Characters = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
         _tableStorageProvider.Add( _tableName, item, _partitionKey, rowKeyThatIsLongerThan256Characters );
         await AsyncAssert.ThrowsAsync<InvalidEntityException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task Add_ItemWithDuplicateRowAndPartitionKey_ThrowsEntityAlreadyExistsException()
      {
         var item = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         string rowKey = "rowkey";

         _tableStorageProvider.Add( _tableName, item, _partitionKey, rowKey );
         await _tableStorageProvider.SaveAsync();


         _tableStorageProvider.Add( _tableName, item, _partitionKey, rowKey );
         await AsyncAssert.ThrowsAsync<EntityAlreadyExistsException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task Add_AddingToOneTableAndRetrievingFromAnother_ThrowsEntityDoesNotExistException()
      {
         var dataItem = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );

         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.GetAsync<SimpleDataItem>( "OtherTableName", _partitionKey, _rowKey ) );
      }

      [TestMethod]
      public async Task AddAndGet_AnonymousType_SerializesAndDeserializesProperly()
      {
         var dataItem = new { FirstType = "a", SecondType = 1 };

         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();


         dynamic result = await _tableStorageProvider.GetAsync( _tableName, _partitionKey, _rowKey );


         Assert.AreEqual( dataItem.FirstType, result.FirstType );
         Assert.AreEqual( dataItem.SecondType, result.SecondType );
      }

      [TestMethod]
      public async Task AddAndGet_AnonymousTypeWithPartionAndRowKeyProperties_ShouldBeInsertedWithThoseKeys()
      {
         var dataItem = new { PartitionKey = "test", RowKey = "key", NonKey = "foo" };

         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;
         _tableStorageProvider.Add( _tableName, dataItem );
         await _tableStorageProvider.SaveAsync();


         dynamic result = await _tableStorageProvider.GetAsync( _tableName, dataItem.PartitionKey, dataItem.RowKey );


         Assert.AreEqual( dataItem.NonKey, result.NonKey );
      }

      [TestMethod]
      public async Task Add_EntityHasLocalDateTime_DateIsRetrievedAsUTCButIsEqual()
      {
         var theDate = new DateTime( 635055151618936589, DateTimeKind.Local );
         var item = new TypeWithDateTime
         {
            DateTimeProperty = theDate
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var actual = await _tableStorageProvider.GetAsync<TypeWithDateTime>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( DateTimeKind.Utc, actual.DateTimeProperty.Kind );
         Assert.AreEqual( theDate.ToUniversalTime(), actual.DateTimeProperty );
      }

      [TestMethod]
      public async Task Add_EntityHasLocalDateTimeStoredInOffset_DateOffsetIsRetrieved()
      {
         var theDateTime = new DateTime( 635055151618936589, DateTimeKind.Local );
         var theDateTimeOffset = new DateTimeOffset( theDateTime );
         var item = new TypeWithDateTimeOffset
         {
            DateTimeOffsetProperty = theDateTimeOffset
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var actual = await _tableStorageProvider.GetAsync<TypeWithDateTimeOffset>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( theDateTimeOffset, actual.DateTimeOffsetProperty );
         Assert.AreEqual( theDateTime, actual.DateTimeOffsetProperty.LocalDateTime );
      }

      [TestMethod]
      public async Task Add_EntityHasPartitionAndRowKeyAttributes_PartitionAndRowKeysSetCorrectly()
      {
         var expected = new DecoratedItem { Id = "foo", Name = "bar", Age = 1 };
         _tableStorageProvider.Add( _tableName, expected );
         await _tableStorageProvider.SaveAsync();

         var actual = await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "foo", "bar" );
         Assert.AreEqual( expected.Name, actual.Name );
         Assert.AreEqual( expected.Id, actual.Id );
      }

      [TestMethod]
      public async Task Add_EntityHasEnumAttribute_IsSavedAndRetrievedProperly()
      {
         var expected = new TypeWithEnumProperty { EnumProperty = TypeWithEnumProperty.TheEnum.SecondItem };
         _tableStorageProvider.Add( _tableName, expected, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var actual = await _tableStorageProvider.GetAsync<TypeWithEnumProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expected.EnumProperty, actual.EnumProperty );
      }

      [TestMethod]
      public async Task Add_EntityHasInvalidEnumValue_IsRetrievedAsDefaultEnumValue()
      {
         var expected = new TypeWithEnumProperty
         {
            EnumProperty = (TypeWithEnumProperty.TheEnum) 10
         };
         _tableStorageProvider.Add( _tableName, expected, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var actual = await _tableStorageProvider.GetAsync<TypeWithEnumProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( TypeWithEnumProperty.TheEnum.FirstItem, actual.EnumProperty );
      }

      [TestMethod]
      public async Task Delete_ItemInTable_ItemDeleted()
      {
         var dataItem = new SimpleDataItem();

         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var items = await _tableStorageProvider.CreateQuery<SimpleDataItem>( _tableName ).PartitionKeyEquals( _partitionKey ).Async();

         Assert.IsFalse( items.Any() );
      }

      [TestMethod]
      public async Task Delete_ManyItemsInTable_ItemsDeleted()
      {
         for ( var i = 0; i < 20; i++ )
         {
            var dataItem = new SimpleDataItem();

            _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey + i.ToString( CultureInfo.InvariantCulture ) );
            await _tableStorageProvider.SaveAsync();
         }


         IEnumerable<dynamic> itemsToDelete = await _tableStorageProvider.CreateQuery( _tableName ).PartitionKeyEquals( _partitionKey ).Async();
         foreach ( var item in itemsToDelete )
         {
            _tableStorageProvider.Delete( _tableName, item.PartitionKey, item.RowKey );
         }
         await _tableStorageProvider.SaveAsync();

         var items = await _tableStorageProvider.CreateQuery( _tableName ).PartitionKeyEquals( _partitionKey ).Async();

         Assert.IsFalse( items.Any() );
      }

      [TestMethod]
      public async Task Delete_ItemIsNotInTable_NothingHappens()
      {
         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();
      }

      [TestMethod]
      public async Task Delete_TableDoesNotExist_NothingHappens()
      {
         _tableStorageProvider.Delete( "table_that_doesnt_exist", _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();
      }

      [TestMethod]
      public async Task Delete_ItemExistsInAnotherInstancesTempStore_ItemIsNotDeleted()
      {
         var dataItem = new SimpleDataItem();
         var secondTableStorageProvider = new InMemoryTableStorageProvider();
         secondTableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );

         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         await secondTableStorageProvider.SaveAsync();

         var instance = await secondTableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
         Assert.IsNotNull( instance );
      }

      [TestMethod]
      public async Task Delete_ItemExistsAndTwoInstancesTryToDelete_ItemIsNotFoundInEitherCase()
      {
         var dataItem = new SimpleDataItem();
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var firstTableStorageProvider = new InMemoryTableStorageProvider();
         var secondTableStorageProvider = new InMemoryTableStorageProvider();

         firstTableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         await firstTableStorageProvider.SaveAsync();
         secondTableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         await secondTableStorageProvider.SaveAsync();


         bool instanceOneExisted = false;
         bool instanceTwoExisted = false;

         try
         {
            await firstTableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
            instanceOneExisted = true;
         }
         catch ( EntityDoesNotExistException )
         {
         }

         try
         {
            await secondTableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
            instanceTwoExisted = true;
         }
         catch ( EntityDoesNotExistException )
         {
         }

         Assert.IsFalse( instanceOneExisted );
         Assert.IsFalse( instanceTwoExisted );
      }

      [TestMethod]
      public async Task Delete_ItemExistsAndIsDeletedButNotSaved_ItemExistsInAnotherInstance()
      {
         var secondTableStorageProvider = new InMemoryTableStorageProvider();
         var dataItem = new SimpleDataItem();
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         var instance = await secondTableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.IsNotNull( instance );
      }

      [TestMethod]
      public async Task Delete_ItemWithETagHasBeenUpdated_ThrowsEntityHasBeenChangedException()
      {
         var decoratedItemWithETag = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 23
         };
         _tableStorageProvider.Add( _tableName, decoratedItemWithETag );
         await _tableStorageProvider.SaveAsync();

         var storedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         storedItem.Age = 25;
         _tableStorageProvider.Update( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Delete( _tableName, storedItem );
         await AsyncAssert.ThrowsAsync<EntityHasBeenChangedException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task GetAsync_OneItemInStore_HydratedItemIsReturned()
      {
         var dataItem = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         Assert.AreEqual( dataItem.FirstType, ( await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey ) ).FirstType );
         Assert.AreEqual( dataItem.SecondType, ( await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey ) ).SecondType );
      }

      [TestMethod]
      public async Task GetAsync_NoItemsInStore_EntityDoesNotExistExceptionThrown()
      {
         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey ) );
      }

      [TestMethod]
      public async Task GetAsync_ManyItemsInStore_HydratedItemIsReturned()
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
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, "b" );

         Assert.AreEqual( "b", result.FirstType );
      }

      [TestMethod]
      public async Task SaveChanges_ItemWasAdded_SaveIsSuccessful()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
                                          {
                                             FirstType = "a",
                                             SecondType = 1
                                          }, _partitionKey, _rowKey );

         await _tableStorageProvider.SaveAsync();
      }

      [TestMethod]
      public async Task AddItem_TwoMemoryContextsAndItemAddedButNotSavedInFirstContext_TheSecondContextWontSeeAddedItem()
      {
         InMemoryTableStorageProvider.ResetAllTables();

         var firstTableStorageProvider = new InMemoryTableStorageProvider();
         var secondTableStorageProvider = new InMemoryTableStorageProvider();

         firstTableStorageProvider.Add( _tableName, new SimpleDataItem
                                        {
                                           FirstType = "a",
                                           SecondType = 1
                                        }, _partitionKey, _rowKey );

         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => secondTableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey ) );
      }

      [TestMethod]
      public async Task AddItem_TwoMemoryContexts_TheSecondContextWillSeeAddedAndSavedItem()
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
         await firstTableStorageProvider.SaveAsync();

         var item = await secondTableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expectedItem.FirstType, item.FirstType );
         Assert.AreEqual( expectedItem.SecondType, item.SecondType );
      }

      [TestMethod]
      public async Task AddItem_TwoMemoryContexts_TheSecondContextWillNotSeeAddedAndSavedItem_WithInstanceAccount()
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
         await firstTableStorageProvider.SaveAsync();

         bool hasThrown = false;
         try
         {
            await secondTableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
         }
         catch ( EntityDoesNotExistException )
         {
            hasThrown = true;
         }

         Assert.IsTrue( hasThrown );
      }

      [TestMethod]
      public async Task AddItem_TwoMemoryContexts_ThePrimaryContextsUncommitedStoreShouldBeUnchangedWhenAnotherIsCreated()
      {
         InMemoryTableStorageProvider.ResetAllTables();
         var firstContext = new InMemoryTableStorageProvider();

         var expectedItem = new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         };

         firstContext.Add( _tableName, expectedItem, _partitionKey, _rowKey );
         await firstContext.SaveAsync();

         new InMemoryTableStorageProvider();

         var item = await firstContext.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expectedItem.FirstType, item.FirstType );
         Assert.AreEqual( expectedItem.SecondType, item.SecondType );
      }

      [TestMethod]
      public async Task GetRangeByPartitionKey_OneItemsInStoreWithinRange_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new SimpleDataItem
         {
            FirstType = "a",
            SecondType = 1
         }, _partitionKeyForRangeLow, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result =
            await _tableStorageProvider.CreateQuery<SimpleDataItem>( _tableName )
               .PartitionKeyFrom( _partitionKeyForRangeLow )
               .Inclusive()
               .PartitionKeyTo( _partitionKeyForRangeHigh )
               .Inclusive()
               .Async();

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod]
      public async Task Add_InsertingTypeWithNullableProperty_ShouldSucceed()
      {
         _tableStorageProvider.Add( _tableName, new NullableSimpleType
                                         {
                                            FirstNullableType = null,
                                            SecondNullableType = 2
                                         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();
      }

      [TestMethod]
      public async Task AddAndRetrieveNewItem_InsertingTypeWithUriProperty_ShouldSucceed()
      {
         var expectedValue = new Uri( "http://google.com" );

         _tableStorageProvider.Add( _tableName, new SimpleDataItem
                                         {
                                            UriTypeProperty = expectedValue,
                                         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var value = await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( expectedValue, value.UriTypeProperty );
      }

      [TestMethod]
      public async Task GetAsync_InsertingTypeWithNullableProperty_ShouldSucceed()
      {
         var expected = new NullableSimpleType
                                  {
                                     FirstNullableType = null,
                                     SecondNullableType = 2
                                  };

         _tableStorageProvider.Add( _tableName, expected, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<NullableSimpleType>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( result.FirstNullableType, expected.FirstNullableType );
         Assert.AreEqual( result.SecondNullableType, expected.SecondNullableType );
      }

      [TestMethod]
      public async Task Update_ItemExistsAndUpdatedPropertyIsValid_ShouldUpdateTheItem()
      {
         await EnsureOneItemInContext( _tableStorageProvider );

         var itemToUpdate = await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );
         string updatedFirstType = "Updated";
         itemToUpdate.FirstType = updatedFirstType;

         _tableStorageProvider.Update( _tableName, itemToUpdate, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var resultingItem = await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( updatedFirstType, resultingItem.FirstType );
      }

      // This test ensures retreiving and updating dynamic entities is 
      // backwards compatible with the optimistic concurrency update
      [TestMethod]
      public async Task Update_MultipleUpdatesFromSingleDynamicEntity_SucceedsRegardlessIfEntityHasBeenChanged()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 33
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;

         var storedItem = await _tableStorageProvider.GetAsync( _tableName, "foo", "bar" );

         storedItem.Age = 44;
         _tableStorageProvider.Update( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();

         storedItem.Age = 39;
         _tableStorageProvider.Update( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();
      }

      [TestMethod]
      public async Task Update_ExistingItemIsUpdatedInOneInstanceAndNotSaved_ShouldBeUnaffectedInOtherInstances()
      {
         var secondStorageProvider = new InMemoryTableStorageProvider();
         var item = new SimpleDataItem
                          {
                             FirstType = "first"
                          };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         item.FirstType = "second";
         _tableStorageProvider.Update( _tableName, item, _partitionKey, _rowKey );

         var result = await secondStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( "first", result.FirstType );
      }

      [TestMethod]
      public async Task GetAsync_AddingItemWithNotSerializedProperty_RetrievedItemMissingProperty()
      {
         var dataItem = new SimpleItemWithDontSerializeAttribute
                        {
                           SerializedString = "foo",
                           NotSerializedString = "bar"
                        };

         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var gotItem = await _tableStorageProvider.GetAsync<SimpleItemWithDontSerializeAttribute>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( null, gotItem.NotSerializedString );
         Assert.AreEqual( dataItem.SerializedString, gotItem.SerializedString );
      }

      [TestMethod]
      public async Task GetAsync_ItemWithETagPropertyInStore_ItemReturnedWithETag()
      {
         var decoratedETagItem = new DecoratedItemWithETag
         {
            Id = "someId",
            Name = "someName",
            Age = 12,
         };

         _tableStorageProvider.Add( _tableName, decoratedETagItem );
         await _tableStorageProvider.SaveAsync();

         var actualItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "someId", "someName" );
         Assert.IsNotNull( actualItem.ETag );
      }

      [TestMethod]
      public async Task GetAsync_RetreiveAsDynamic_DynamicItemHasETagProperty()
      {
         var decoratedItem = new DecoratedItem
         {
            Id = "id",
            Name = "name",
            Age = 33
         };

         _tableStorageProvider.Add( _tableName, decoratedItem );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = true;

         var actualItem = await _tableStorageProvider.GetAsync( _tableName, "id", "name" );
         var itemAsDict = actualItem as IDictionary<string, object>;
         Assert.IsTrue( itemAsDict.ContainsKey( "ETag" ) );
      }

      [TestMethod]
      public async Task Add_ClassWithPropertyOfTypeThatHasDontSerializeAttribute_DoesNotSerializeThatProperty()
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
         await _tableStorageProvider.SaveAsync();

         var resultItem = await _tableStorageProvider.GetAsync<SimpleClassContainingTypeWithDontSerializeAttribute>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( null, resultItem.ThingWithDontSerializeAttribute );
         Assert.AreEqual( newItem.StringWithoutDontSerializeAttribute, resultItem.StringWithoutDontSerializeAttribute );
      }

      [TestMethod]
      public async Task Update_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         var itemToUpdate = new SimpleDataItem
                            {
                               FirstType = "First",
                               SecondType = 2
                            };

         itemToUpdate.FirstType = "Do not care";

         _tableStorageProvider.Update( _tableName, itemToUpdate, _partitionKey, _rowKey );
         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task SaveAsync_TwoTablesHaveBeenWrittenTo_ShouldSaveBoth()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         _tableStorageProvider.Add( "firstTable", simpleItem, _partitionKey, _rowKey );

         _tableStorageProvider.Add( "secondTable", simpleItem, _partitionKey, _rowKey );

         await _tableStorageProvider.SaveAsync();

         var itemOne = await _tableStorageProvider.GetAsync<SimpleDataItem>( "firstTable", _partitionKey, _rowKey );
         var itemTwo = await _tableStorageProvider.GetAsync<SimpleDataItem>( "secondTable", _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, itemOne.FirstType );
         Assert.AreEqual( simpleItem.FirstType, itemTwo.FirstType );
      }

      [TestMethod]
      public async Task GetAsync_AddItemToOneTableAndReadFromAnother_ItemIsNotReturnedFromSecondTable()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };
         _tableStorageProvider.Add( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         string differentTableName = "hash";

         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.GetAsync<SimpleDataItem>( differentTableName, _partitionKey, _rowKey ) );
      }

      [TestMethod]
      public async Task Upsert_MultipleUpserts_UpdatesItem()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "second";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "third";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "fourth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "fifth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "umpteenth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var actualDataItem = await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      public async Task Upsert_MultipleUpsertsAndCallingSaveAtTheEnd_UpdatesItem()
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

         await _tableStorageProvider.SaveAsync();

         var actualDataItem = await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      public async Task Upsert_MultipleUpsertsWithoutCallingSave_CallingGetThrowsEntityDoesNotExistException()
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

         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey ) );
      }

      [TestMethod]
      public async Task Upsert_MultipleItemsExist_UpdateSpecificItem()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "second";
         _tableStorageProvider.Upsert( _tableName, simpleItem, "DONTCARE1", "DONTCARE2" );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "third";
         _tableStorageProvider.Upsert( _tableName, simpleItem, "DONTCARE3", "DONTCARE4" );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "fourth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, "DONTCARE5", "DONTCARE6" );
         await _tableStorageProvider.SaveAsync();

         simpleItem.FirstType = "fifth";
         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var actualDataItem = await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      public async Task Upsert_UpsertAndCallingSaveAfterTryingToReadFromTheTable_ShouldActuallyInsert()
      {
         var simpleItem = new SimpleDataItem
         {
            FirstType = "first"
         };

         try
         {
            await _tableStorageProvider.GetAsync<SimpleDataItem>( _tableName, "DoNotCare", "DoNotCare" );
         }
         catch ( EntityDoesNotExistException )
         {
         }


         _tableStorageProvider.Upsert( _tableName, simpleItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var inMemoryTableStorageProvider = new InMemoryTableStorageProvider();
         var actualDataItem = await inMemoryTableStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( simpleItem.FirstType, actualDataItem.FirstType );
      }

      [TestMethod]
      public async Task Upsert_ExistingItemIsUpsertedInOneInstanceAndNotSaved_ShouldBeUnaffectedInOtherInstances()
      {
         var secondStorageProvider = new InMemoryTableStorageProvider();
         var item = new SimpleDataItem
                          {
                             FirstType = "first"
                          };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         item.FirstType = "second";
         _tableStorageProvider.Upsert( _tableName, item, _partitionKey, _rowKey );

         var result = await secondStorageProvider.GetAsync<SimpleDataItem>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( "first", result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Upsert_ItemUpsertedTwiceAndNotAffectedByETag_ETagPropertyGetsUpdatedEachUpsert()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo2",
            Name = "bar2",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var retreivedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo2", "bar2" );

         retreivedItem.Age = 39;
         _tableStorageProvider.Upsert( _tableName, retreivedItem );
         await _tableStorageProvider.SaveAsync();

         var upsertedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo2", "bar2" );
         Assert.AreNotEqual( retreivedItem.ETag, upsertedItem.ETag );

         retreivedItem.Age = 41;
         _tableStorageProvider.Upsert( _tableName, retreivedItem );
         await _tableStorageProvider.SaveAsync();

         var upsertedItem2 = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo2", "bar2" );
         Assert.AreNotEqual( upsertedItem.ETag, upsertedItem2.ETag );
      }

      [TestMethod]
      public async Task Merge_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         _tableStorageProvider.Merge( _tableName, new SimpleDataItem { FirstType = "first" }, "not", "found" );

         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task Merge_ItemExistsAndOnePropertyOverwritten_WrittenPropertyHasNewValueAndUnwrittenPropertiesRetainValues()
      {
         dynamic item = new ExpandoObject();
         item.Height = 50;
         item.Name = "Bill";

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         dynamic update = new ExpandoObject();
         update.Height = 60;
         _tableStorageProvider.Merge( _tableName, update, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         dynamic updatedItem = await _tableStorageProvider.GetAsync( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( 60, updatedItem.Height );
         Assert.AreEqual( item.Name, updatedItem.Name );
      }

      [TestMethod]
      public async Task Merge_DynamicItemHasOutdatedETag_ThrowsEntityHasBeenChangedException()
      {
         dynamic item = new ExpandoObject();
         item.Height = 50;
         item.Name = "Bill";

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = true;
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;

         var retreivedItem = await _tableStorageProvider.GetAsync( _tableName, _partitionKey, _rowKey );
         retreivedItem.Height = 66;
         _tableStorageProvider.Merge( _tableName, retreivedItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var tmp = await _tableStorageProvider.GetAsync( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 66, tmp.Height );
         Assert.AreNotEqual( retreivedItem.ETag, tmp.ETag );

         retreivedItem.Height = 70;
         _tableStorageProvider.Merge( _tableName, retreivedItem, _partitionKey, _rowKey );

         await AsyncAssert.ThrowsAsync<EntityHasBeenChangedException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      public async Task Merge_DynamicItemHasOutdatedETagConflictHandlingOverwrite_MergesItem()
      {
         dynamic item = new ExpandoObject();
         item.Height = 50;
         item.Name = "Bill";

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = true;
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;

         var retreivedItem = await _tableStorageProvider.GetAsync( _tableName, _partitionKey, _rowKey );
         retreivedItem.Height = 66;
         _tableStorageProvider.Merge( _tableName, retreivedItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var tmp = await _tableStorageProvider.GetAsync( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 66, tmp.Height );
         Assert.AreNotEqual( retreivedItem.ETag, tmp.ETag );

         retreivedItem.Height = 70;
         _tableStorageProvider.Merge( _tableName, retreivedItem, _partitionKey, _rowKey, ConflictHandling.Overwrite );
         await _tableStorageProvider.SaveAsync();

         var actual = await _tableStorageProvider.GetAsync( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 70, actual.Height );
      }

      [TestMethod]
      public async Task Add_AddingItemWithPropertyWithInternalGetter_WillSerializeTheProperty()
      {
         var item = new TypeWithPropertyWithInternalGetter
         {
            FirstType = "a",
            PropertyWithInternalGetter = 1
         };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithPropertyWithInternalGetter>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( 1, result.PropertyWithInternalGetter );
      }

      [TestMethod]
      public async Task GetCollection_ManyItemsInStore_ShouldBeRetreivedInProperSortedOrder()
      {
         var dataItem1 = new SimpleDataItem { FirstType = "a", SecondType = 1 };
         var dataItem2 = new SimpleDataItem { FirstType = "b", SecondType = 2 };
         var dataItem3 = new SimpleDataItem { FirstType = "c", SecondType = 3 };
         var dataItem4 = new SimpleDataItem { FirstType = "d", SecondType = 4 };

         _tableStorageProvider.Add( _tableName, dataItem1, _partitionKey, "3" );
         _tableStorageProvider.Add( _tableName, dataItem2, _partitionKey, "2" );
         _tableStorageProvider.Add( _tableName, dataItem3, _partitionKey, "1" );
         _tableStorageProvider.Add( _tableName, dataItem4, _partitionKey, "4" );
         await _tableStorageProvider.SaveAsync();

         var listOfItems = ( await _tableStorageProvider.CreateQuery<SimpleDataItem>( _tableName ).PartitionKeyEquals( _partitionKey ).Async() ).ToArray();

         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem1 ), "Making sure item 3 comes before item 1." );
         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem2 ), "Making sure item 3 comes before item 2." );
         Assert.IsTrue( dataItem3.ComesBefore( listOfItems, dataItem4 ), "Making sure item 3 comes before item 4." );

         Assert.IsTrue( dataItem2.ComesBefore( listOfItems, dataItem1 ), "Making sure item 2 comes before item 1." );
         Assert.IsTrue( dataItem2.ComesBefore( listOfItems, dataItem4 ), "Making sure item 2 comes before item 4." );

         Assert.IsTrue( dataItem1.ComesBefore( listOfItems, dataItem4 ), "Making sure item 1 comes before item 4." );
      }

      [TestMethod]
      public async Task GetCollection_ItemsInStoreRetrievedDynamically_ShouldBeRetreived()
      {
         int expectedCount = 5;
         await EnsureItemsInContextAsync( _tableStorageProvider, expectedCount );

         IEnumerable<dynamic> items = await _tableStorageProvider.CreateQuery( _tableName ).Async();

         Assert.AreEqual( expectedCount, items.Count() );
      }

      [TestMethod]
      public async Task WriteOperations_CSharpDateTimeNotCompatibleWithEdmDateTime_StillStoresDateTime()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 ) } );
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 ) } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 ) } );
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 ) } );
         await _tableStorageProvider.SaveAsync();

         var retrievedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( ( DateTime.MinValue + TimeSpan.FromDays( 1000 ) ).Year, retrievedItem.CreationDate.Year );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task WriteOperations_CSharpDateTimeMinValue_DateTimeStoredSuccessfully()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue } );
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue } );
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MinValue } );
         await _tableStorageProvider.SaveAsync();

         var retrievedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( DateTime.MinValue, retrievedItem.CreationDate );
      }

      [TestMethod]
      public async Task WriteOperations_CSharpDateTimeMaxValue_DateTimeStoredSuccessfully()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MaxValue } );
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MaxValue } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MaxValue } );
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime() { Id = "blah", Name = "another blah", CreationDate = DateTime.MaxValue } );
         await _tableStorageProvider.SaveAsync();

         var retrievedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( DateTime.MaxValue, retrievedItem.CreationDate );
      }

      [TestMethod]
      public async Task GetAsync_ItemWithTimestmapPropertyInStore_ItemReturnedWithTimestamp()
      {
         var decoratedTimestampItem = new DecoratedItemWithTimestamp
         {
            Id = "someId",
            Name = "someName",
            Age = 12
         };

         _tableStorageProvider.Add( _tableName, decoratedTimestampItem );
         await _tableStorageProvider.SaveAsync();

         var actualItem = await _tableStorageProvider.GetAsync<DecoratedItemWithTimestamp>( _tableName, "someId", "someName" );
         Assert.IsNotNull( actualItem.Timestamp );
         Assert.IsTrue( actualItem.Timestamp > DateTimeOffset.MinValue );
      }

      [TestMethod]
      public async Task GetAsync_RetreiveAsDynamic_DynamicItemHasTimestampProperty()
      {
         var decoratedItem = new DecoratedItem
         {
            Id = "id",
            Name = "name",
            Age = 33
         };

         _tableStorageProvider.Add( _tableName, decoratedItem );
         await _tableStorageProvider.SaveAsync();

         var actualItem = await _tableStorageProvider.GetAsync( _tableName, "id", "name" );
         var itemAsDict = actualItem as IDictionary<string, object>;
         Assert.IsTrue( itemAsDict.ContainsKey( "Timestamp" ) );
         Assert.IsTrue( actualItem.Timestamp > DateTimeOffset.MinValue );
      }

      private async Task EnsureItemsInContextAsync( TableStorageProvider tableStorageProvider, int count )
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
         await tableStorageProvider.SaveAsync();
      }

      private async Task EnsureOneItemInContext( TableStorageProvider tableStorageProvider )
      {
         var item = new SimpleDataItem
                    {
                       FirstType = "First",
                       SecondType = 2
                    };

         tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await tableStorageProvider.SaveAsync();
      }
   }
}
