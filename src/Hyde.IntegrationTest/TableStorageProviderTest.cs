using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
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
         public CloudBlockBlob FirstType
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
         _storageAccount = new ConnectionStringCloudStorageAccount( ConfigurationManager.AppSettings["storageConnectionString"] );

         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );

         _client = new CloudTableClient( new Uri( _storageAccount.TableEndpoint ), _storageAccount.Credentials );

         _tableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );

         var table = _client.GetTableReference( _tableName );
         table.Create();
      }

      [TestCleanup]
      public void TestCleanup()
      {
         var table = _client.GetTableReference( _tableName );
         table.Delete();
      }

      [ClassCleanup]
      public static void ClassCleanup()
      {
         var storageAccountProvider = new ConnectionStringCloudStorageAccount( ConfigurationManager.AppSettings["storageConnectionString"] );

         var client = new CloudTableClient( new Uri( storageAccountProvider.TableEndpoint ), storageAccountProvider.Credentials );

         var orphanedTables = client.ListTables( _baseTableName );
         foreach ( var orphanedTableName in orphanedTables )
         {
            var table = client.GetTableReference( orphanedTableName.Name );
            table.DeleteIfExists();
         }
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Constructor_TableDoesntExist_TableIsCreated()
      {
         Assert.IsTrue( _client.GetTableReference( _tableName ).Exists() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddItem_TypeWithSingleStringProperty_ItemAddedToStore()
      {
         var dataItem = new TypeWithStringProperty
         {
            FirstType = "b"
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( "b", result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddItem_TypeWithInheritance_ItemAddedToStore()
      {
         var dataItem = new TypeInherited
         {
            FirstType = "string1",
            SecondType = "string2"
         };

         _tableStorageProvider.Add( _tableName, dataItem, "pk", "rk" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeInherited>( _tableName, "pk", "rk" );
         Assert.AreEqual( "string2", result.SecondType );
      }

      [TestCategory( "Integration" ), TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Get_NoItemsInStore_EntityDoesNotExistExceptionThrown()
      {
         _tableStorageProvider.Get<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Get_ItemInStore_ItemReturned()
      {
         var dataItem = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( dataItem.FirstType, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void GetAsync_ItemInStore_ItemReturnedByTask()
      {
         var dataItem = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( dataItem.FirstType, result.Result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void GetAsync_ItemNotInStore_AccessingResultThrowsEntityDoesNotExistWrappedInAggregateException()
      {
         try
         {
            var result = _tableStorageProvider.GetAsync<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey ).Result;
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( AggregateException e )
         {
            Assert.IsTrue( e.InnerException.GetType() ==  typeof( EntityDoesNotExistException ) );
         }
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Delete_ItemInStore_ItemDeleted()
      {
         var dataItem = new TypeWithStringProperty
         {
            FirstType = "a"
         };

         _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var items = _tableStorageProvider.GetCollection<TypeWithStringProperty>( _tableName, _partitionKey );

         Assert.IsFalse( items.Any() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Delete_ManyItemsInStore_ItemsDeleted()
      {
         for ( var i = 0; i < 1001; i++ )
         {
            var dataItem = new TypeWithStringProperty
            {
               FirstType = "a" + i
            };

            _tableStorageProvider.Add( _tableName, dataItem, _partitionKey, _rowKey + dataItem.FirstType );
         }
         _tableStorageProvider.Save( Execute.InBatches );


         _tableStorageProvider.DeleteCollection( _tableName, _partitionKey );
         _tableStorageProvider.Save( Execute.InBatches );

         var items = _tableStorageProvider.GetCollection<TypeWithStringProperty>( _tableName, _partitionKey );

         Assert.IsFalse( items.Any() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Delete_ItemIsNotInStore_NothingHappens()
      {
         _tableStorageProvider.Delete( _tableName, _partitionKey, _rowKey );
         _tableStorageProvider.Save();
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Delete_TableDoesNotExist_NothingHappens()
      {
         _tableStorageProvider.Delete( "table_that_does_not_exist", _partitionKey, _rowKey );
         _tableStorageProvider.Save();
      }

      [TestCategory( "Integration" ), TestMethod]
      public void GetCollection_NothingInStore_EmptyIEnumerableReturned()
      {
         var result = _tableStorageProvider.GetCollection<TypeWithStringProperty>( _tableName, _partitionKey );
         Assert.AreEqual( 0, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void GetCollection_OneItemInStore_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "a" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetCollection<TypeWithStringProperty>( _tableName, _partitionKey );
         Assert.AreEqual( 1, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void GetCollection_ManyItemsInStore_EnumerableWithManyItemsReturned()
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
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetCollection<TypeWithStringProperty>( _tableName, _partitionKey );
         Assert.AreEqual( 3, result.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void GetCollection_MultiplePartitions_ItemsFromAllPartitionsReturned()
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
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetCollection<DecoratedItem>( _tableName );
         Assert.AreEqual( 3, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void GetRangeByPartitionKey_NothingInStore_EmptyIEnumerableReturned()
      {
         var result = _tableStorageProvider.GetRangeByPartitionKey<TypeWithStringProperty>( _tableName, _partitionKey, _partitionKeyForRange );
         Assert.AreEqual( 0, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void GetRangeByPartitionKey_OneItemInStore_EnumerableWithOneItemReturned()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = "a"
         }, _partitionKey, "a" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByPartitionKey<TypeWithStringProperty>( _tableName, _partitionKey, _partitionKeyForRange );
         Assert.AreEqual( 1, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void GetRangeByPartitionKey_ManyItemsInStoreOneOutsideOfRange_EnumerableWithOneLessThanTheTotalOfItemsReturned()
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
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByPartitionKey<TypeWithStringProperty>( _tableName, _partitionKey, _partitionKey );

         Assert.AreEqual( 2, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleIntProperty_ItemProperlyAddedAndRetrieved()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithIntProperty
         {
            FirstType = 1
         }, _partitionKey, 1.ToString() );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithIntProperty>( _tableName, _partitionKey, 1.ToString() );
         Assert.AreEqual( 1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleDoubleProperty_ItemProperlyAddedAndRetrieved()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithDoubleProperty
         {
            FirstType = 0.1
         }, _partitionKey, 0.1.ToString() );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithDoubleProperty>( _tableName, _partitionKey, 0.1.ToString() );
         Assert.AreEqual( 0.1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleBinaryProperty_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithBinaryProperty
         {
            FirstType = new byte[] { 1, 2, 3, 4 }
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithBinaryProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 1, result.FirstType[0] );
         Assert.AreEqual( 2, result.FirstType[1] );
         Assert.AreEqual( 3, result.FirstType[2] );
         Assert.AreEqual( 4, result.FirstType[3] );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleGuidProperty_ItemProperlyAddedAndRetreived()
      {
         Guid guid = Guid.Empty;
         _tableStorageProvider.Add( _tableName, new TypeWithGuidProperty
         {
            FirstType = guid
         }, _partitionKey, guid.ToString() );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithGuidProperty>( _tableName, _partitionKey, guid.ToString() );
         Assert.AreEqual( guid, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleDateTimeProperty_ItemProperlyAddedAndRetreived()
      {
         var dateTime = new DateTime( 2011, 1, 1, 1, 1, 1, DateTimeKind.Utc );
         _tableStorageProvider.Add( _tableName, new TypeWithDatetimeProperty
         {
            FirstType = dateTime
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithDatetimeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.IsTrue( DateTimesPrettyMuchEqual( dateTime, result.FirstType ) );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleDateTimeProperty_EntityHasLocalDateTime_DateIsRetrievedAsUTCButIsEqual()
      {
         var theDate = new DateTime( 635055151618936589, DateTimeKind.Local );
         var item = new TypeWithDatetimeProperty
         {
            FirstType = theDate
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get<TypeWithDatetimeProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( DateTimeKind.Utc, actual.FirstType.Kind );
         Assert.AreEqual( theDate.ToUniversalTime(), actual.FirstType );  
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleDateTimeOffsetProperty_EntityHasLocalDateTimeStoredInOffset_DateOffsetIsRetrieved()
      {
         var theDateTime = new DateTime( 635055151618936589, DateTimeKind.Local );
         var theDateTimeOffset = new DateTimeOffset( theDateTime );
         var item = new TypeWithDatetimeOffsetProperty
         {
            FirstType = theDateTimeOffset
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var actual = _tableStorageProvider.Get<TypeWithDatetimeOffsetProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( theDateTimeOffset, actual.FirstType );
         Assert.AreEqual( theDateTime, actual.FirstType.LocalDateTime );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleBooleanProperty_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithBooleanProperty
         {
            FirstType = true
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithBooleanProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( true, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleLongProperty_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithLongProperty
         {
            FirstType = 1
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithLongProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleUriProperty_ItemProperlyAddedAndRetreived()
      {
         var value = new Uri( @"http://google.com" );

         _tableStorageProvider.Add( _tableName, new TypeWithUriProperty
         {
            FirstType = value
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithUriProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( value, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullUriProperty_ItemProperlyAddedAndRetreived()
      {
         Uri value = null;

         _tableStorageProvider.Add( _tableName, new TypeWithUriProperty
         {
            FirstType = value
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithUriProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( value, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullablePropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableIntTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableIntTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableIntPropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableIntTypeProperty
         {
            FirstType = 1
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableIntTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( 1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableLongThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableLongTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableIntTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableLongThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableLongTypeProperty
         {
            FirstType = -1
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableLongTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( -1, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableDoublePropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         const double doubleValue = 1.3;

         _tableStorageProvider.Add( _tableName, new TypeWithNullableDoubleTypeProperty
         {
            FirstType = doubleValue
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableDoubleTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( doubleValue, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableDoublePropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableDoubleTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableDoubleTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableGuidPropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         var guid = Guid.NewGuid();

         _tableStorageProvider.Add( _tableName, new TypeWithNullableGuidTypeProperty
         {
            FirstType = guid
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableGuidTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( guid, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableGuidPropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableGuidTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableGuidTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableDateTimePropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         var date = DateTime.UtcNow;

         _tableStorageProvider.Add( _tableName, new TypeWithNullableDateTimeTypeProperty
         {
            FirstType = date
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableDateTimeTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.IsTrue( date - result.FirstType.Value.ToUniversalTime() < TimeSpan.FromMilliseconds( 1 ) );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableDateTimePropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableDateTimeTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableDateTimeTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableBoolPropertyThatIsNotSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableBoolTypeProperty
         {
            FirstType = true
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableBoolTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( true, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithSingleNullableBoolPropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithNullableBoolTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableBoolTypeProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithByteArrayPropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithBinaryProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithBinaryProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithStringPropertyThatIsSetToNull_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( null, result.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddingAndRetreivingTypeWithEnumProperty_ItemProperlyAddedAndRetreived()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithEnumProperty { EnumValue = TypeWithEnumProperty.TheEnum.SecondValue }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithEnumProperty>( _tableName, _partitionKey, _rowKey );
         Assert.AreEqual( TypeWithEnumProperty.TheEnum.SecondValue, result.EnumValue );
      }

      [TestCategory( "Integration" ), TestMethod]
      [ExpectedException( typeof( NotSupportedException ) )]
      public void AddingTypeWithUnsupportedProperty_NotSupportedExceptionThrown()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithUnsupportedProperty(), _partitionKey, _rowKey );

         _tableStorageProvider.Save();
      }

      [TestCategory( "Integration" ), TestMethod]
      [ExpectedException( typeof( EntityAlreadyExistsException ) )]
      public void AddingItemWithDuplicatePartitionAndRowKey_ExceptionThrown()
      {
         var validType = new TypeWithStringProperty
         {
            FirstType = "DoNotCare"
         };

         _tableStorageProvider.Add( _tableName, validType, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         _tableStorageProvider.Add( _tableName, validType, _partitionKey, _rowKey );

         _tableStorageProvider.Save();
      }

      [TestCategory( "Integration" ), TestMethod]
      public void AddMoreThan1000ForContinuationTokens_ContinuationTokensAreHidden()
      {
         for ( int i = 0; i < 1100; i++ )
         {
            _tableStorageProvider.Add( _tableName, new TypeWithIntProperty
            {
               FirstType = i
            }, _partitionKey, i.ToString() );
         }
         _tableStorageProvider.Save( Execute.InBatches );

         var result = _tableStorageProvider.GetCollection<TypeWithIntProperty>( _tableName, _partitionKey );
         Assert.AreEqual( 1100, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Add_ItemHasPartitionAndRowKeyProperties_PartitionAndRowKeyAreCorrectlySaved()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         } );
         _tableStorageProvider.Save();

         var item = _tableStorageProvider.Get<DecoratedItem>( _tableName, "foo", "bar" );

         Assert.AreEqual( "foo", item.Id, "partition key not set" );
         Assert.AreEqual( "bar", item.Name, "row key not set" );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Add_ItemHasPartitionAndRowKeyProperties_PropertiesAreNotSavedTwiceInTableStorage()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem
         {
            Id = "48823",
            Name = "Kovacs",
            Age = 142,
         } );
         _tableStorageProvider.Save();

         var tableServiceContext = _client.GetTableServiceContext();
         var item = ( from e in tableServiceContext.CreateQuery<DecoratedItemEntity>( _tableName )
                      where e.PartitionKey == "48823" && e.RowKey == "Kovacs"
                      select e ).First();

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
      public void Upsert_ItemExistsAndIsThenUpdated_ItemIsProperlyUpdated()
      {
         var itemToUpsert = new TypeWithStringProperty
         {
            FirstType = "first"
         };

         _tableStorageProvider.Upsert( _tableName, itemToUpsert, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );
         itemToUpsert = new TypeWithStringProperty
         {
            FirstType = "second"
         };

         _tableStorageProvider.Upsert( _tableName, itemToUpsert, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var itemInTable = _tableStorageProvider.Get<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( itemToUpsert.FirstType, itemInTable.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Upsert_ItemExistsAndHasPartitionAndRowKeys_ItemIsUpdated()
      {
         var item = new DecoratedItem
         {
            Id = "foo2",
            Name = "bar2",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         _tableStorageProvider.Save();

         var upsertedItem = new DecoratedItem
         {
            Id = "foo2",
            Name = "bar2",
            Age = 34
         };
         _tableStorageProvider.Upsert( _tableName, upsertedItem );
         _tableStorageProvider.Save();

         upsertedItem = _tableStorageProvider.Get<DecoratedItem>( _tableName, "foo2", "bar2" );
         Assert.AreEqual( 34, upsertedItem.Age );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Update_ItemExistsAndUpdateIsValid_ShouldPerformTheUpdate()
      {
         EnsureOneItemInTableStorage();

         var itemToUpdate = _tableStorageProvider.Get<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         string updatedFirstTypeValue = "I am updated";
         itemToUpdate.FirstType = updatedFirstTypeValue;

         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );
         _tableStorageProvider.Update( _tableName, itemToUpdate, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var updatedItem = _tableStorageProvider.Get<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( updatedFirstTypeValue, updatedItem.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Update_ItemExistsAndHasPartitionAndRowKeyProperties_ItemIsUpdated()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         _tableStorageProvider.Save();

         var updatedItem = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 34
         };
         _tableStorageProvider.Update( _tableName, updatedItem );
         _tableStorageProvider.Save();

         updatedItem = _tableStorageProvider.Get<DecoratedItem>( _tableName, "foo", "bar" );
         Assert.AreEqual( 34, updatedItem.Age );
      }

      [TestCategory( "Integration" ), TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Update_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         var itemToUpdate = new TypeWithStringProperty
         {
            FirstType = "first"
         };

         itemToUpdate.FirstType = "Updated";

         _tableStorageProvider.Update( _tableName, itemToUpdate, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         Assert.Fail( "Should have thrown EntityDoesNotExistException" );
      }

      [TestCategory( "Integration" ), TestMethod]
      [ExpectedException( typeof( EntityDoesNotExistException ) )]
      public void Merge_ItemDoesNotExist_ShouldThrowEntityDoesNotExistException()
      {
         _tableStorageProvider.Merge( _tableName, new TypeWithBooleanProperty { FirstType = true }, "not", "found" );
         _tableStorageProvider.Save();
      }

      [TestCategory( "Integration" ), TestMethod]
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

      [TestCategory( "Integration" ), TestMethod]
      public void Add_ItemWithNotSerializedProperty_DoesntSerializeThatProperty()
      {
         var newItem = new SimpleItemWithDontSerializeAttribute
         {
            SerializedString = "foo",
            NotSerializedString = "If You see me later, you lose"
         };

         _tableStorageProvider.Add( _tableName, newItem, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var resultItem = _tableStorageProvider.Get<SimpleItemWithDontSerializeAttribute>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( resultItem.SerializedString, newItem.SerializedString );
         Assert.AreEqual( null, resultItem.NotSerializedString );
      }

      [TestCategory( "Integration" ), TestMethod]
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


      public class SimpleDataItemWithDateTime
      {
         public DateTime DateTimeField
         {
            get;
            set;
         }
      }

      [TestMethod, TestCategory( "Integration" )]
      public void Insert_ItemWithDateTimeField_DateTimeFieldStaysUtc()
      {
         const string partitionKey = "DONTCARE1";
         const string rowKey = "DONTCARE2";

         var itemWithDateTime = new SimpleDataItemWithDateTime();
         itemWithDateTime.DateTimeField = DateTime.UtcNow;

         _tableStorageProvider.Add( _tableName, itemWithDateTime, partitionKey, rowKey );
         _tableStorageProvider.Save();

         var retrievedItem = _tableStorageProvider.Get<SimpleDataItemWithDateTime>( _tableName, partitionKey, rowKey );

         Assert.IsTrue( DateTimesPrettyMuchEqual( itemWithDateTime.DateTimeField, retrievedItem.DateTimeField ) );
         Assert.AreEqual( DateTimeKind.Utc, retrievedItem.DateTimeField.Kind );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void GetRangeByRowKey_ZeroItemsInStore_EnumerableWithNoItemsReturned()
      {
         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void GetRangeByRowKey_OneItemInStoreButDoesntMatchPredicate_EnumerableWithNoItemsReturned()
      {
         var item = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, "there" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void GetRangeByRowKey_OneItemInStore_EnumerableWithNoItemsReturned()
      {
         var item = new TypeWithStringProperty
         {
            FirstType = "a"
         };
         _tableStorageProvider.Add( _tableName, item, _partitionKey, "hithere" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void GetRangeByRowKey_ManyItemsInStore_EnumerableWithAppropriateItemsReturned()
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
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void Update_ThreeSeparateUpdatesOfSameElement_ShouldSucceed()
      {
         var item = new DecoratedItem
         {
            Id = "foo",
            Name = "bar",
            Age = 42
         };
         _tableStorageProvider.Add( _tableName, item );
         _tableStorageProvider.Save();

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

         _tableStorageProvider.Save();

         updatedItem = _tableStorageProvider.Get<DecoratedItem>( _tableName, "foo", "bar" );
         Assert.AreEqual( 22, updatedItem.Age );
      }

      [TestMethod]
      [TestCategory("Integration")]
      public void Save_MultipleOperationsOnSameTable_OperationsExecutedInOrder()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "one" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "two" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "three" } );
         _tableStorageProvider.Save();

         // We can tell the last operation was executed last by
         // setting it up to fail and then verifying that the other two completed.
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "one", Age = 42 } );
         _tableStorageProvider.Delete( _tableName, new DecoratedItem { Id = "123", Name = "three" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "two" } );

         try
         {
            _tableStorageProvider.Save();
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( EntityAlreadyExistsException )
         {
         }

         var results = _tableStorageProvider.GetCollection<DecoratedItem>( _tableName, "123" ).ToList();
         Assert.AreEqual( 2, results.Count() );
         Assert.AreEqual( 42, _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "one" ).Age );
         Assert.IsFalse( results.Any( i => i.Name == "three" ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void SaveAsync_MultipleOperationsIndividually_AllOperationsExecuted()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "one" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "two" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "three" } );
         var task = _tableStorageProvider.SaveAsync();

         task.Wait();

         var results = _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).ToList();
         Assert.AreEqual( 3, results.Count );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void SaveAsync_MultipleOperationsIndividuallyAndSecondFails_FollwingOperationsNotExecuted()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "one" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "two" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "three" } );
         _tableStorageProvider.Save();

         // We can tell the last operation was executed last by
         // setting it up to fail and then verifying that the other two completed.
         _tableStorageProvider.Update( _tableName, new DecoratedItem { Id = "123", Name = "one", Age = 42 } );
         _tableStorageProvider.Delete( _tableName, new DecoratedItem { Id = "123", Name = "three" } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "123", Name = "two" } );

         var task = _tableStorageProvider.SaveAsync();
         try
         {
            task.Wait();
            Assert.Fail( "Should have thrown exception" );
         }
         catch ( AggregateException e )
         {
         }

         var results = _tableStorageProvider.CreateQuery<DecoratedItem>( _tableName ).ToList();
         Assert.AreEqual( 2, results.Count() );
         Assert.AreEqual( 42, _tableStorageProvider.Get<DecoratedItem>( _tableName, "123", "one" ).Age );
         Assert.IsFalse( results.Any( i => i.Name == "three" ) );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void GetCollection_ManyItemsInStore_TakeMethodReturnsProperAmount()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "a" }, _partitionKey, "a" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "b" }, _partitionKey, "b" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "c" }, _partitionKey, "c" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetCollection<TypeWithStringProperty>( _tableName, _partitionKey ).Top( 2 );
         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void GetCollection_ManyItemsInStore_MaxKeyValueAllowsUnboundedRangeQuery()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "a" }, _partitionKey, "\uFFFF" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "b" }, _partitionKey, "\uFFFF\uFFFF" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "c" }, _partitionKey, "\uFFFF\uFFFF\uFFFF" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, "\uFFFF\uFFFF", _tableStorageProvider.MaximumKeyValue );

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      // This test fails on a local emulator but passes against an actual azure storage account.  The correct query is sent by the client, 
      // but incorrect results are received from the emulator.
      public void GetCollection_ManyItemsInStore_MinKeyValueAllowsUnboundedRangeQuery()
      {
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "a" }, _partitionKey, "\u0020" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "b" }, _partitionKey, "\u0020\u0020" );
         _tableStorageProvider.Add( _tableName, new TypeWithStringProperty { FirstType = "c" }, _partitionKey, "\u0020\u0020\u0020" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, _tableStorageProvider.MinimumKeyValue, "\u0020\u0020" );

         Assert.AreEqual( 2, result.Count() );
      }

      [TestMethod]
      [TestCategory( "Integration" )]
      public void Get_EntityIsSerializedWithNullValue_DynamicResponseDoesNotContainNullProperties()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItemWithNullableProperty { Id = "0", Name = "1" } );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get( _tableName, "0", "1" );
         var asDict = result as IDictionary<string, object>;
         Assert.AreEqual( 2, asDict.Count() );
         Assert.IsTrue( asDict.ContainsKey( "PartitionKey" ) );
         Assert.IsTrue( asDict.ContainsKey( "RowKey" ) );
         Assert.IsFalse( asDict.ContainsKey( "Description" ) );
      }

      [TestMethod]
      [TestCategory("Integration")]
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
      [TestCategory("Integration")]
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
      [TestCategory( "Integration" )]
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

      private void EnsureOneItemInTableStorage()
      {
         var item = new TypeWithStringProperty
         {
            FirstType = "first"
         };

         _tableStorageProvider.Add( _tableName, item, _partitionKey, _rowKey );
         _tableStorageProvider.Save();
      }

      private static bool DateTimesPrettyMuchEqual( DateTime dt1, DateTime dt2 )
      {
         return dt1.Year == dt2.Year && dt1.Month == dt2.Month && dt1.Day == dt2.Day && dt1.Hour == dt2.Hour && dt1.Minute == dt2.Minute
                && dt1.Second == dt2.Second && dt1.Kind == dt2.Kind;
      }
   }
}
