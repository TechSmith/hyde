using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.IntegrationTest
{
   [TestClass]
   public class TableStorageProviderTest
   {
      private const string _partitionKey = "pk";
      private const string _rowKey = "rk";
      private const string _partitionKeyForRange = "z";

      private static readonly string _baseTableName = "IntegrationTestTable";
      private string _tableName;
      private CloudTableClient _client;
      private ICloudStorageAccount _storageAccount;

      public class TypeWithStringProperty
      {
         public string FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithIntProperty
      {
         public int FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithDoubleProperty
      {
         public double FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithBinaryProperty
      {
         public byte[] FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithGuidProperty
      {
         public Guid FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithDatetimeProperty
      {
         public DateTime FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithDatetimeOffsetProperty
      {
         public DateTimeOffset FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithBooleanProperty
      {
         public bool FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithNullableIntTypeProperty
      {
         public int? FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithNullableLongTypeProperty
      {
         public long? FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithNullableDoubleTypeProperty
      {
         public double? FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithNullableGuidTypeProperty
      {
         public Guid? FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithNullableDateTimeTypeProperty
      {
         public DateTime? FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithNullableBoolTypeProperty
      {
         public bool? FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithLongProperty
      {
         public long FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithUriProperty
      {
         public Uri FirstType
         {
            get;
            set;
         }
      }

      public class TypeWithUnsupportedProperty
      {
         public MemoryStorageAccount FirstType
         {
            get;
            set;
         }
      }

      public class TypeInherited : TypeWithStringProperty
      {
         public string SecondType
         {
            get;
            set;
         }
      }

      public class SimpleItemWithDontSerializeAttribute
      {

         public string SerializedString
         {
            get;
            set;
         }

         [DontSerialize]
         public string NotSerializedString
         {
            get;
            set;
         }
      }

      [DontSerialize]
      public class SimpleTypeWithDontSerializeAttribute
      {
         public string StringWithoutDontSerializeAttribute
         {
            get;
            set;
         }
      }

      public class SimpleClassContainingTypeWithDontSerializeAttribute
      {
         public SimpleTypeWithDontSerializeAttribute ThingWithDontSerializeAttribute
         {
            get;
            set;
         }

         public string StringWithoutDontSerializeAttribute
         {
            get;
            set;
         }
      }

      public class TypeWithEnumProperty
      {
         public TheEnum EnumValue
         {
            get;
            set;
         }

         public enum TheEnum
         {
            FirstValue,
            SecondValue
         }
      }

      private TableStorageProvider _tableStorageProvider;

      [TestInitialize]
      public void TestInitialize()
      {
         _storageAccount = Configuration.GetTestStorageAccount();

         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );

         _client = new CloudTableClient( new Uri( _storageAccount.TableEndpoint ), _storageAccount.Credentials );

         _tableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );

         var table = _client.GetTableReference( _tableName );
         table.CreateAsync().Wait();
      }

      [TestCleanup]
      public void TestCleanup()
      {
         var table = _client.GetTableReference( _tableName );
         table.DeleteAsync().Wait();
      }

      [ClassCleanup]
      public static void ClassCleanup()
      {
         var storageAccountProvider = Configuration.GetTestStorageAccount();

         var client = new CloudTableClient( new Uri( storageAccountProvider.TableEndpoint ), storageAccountProvider.Credentials );

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

      [TestCategory( "Integration" ), TestMethod]
      public async Task Constructor_TableDoesntExist_TableIsCreated()
      {
         Assert.IsTrue( await _client.GetTableReference( _tableName ).ExistsAsync() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddItem_TypeWithSingleStringProperty_ItemAddedToStore()
      {
         var dataItem = new TypeWithStringProperty
         {
            FirstType = "b"
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( "b", result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddItem_TypeWithInheritance_ItemAddedToStore()
      {
         var dataItem = new TypeInherited
         {
            FirstType = "string1",
            SecondType = "string2"
         };

         _tableStorageProvider.Add( _tableName, dataItem, "pk", "rk" );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeInherited>( _tableName, "pk", "rk" );
         Assert.AreEqual( "string2", result.SecondType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetAsync_NoItemsInStore_EntityDoesNotExistExceptionThrown()
      {
         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey ) );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetAsync_ItemInStore_ItemReturned()
      {
         var dataItem = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( dataItem.FirstType, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetAsync_DecoratedItemWithETag_RetreivedItemHasValidETag()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 34
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var retrievedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         Assert.IsNotNull( retrievedItem.ETag );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetAsync_ItemInStore_ItemReturnedByTask()
      {
         var dataItem = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( dataItem.FirstType, result.Result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetAsync_ItemNotInStore_AccessingResultThrowsEntityDoesNotExistWrappedInAggregateException()
      {
         try
         {
            await _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );
         }
         catch ( EntityDoesNotExistException )
         {
            return;
         }
         Assert.Fail( "Should have thrown exception" );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Delete_ItemInStore_ItemDeleted()
      {
         var dataItem = new TypeWithStringProperty
         {
            FirstType = "a"
         };

         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var items = ( await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Async() );

         Assert.IsFalse( items.Any() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Delete_ItemWithETagHasBeenChanged_ThrowsEntityHasBeenChangedException()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 12
         };

         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var storedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         storedItem.Age = 33;
         _tableStorageProvider.Update( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Delete( _tableName, storedItem );

         await AsyncAssert.ThrowsAsync<EntityHasBeenChangedException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Delete_ItemWithETagHasBeenChangedConflictHandlingOverwrite_DeletesItem()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 12
         };

         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var storedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         storedItem.Age = 33;
         _tableStorageProvider.Update( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Delete( _tableName, storedItem, ConflictHandling.Overwrite );
         await _tableStorageProvider.SaveAsync();

         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" ) );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Delete_ItemWithoutETagHasBeenChanged_RespectsTheDelete()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 12
         };

         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var storedItem = await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "foo", "bar" );

         storedItem.Age = 33;
         _tableStorageProvider.Update( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Delete( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();

         var items = ( await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Async() );

         Assert.IsFalse( items.Any() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Delete_ManyItemsInStore_ItemsDeleted()
      {
         for ( var i = 0; i < 1001; i++ )
         {
            var dataItem = new TypeWithStringProperty
            {
               FirstType = "a" + i
            };

            _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey + dataItem.FirstType );
         }
         await _tableStorageProvider.SaveAsync( Execute.InBatches );


         var itemsToDelete = await _tableStorageProvider.CreateQuery( _tableName ).PartitionKeyEquals( _partitionKey ).Async();
         foreach ( var item in itemsToDelete )
         {
            _tableStorageProvider.Delete( _tableName, item.PartitionKey, item.RowKey );
         }
         await _tableStorageProvider.SaveAsync( Execute.InBatches );

         var items = ( await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Async() );

         Assert.IsFalse( items.Any() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Delete_ItemIsNotInStore_NothingHappens()
      {
         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Delete_TableDoesNotExist_NothingHappens()
      {
         _tableStorageProvider.Delete( "tableThatDoesNotExist", _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetCollection_NothingInStore_EmptyIEnumerableReturned()
      {
         var result = ( await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Async() );
         Assert.AreEqual( 0, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetCollection_OneItemInStore_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "a" );
         await _tableStorageProvider.SaveAsync();

         var result = ( await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Async() );
         Assert.AreEqual( 1, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetCollection_ManyItemsInStore_EnumerableWithManyItemsReturned()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "a" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "b"
         }, _partitionKey, "b" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "c"
         }, _partitionKey, "c" );
         await _tableStorageProvider.SaveAsync();

         var result = ( await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Async() );
         Assert.AreEqual( 3, result.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task GetCollection_MultiplePartitions_ItemsFromAllPartitionsReturned()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "1",
            Name = "Jill",
            Age = 27
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "2",
            Name = "Jim",
            Age = 32
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "3",
            Name = "Jackie",
            Age = 12
         } );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).Async();
         Assert.AreEqual( 3, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetRangeByPartitionKey_NothingInStore_EmptyIEnumerableReturned()
      {
         var result = await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyFrom( _partitionKey ).Inclusive().PartitionKeyTo( _partitionKeyForRange ).Inclusive().Async();
         Assert.AreEqual( 0, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetRangeByPartitionKey_OneItemInStore_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "a" );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyFrom( _partitionKey ).Inclusive().PartitionKeyTo( _partitionKeyForRange ).Inclusive().Async();
         Assert.AreEqual( 1, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task GetRangeByPartitionKey_ManyItemsInStoreOneOutsideOfRange_EnumerableWithOneLessThanTheTotalOfItemsReturned()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "a" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "b"
         }, _partitionKey, "b" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "c"
         }, "0", "c" );
         await _tableStorageProvider.SaveAsync();

         var result =
            await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName )
               .PartitionKeyFrom( _partitionKey )
               .Inclusive()
               .PartitionKeyTo( _partitionKey )
               .Inclusive()
               .Async();

         Assert.AreEqual( 2, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleIntProperty_ItemProperlyAddedAndRetrieved()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithIntProperty
         {
            FirstType = 1
         }, _partitionKey, 1.ToString() );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithIntProperty>( _tableName, _partitionKey, 1.ToString() );
         Assert.AreEqual( 1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleDoubleProperty_ItemProperlyAddedAndRetrieved()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithDoubleProperty
         {
            FirstType = 0.1
         }, _partitionKey, 0.1.ToString() );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithDoubleProperty>( _tableName, _partitionKey, 0.1.ToString() );
         Assert.AreEqual( 0.1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleBinaryProperty_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithBinaryProperty
         {
            FirstType = new byte[] { 1, 2, 3, 4 }
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithBinaryProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 1, result.FirstType[0] );
         Assert.AreEqual( 2, result.FirstType[1] );
         Assert.AreEqual( 3, result.FirstType[2] );
         Assert.AreEqual( 4, result.FirstType[3] );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleGuidProperty_ItemProperlyAddedAndRetreived()
      {
         Guid guid = Guid.Empty;
         _tableStorageProvider.Add( _tableName, new TypeWithGuidProperty
         {
            FirstType = guid
         }, _partitionKey, guid.ToString() );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithGuidProperty>( _tableName, _partitionKey, guid.ToString() );
         Assert.AreEqual( guid, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleDateTimeProperty_ItemProperlyAddedAndRetreived()
      {
         var dateTime = new DateTime( 2011, 1, 1, 1, 1, 1, DateTimeKind.Utc );
         _tableStorageProvider.Add( _tableName, new TypeWithDatetimeProperty
         {
            FirstType = dateTime
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithDatetimeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.IsTrue( DateTimesPrettyMuchEqual( dateTime, result.FirstType ) );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleDateTimeProperty_EntityHasLocalDateTime_DateIsRetrievedAsUTCButIsEqual()
      {
         var theDate = new DateTime( 635055151618936589, DateTimeKind.Local );
         var item = new TypeWithDatetimeProperty
         {
            FirstType = theDate
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var actual = await _tableStorageProvider.GetAsync<TypeWithDatetimeProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( DateTimeKind.Utc, actual.FirstType.Kind );
         Assert.AreEqual( theDate.ToUniversalTime(), actual.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleDateTimeOffsetProperty_EntityHasLocalDateTimeStoredInOffset_DateOffsetIsRetrieved()
      {
         var theDateTime = new DateTime( 635055151618936589, DateTimeKind.Local );
         var theDateTimeOffset = new DateTimeOffset( theDateTime );
         var item = new TypeWithDatetimeOffsetProperty
         {
            FirstType = theDateTimeOffset
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var actual = await _tableStorageProvider.GetAsync<TypeWithDatetimeOffsetProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( theDateTimeOffset, actual.FirstType );
         Assert.AreEqual( theDateTime, actual.FirstType.LocalDateTime );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleBooleanProperty_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithBooleanProperty
         {
            FirstType = true
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithBooleanProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( true, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleLongProperty_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithLongProperty
         {
            FirstType = 1
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithLongProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleUriProperty_ItemProperlyAddedAndRetreived()
      {
         var value = new Uri( @"http://google.com" );

         _tableStorageProvider.Add( _tableName, new TypeWithUriProperty
         {
            FirstType = value
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithUriProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( value, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullUriProperty_ItemProperlyAddedAndRetreived()
      {
         Uri value = null;

         _tableStorageProvider.Add( _tableName, new TypeWithUriProperty
         {
            FirstType = value
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithUriProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( value, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullablePropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableIntTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableIntTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableIntPropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableIntTypeProperty
         {
            FirstType = 1
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableIntTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableLongThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableLongTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableIntTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableLongThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableLongTypeProperty
         {
            FirstType = -1
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableLongTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( -1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableDoublePropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         const double doubleValue = 1.3;

         _tableStorageProvider.Add( _tableName, new TypeWithNullableDoubleTypeProperty
         {
            FirstType = doubleValue
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableDoubleTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( doubleValue, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableDoublePropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableDoubleTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableDoubleTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableGuidPropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         var guid = Guid.NewGuid();

         _tableStorageProvider.Add( _tableName, new TypeWithNullableGuidTypeProperty
         {
            FirstType = guid
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableGuidTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( guid, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableGuidPropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableGuidTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableGuidTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableDateTimePropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         var date = DateTime.UtcNow;

         _tableStorageProvider.Add( _tableName, new TypeWithNullableDateTimeTypeProperty
         {
            FirstType = date
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableDateTimeTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.IsTrue( date - result.FirstType.Value.ToUniversalTime() < TimeSpan.FromMilliseconds( 1 ) );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableDateTimePropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableDateTimeTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableDateTimeTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableBoolPropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableBoolTypeProperty
         {
            FirstType = true
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableBoolTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( true, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithSingleNullableBoolPropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableBoolTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithNullableBoolTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithByteArrayPropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithBinaryProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithBinaryProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithStringPropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingAndRetreivingTypeWithEnumProperty_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithEnumProperty
         {
            EnumValue = TypeWithEnumProperty.TheEnum.SecondValue
         }, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<TypeWithEnumProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( TypeWithEnumProperty.TheEnum.SecondValue, result.EnumValue );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingTypeWithUnsupportedProperty_NotSupportedExceptionThrown()
      {
         Assert.ThrowsException<NotSupportedException>( () => _tableStorageProvider.Add( _tableName, new TypeWithUnsupportedProperty(), _partitionKey, _rowKey ) );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddingItemWithDuplicatePartitionAndRowKey_ExceptionThrown()
      {
         var validType = new TypeWithStringProperty
         {
            FirstType = "DoNotCare"
         };

         _tableStorageProvider.Add( _tableName, validType, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.Add( _tableName, validType, _partitionKey, _rowKey );

         await AsyncAssert.ThrowsAsync<EntityAlreadyExistsException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task AddMoreThan1000ForContinuationTokens_ContinuationTokensAreHidden()
      {
         for ( int i = 0; i < 1100; i++ )
         {
            _tableStorageProvider.Add( _tableName, new TypeWithIntProperty
            {
               FirstType = i
            }, _partitionKey, i.ToString() );
         }
         await _tableStorageProvider.SaveAsync( Execute.InBatches );

         var result = await _tableStorageProvider.CreateQuery<TypeWithIntProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Async();
         Assert.AreEqual( 1100, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Add_ItemHasPartitionAndRowKeyProperties_PartitionAndRowKeyAreCorrectlySaved()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         } );
         await _tableStorageProvider.SaveAsync();

         var item = await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "foo", "bar" );

         Assert.AreEqual( "foo", item.Id, "partition key not set" );
         Assert.AreEqual( "bar", item.Name, "row key not set" );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Add_ItemHasPartitionAndRowKeyProperties_PropertiesAreNotSavedTwiceInTableStorage()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "48823",
            Name = "Kovacs",
            Age = 142,
         } );
         await _tableStorageProvider.SaveAsync();

         var table = _client.GetTableReference( _tableName );
         TableResult result = await table.ExecuteAsync( TableOperation.Retrieve<DecoratedItemEntity>( "48823", "Kovacs" ) );
         var item = result.Result as DecoratedItemEntity;

         Assert.IsNull( item.Id );
         Assert.IsNull( item.Name );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Add_ItemHasPropertiesNamedPartitionKeyAndRowKey_ThrowsInvalidEntityException()
      {
         try
         {
            _tableStorageProvider.Add( _tableName, new RowPointer
            {
               Id = "12367",
               PartitionKey = "abba",
               RowKey = "acac"
            } );
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( InvalidEntityException )
         {
         }
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Upsert_ItemExistsAndIsThenUpdated_ItemIsProperlyUpdated()
      {
         var itemToUpsert = new TypeWithStringProperty
         {
            FirstType = "first"
         };

         _tableStorageProvider.Upsert( _tableName, itemToUpsert, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );
         itemToUpsert = new TypeWithStringProperty
         {
            FirstType = "second"
         };

         _tableStorageProvider.Upsert( _tableName, itemToUpsert, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var itemInTable = await _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( itemToUpsert.FirstType, itemInTable.FirstType );
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

      [TestCategory( "Integration" ), TestMethod]
      public async Task Upsert_ItemExistsAndHasPartitionAndRowKeys_ItemIsUpdated()
      {
         var item = new DecoratedItem
         {
            Id = "foo2",
            Name = "bar2",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var upsertedItem = new DecoratedItem
         {
            Id = "foo2",
            Name = "bar2",
            Age = 34
         };
         _tableStorageProvider.Upsert( _tableName, upsertedItem );
         await _tableStorageProvider.SaveAsync();

         upsertedItem = await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "foo2", "bar2" );
         Assert.AreEqual( 34, upsertedItem.Age );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Update_ItemExistsAndUpdateIsValid_ShouldPerformTheUpdate()
      {
         await EnsureOneItemInTableStorageAsync();

         var itemToUpdate = await _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         string updatedFirstTypeValue = "I am updated";
         itemToUpdate.FirstType = updatedFirstTypeValue;

         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );
         _tableStorageProvider.Update( _tableName, itemToUpdate, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var updatedItem = await _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( updatedFirstTypeValue, updatedItem.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Update_ItemExistsAndHasPartitionAndRowKeyProperties_ItemIsUpdated()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var updatedItem = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 34
         };
         _tableStorageProvider.Update( _tableName, updatedItem );
         await _tableStorageProvider.SaveAsync();

         updatedItem = await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "foo", "bar" );
         Assert.AreEqual( 34, updatedItem.Age );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Update_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         var itemToUpdate = new TypeWithStringProperty
         {
            FirstType = "first"
         };

         itemToUpdate.FirstType = "Updated";

         _tableStorageProvider.Update( _tableName, itemToUpdate, _partitionKey, _rowKey );

         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task Update_ClassWithETagAttributeHasAnOldETag_ThrowsEntityHasBeenChangedException()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var updatedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         updatedItem.Age = 22;
         _tableStorageProvider.Update( _tableName, updatedItem );
         await _tableStorageProvider.SaveAsync();

         updatedItem.Age = 33;
         _tableStorageProvider.Update( _tableName, updatedItem );
         await AsyncAssert.ThrowsAsync<EntityHasBeenChangedException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task Update_ClassWithETagAttributeHasAnOldETagConflictHandlingOverwrite_UpdatesItem()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var updatedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         updatedItem.Age = 22;
         _tableStorageProvider.Update( _tableName, updatedItem );
         await _tableStorageProvider.SaveAsync();

         updatedItem.Age = 33;
         _tableStorageProvider.Update( _tableName, updatedItem, ConflictHandling.Overwrite );
         await _tableStorageProvider.SaveAsync();

         var actualItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );
         Assert.AreEqual( 33, actualItem.Age );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task Update_ShouldIncludeETagWithDynamicsIsTrueAndDynamicHasAnETagMismatch_ThrowsEntityHasBeenChangedException()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = true;
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;

         var updatedItem = await _tableStorageProvider.GetAsync( _tableName, "foo", "bar" );

         updatedItem.Age = 22;
         _tableStorageProvider.Update( _tableName, updatedItem );
         await _tableStorageProvider.SaveAsync();

         updatedItem.Age = 33;
         _tableStorageProvider.Update( _tableName, updatedItem );
         await AsyncAssert.ThrowsAsync<EntityHasBeenChangedException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task Update_ShouldIncludeETagWithDynamicsIsFalseAndDynamicHasAnETagMismatch_SuccessfullyExecutesBothUpdates()
      {
         var item = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = false;
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;

         var updatedItem = await _tableStorageProvider.GetAsync( _tableName, "foo", "bar" );

         updatedItem.Age = 22;
         _tableStorageProvider.Update( _tableName, updatedItem );
         await _tableStorageProvider.SaveAsync();

         var storedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );
         Assert.AreEqual( 22, storedItem.Age );

         updatedItem.Age = 33;
         _tableStorageProvider.Update( _tableName, updatedItem );
         await _tableStorageProvider.SaveAsync();

         storedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );
         Assert.AreEqual( 33, storedItem.Age );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Merge_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         _tableStorageProvider.Merge( _tableName, new TypeWithBooleanProperty
         {
            FirstType = true
         }, "not", "found" );
         await AsyncAssert.ThrowsAsync<EntityDoesNotExistException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestCategory( "Integration" ), TestMethod]
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

      [TestCategory( "Integration" ), TestMethod]
      public async Task Merge_ClassHasAnOldETag_ThrowsEntityHasBeenChangedException()
      {
         var decoratedItem = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 24
         };

         _tableStorageProvider.Add( _tableName, decoratedItem );
         await _tableStorageProvider.SaveAsync();

         var storedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         storedItem.Age = 44;
         _tableStorageProvider.Merge( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();

         storedItem.Age = 59;
         _tableStorageProvider.Merge( _tableName, storedItem );
         await AsyncAssert.ThrowsAsync<EntityHasBeenChangedException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Merge_ClassHasAnOldETagConflictHandlingOverwrite_MergesItem()
      {
         var decoratedItem = new DecoratedItemWithETag
         {
            Id = "foo",
            Name = "bar",
            Age = 24
         };

         _tableStorageProvider.Add( _tableName, decoratedItem );
         await _tableStorageProvider.SaveAsync();

         var storedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );

         storedItem.Age = 44;
         _tableStorageProvider.Merge( _tableName, storedItem );
         await _tableStorageProvider.SaveAsync();

         storedItem.Age = 59;
         _tableStorageProvider.Merge( _tableName, storedItem, ConflictHandling.Overwrite );
         await _tableStorageProvider.SaveAsync();

         var actualItem = await _tableStorageProvider.GetAsync<DecoratedItemWithETag>( _tableName, "foo", "bar" );
         Assert.AreEqual( 59, actualItem.Age );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task Add_ItemWithNotSerializedProperty_DoesntSerializeThatProperty()
      {
         var newItem = new SimpleItemWithDontSerializeAttribute
         {
            SerializedString = "foo",
            NotSerializedString = "If You see me later, you lose"
         };

         _tableStorageProvider.Add( _tableName, newItem, _partitionKey, _rowKey );
         await _tableStorageProvider.SaveAsync();

         var resultItem = await _tableStorageProvider.GetAsync<SimpleItemWithDontSerializeAttribute>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( resultItem.SerializedString, newItem.SerializedString );
         Assert.AreEqual( null, resultItem.NotSerializedString );
      }

      [TestCategory( "Integration" ), TestMethod]
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


      public class SimpleDataItemWithDateTime
      {
         public DateTime DateTimeField
         {
            get;
            set;
         }
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task Insert_ItemWithDateTimeField_DateTimeFieldStaysUtc()
      {
         const string partitionKey = "DONTCARE1";
         const string rowKey = "DONTCARE2";

         var itemWithDateTime = new SimpleDataItemWithDateTime();
         itemWithDateTime.DateTimeField = DateTime.UtcNow;

         _tableStorageProvider.Add( _tableName, itemWithDateTime, partitionKey, rowKey );
         await _tableStorageProvider.SaveAsync();

         var retrievedItem = await _tableStorageProvider.GetAsync<SimpleDataItemWithDateTime>( _tableName, partitionKey, rowKey );

         Assert.IsTrue( DateTimesPrettyMuchEqual( itemWithDateTime.DateTimeField, retrievedItem.DateTimeField ) );
         Assert.AreEqual( DateTimeKind.Utc, retrievedItem.DateTimeField.Kind );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetRangeByRowKey_ZeroItemsInStore_EnumerableWithNoItemsReturned()
      {
         var result = await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).RowKeyFrom( "hi" ).Inclusive().RowKeyTo( "hj" ).Inclusive().Async();

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetRangeByRowKey_OneItemInStoreButDoesntMatchPredicate_EnumerableWithNoItemsReturned()
      {
         var item = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, "there" );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).RowKeyFrom( "hi" ).Inclusive().RowKeyTo( "hj" ).Inclusive().Async();

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetRangeByRowKey_OneItemInStore_EnumerableWithNoItemsReturned()
      {
         var item = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, "hithere" );
         await _tableStorageProvider.SaveAsync();

         var result =
            await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName )
               .PartitionKeyEquals( _partitionKey )
               .RowKeyFrom( "hi" )
               .Inclusive()
               .RowKeyTo( "hj" )
               .Inclusive()
               .Async();

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task GetRangeByRowKey_ManyItemsInStore_EnumerableWithAppropriateItemsReturned()
      {
         var item1 = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         var item2 = new TypeWithStringProperty
         {
            FirstType = "b"
         };
         var item3 = new TypeWithStringProperty
         {
            FirstType = "c"
         };
         var item4 = new TypeWithStringProperty
         {
            FirstType = "d"
         };

         _tableStorageProvider.Add( _tableName, item1, _partitionKey, "asdf" );
         _tableStorageProvider.Add( _tableName, item2, _partitionKey, "hithere" );
         _tableStorageProvider.Add( _tableName, item3, _partitionKey, "jklh" );
         _tableStorageProvider.Add( _tableName, item4, _partitionKey, "hi" );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).RowKeyFrom( "hi" ).Inclusive().RowKeyTo( "hj" ).Inclusive().Async();

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public async Task Update_ThreeSeparateUpdatesOfSameElement_ShouldSucceed()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         await _tableStorageProvider.SaveAsync();

         var updatedItem = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 34
         };
         _tableStorageProvider.Update( _tableName, updatedItem );

         updatedItem.Age = 11;
         _tableStorageProvider.Update( _tableName, updatedItem );

         updatedItem.Age = 22;
         _tableStorageProvider.Update( _tableName, updatedItem );

         await _tableStorageProvider.SaveAsync();

         updatedItem = await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "foo", "bar" );
         Assert.AreEqual( 22, updatedItem.Age );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_MultipleOperationsOnSameTable_OperationsExecutedInOrder()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "one"
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "two"
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "three"
         } );
         await _tableStorageProvider.SaveAsync();

         // We can tell the last operation was executed last by
         // setting it up to fail and then verifying that the other two completed.
         _tableStorageProvider.Update( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "one",
            Age = 42
         } );
         _tableStorageProvider.Delete( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "three"
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "two"
         } );

         try
         {
            await _tableStorageProvider.SaveAsync();
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         var results = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).PartitionKeyEquals( "123" ).Async() ).ToList();
         Assert.AreEqual( 2, results.Count() );
         Assert.AreEqual( 42, ( await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "one" ) ).Age );
         Assert.IsFalse( results.Any( i => i.Name == "three" ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_MultipleOperationsIndividually_AllOperationsExecuted()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "one"
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "two"
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "three"
         } );
         await _tableStorageProvider.SaveAsync();

         var results = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).Async() ).ToList();
         Assert.AreEqual( 3, results.Count );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task SaveAsync_MultipleOperationsIndividuallyAndSecondFails_FollwingOperationsNotExecuted()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "one"
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "two"
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "three"
         } );
         await _tableStorageProvider.SaveAsync();

         // We can tell the last operation was executed last by
         // setting it up to fail and then verifying that the other two completed.
         _tableStorageProvider.Update( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "one",
            Age = 42
         } );
         _tableStorageProvider.Delete( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "three"
         } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "123",
            Name = "two"
         } );

         try
         {
            await _tableStorageProvider.SaveAsync();
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         var results = ( await _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).Async() ).ToList();
         Assert.AreEqual( 2, results.Count() );
         Assert.AreEqual( 42, ( await _tableStorageProvider.GetAsync<DecoratedItem>( _tableName, "123", "one" ) ).Age );
         Assert.IsFalse( results.Any( i => i.Name == "three" ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task GetCollection_ManyItemsInStore_TopMethodReturnsProperAmount()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "a" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "b"
         }, _partitionKey, "b" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "c"
         }, _partitionKey, "c" );
         await _tableStorageProvider.SaveAsync();

         var result = ( await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Top( 2 ).Async() );
         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task CreateQueryWithTopAsync_ManyItemsInStore_TopMethodReturnsProperAmount()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "a" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "b"
         }, _partitionKey, "b" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "c"
         }, _partitionKey, "c" );
         await _tableStorageProvider.SaveAsync();

         var result = ( _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Top( 2 ).Async().Result ).ToList();
         Assert.AreEqual( 2, result.Count() );
         Assert.AreEqual( "a", result.First().FirstType );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task GetCollection_ManyItemsInStore_MaxKeyValueAllowsUnboundedRangeQuery()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "\uFFFF" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "b"
         }, _partitionKey, "\uFFFF\uFFFF" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "c"
         }, _partitionKey, "\uFFFF\uFFFF\uFFFF" );
         await _tableStorageProvider.SaveAsync();

         var result =
            await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName )
               .PartitionKeyEquals( _partitionKey )
               .RowKeyFrom( "\uFFFF\uFFFF" )
               .Inclusive()
               .RowKeyTo( _tableStorageProvider.MaximumKeyValue )
               .Inclusive()
               .Async();

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      // This test fails on a local emulator but passes against an actual azure storage account.  The correct query is sent by the client, 
      // but incorrect results are received from the emulator.
      public async Task GetCollection_ManyItemsInStore_MinKeyValueAllowsUnboundedRangeQuery()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "\u0020" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "b"
         }, _partitionKey, "\u0020\u0020" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "c"
         }, _partitionKey, "\u0020\u0020\u0020" );
         await _tableStorageProvider.SaveAsync();

         var result =
            await _tableStorageProvider.CreateQuery<TypeWithStringProperty>( _tableName )
               .PartitionKeyEquals( _partitionKey )
               .RowKeyFrom( _tableStorageProvider.MinimumKeyValue )
               .Inclusive()
               .RowKeyTo( "\u0020\u0020" )
               .Inclusive()
               .Async();

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task GetAsync_EntityIsSerializedWithNullValue_DynamicResponseDoesNotContainNullProperties()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithNullableProperty
         {
            Id = "0",
            Name = "1"
         } );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync( _tableName, "0", "1" );
         var asDict = result as IDictionary<string, object>;
         Assert.AreEqual( 3, asDict.Count() );
         Assert.IsTrue( asDict.ContainsKey( "PartitionKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "RowKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "Timestamp" ) );
         Assert.IsFalse( asDict.ContainsKey( "Description" ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task GetAsync_EntityIsSerialized_DynamicResponseContainsValidTimestampProperty()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "0",
            Name = "1"
         } );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync( _tableName, "0", "1" );
         var asDict = result as IDictionary<string, object>;
         Assert.AreEqual( 4, asDict.Count() );

         var ts = (DateTimeOffset) asDict["Timestamp"];

         Assert.IsNotNull( ts );
         Assert.IsTrue( ts > DateTimeOffset.MinValue );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task GetAsync_EntityIsSerialized_ResponseContainsValidTimestampProperty()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "0",
            Name = "1"
         } );
         await _tableStorageProvider.SaveAsync();

         var result = await _tableStorageProvider.GetAsync<DecoratedItemWithTimestamp>( _tableName, "0", "1" );
         Assert.IsNotNull( result.Timestamp );
         Assert.IsTrue( result.Timestamp > DateTimeOffset.MinValue );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task Set_EntityHasETagAndTimestampAndEtagIsInvalid_Throws()
      {
         _tableStorageProvider.Add( _tableName, new { Id = "0", Name = "1" }, "pk", "rk" );
         await _tableStorageProvider.SaveAsync();

         _tableStorageProvider.ShouldIncludeETagWithDynamics = true;
         _tableStorageProvider.ShouldThrowForReservedPropertyNames = false;
         var dataWithETagAndTimstamp = await _tableStorageProvider.GetAsync( _tableName, "pk", "rk" );
         Assert.IsTrue( dataWithETagAndTimstamp.Timestamp > DateTimeOffset.MinValue, "The Timestamp should have been set on the object when it was retrieved" );


         _tableStorageProvider.Update( _tableName, new { Id = "1", Name = "2" }, "pk", "rk" );
         await _tableStorageProvider.SaveAsync();


         dataWithETagAndTimstamp.Id = "newId";
         _tableStorageProvider.Update( _tableName, dataWithETagAndTimstamp );
         await AsyncAssert.ThrowsAsync<EntityHasBeenChangedException>( () => _tableStorageProvider.SaveAsync() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task WriteOperations_CSharpDateTimeNotCompatibleWithEdmDateTime_StillStoresDateTime()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 )
         } );
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 )
         } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 )
         } );
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MinValue + TimeSpan.FromDays( 1000 )
         } );
         await _tableStorageProvider.SaveAsync();

         var retrievedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( ( DateTime.MinValue + TimeSpan.FromDays( 1000 ) ).Year, retrievedItem.CreationDate.Year );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task WriteOperations_CSharpDateTimeMinValue_DateTimeStoredSuccessfully()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MinValue
         } );
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MinValue
         } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MinValue
         } );
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MinValue
         } );
         await _tableStorageProvider.SaveAsync();

         var retrievedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( DateTime.MinValue, retrievedItem.CreationDate );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public async Task WriteOperations_CSharpDateTimeMaxValue_DateTimeStoredSuccessfully()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MaxValue
         } );
         _tableStorageProvider.Update( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MaxValue
         } );
         _tableStorageProvider.Upsert( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MaxValue
         } );
         _tableStorageProvider.Merge( _tableName, new DecoratedItemWithDateTime()
         {
            Id = "blah",
            Name = "another blah",
            CreationDate = DateTime.MaxValue
         } );
         await _tableStorageProvider.SaveAsync();

         var retrievedItem = await _tableStorageProvider.GetAsync<DecoratedItemWithDateTime>( _tableName, "blah", "another blah" );
         Assert.AreEqual( DateTime.MaxValue, retrievedItem.CreationDate );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task CreateQueryAsync_MultipleBatches_ContinuationsAreHidden()
      {
         for ( int i = 0; i < 1100; i++ )
         {
            _tableStorageProvider.Add( _tableName, new TypeWithIntProperty
            {
               FirstType = i
            }, _partitionKey, i.ToString() );
         }
         await _tableStorageProvider.SaveAsync( Execute.InBatches );

         var result = _tableStorageProvider.CreateQuery<TypeWithIntProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).Async().Result;
         Assert.AreEqual( 1100, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public async Task CreateQueryPartialAsync_MultipleBatches_ContinuationsAreExposed()
      {
         for ( int i = 0; i < 1100; i++ )
         {
            _tableStorageProvider.Add( _tableName, new TypeWithIntProperty
            {
               FirstType = i
            }, _partitionKey, i.ToString() );
         }
         await _tableStorageProvider.SaveAsync( Execute.InBatches );

         var result = _tableStorageProvider.CreateQuery<TypeWithIntProperty>( _tableName ).PartitionKeyEquals( _partitionKey ).PartialAsync().Result;
         Assert.AreEqual( 1000, result.Count() );
         var continuation = result.GetNextAsync().Result;
         Assert.AreEqual( 100, continuation.Count() );
      }

      private Task EnsureOneItemInTableStorageAsync()
      {
         var item = new TypeWithStringProperty
         {
            FirstType = "first"
         };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         return _tableStorageProvider.SaveAsync();
      }

      private static bool DateTimesPrettyMuchEqual( DateTime dt1, DateTime dt2 )
      {
         return dt1.Year == dt2.Year && dt1.Month == dt2.Month && dt1.Day == dt2.Day && dt1.Hour == dt2.Hour && dt1.Minute == dt2.Minute
                && dt1.Second == dt2.Second && dt1.Kind == dt2.Kind;
      }
   }
}
