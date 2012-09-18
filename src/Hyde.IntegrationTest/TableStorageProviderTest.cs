using System;
using System.Configuration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.StorageClient;
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
      private ConnectionStringCloudStorageAccount _storageAccount;

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
         public CloudBlob FirstType
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

      private TableStorageProvider _tableStorageProvider;

      [TestInitialize]
      public void TestInitialize()
      {
         _storageAccount = new ConnectionStringCloudStorageAccount( ConfigurationManager.AppSettings["storageConnectionString"] );

         _tableStorageProvider = new AzureTableStorageProvider( _storageAccount );

         _client = new CloudTableClient( _storageAccount.TableEndpoint,
                                         _storageAccount.Credentials );

         _tableName = _baseTableName + Guid.NewGuid().ToString().Replace( "-", string.Empty );

         _client.CreateTable( _tableName );
      }

      [TestCleanup]
      public void TestCleanup()
      {
         _client.DeleteTableIfExist( _tableName );
      }

      [ClassCleanup]
      public static void ClassCleanup()
      {
         var storageAccountProvider = new ConnectionStringCloudStorageAccount( ConfigurationManager.AppSettings["storageConnectionString"] );

         var client = new CloudTableClient( storageAccountProvider.TableEndpoint,
                                         storageAccountProvider.Credentials );

         var orphanedTables = client.ListTables( _baseTableName );
         foreach ( var orphanedTableName in orphanedTables )
         {
            client.DeleteTableIfExist( orphanedTableName );
         }
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Constructor_TableDoesntExist_TableIsCreated()
      {
         Assert.IsTrue( _client.DoesTableExist( _tableName ) );
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
            _tableStorageProvider.Save();
         }


         _tableStorageProvider.DeleteCollection( _tableName, _partitionKey );
         _tableStorageProvider.Save();

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
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "1", Name = "Jill", Age = 27 } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "2", Name = "Jim", Age = 32 } );
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "3", Name = "Jackie", Age = 12 } );
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
         _tableStorageProvider.Add( _tableName, new TypeWithNullableDateTimeTypeProperty
         {
            FirstType = null
         }, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.Get<TypeWithNullableDateTimeTypeProperty>( _tableName, _partitionKey, _rowKey );
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
            _tableStorageProvider.Save();
         }

         var result = _tableStorageProvider.GetCollection<TypeWithIntProperty>( _tableName, _partitionKey );
         Assert.AreEqual( 1100, result.Count() );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Add_ItemHasPartitionAndRowKeyProperties_PartitionAndRowKeyAreCorrectlySaved()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "foo", Name = "bar", Age = 42 } );
         _tableStorageProvider.Save();

         var item = _tableStorageProvider.Get<DecoratedItem>( _tableName, "foo", "bar" );

         Assert.AreEqual( "foo", item.Id, "partition key not set" );
         Assert.AreEqual( "bar", item.Name, "row key not set" );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Add_ItemHasPartitionAndRowKeyProperties_PropertiesAreNotSavedTwiceInTableStorage()
      {
         _tableStorageProvider.Add( _tableName, new DecoratedItem { Id = "48823", Name = "Kovacs", Age = 142, } );
         _tableStorageProvider.Save();

         var tableServiceContext = _client.GetDataServiceContext();
         var item = ( from e in tableServiceContext.CreateQuery<DecoratedItemEntity>( _tableName )
                      where e.PartitionKey == "48823" && e.RowKey == "Kovacs"
                      select e ).AsTableServiceQuery().First();

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
         itemToUpsert = new TypeWithStringProperty { FirstType = "second" };

         _tableStorageProvider.Upsert( _tableName, itemToUpsert, _partitionKey, _rowKey );
         _tableStorageProvider.Save();

         var itemInTable = _tableStorageProvider.Get<TypeWithStringProperty>( _tableName, _partitionKey, _rowKey );

         Assert.AreEqual( itemToUpsert.FirstType, itemInTable.FirstType );
      }

      [TestCategory( "Integration" ), TestMethod]
      public void Upsert_ItemExistsAndHasPartitionAndRowKeys_ItemIsUpdated()
      {
         var item = new DecoratedItem { Id = "foo2", Name = "bar2", Age = 42 };
         _tableStorageProvider.Add( _tableName, item );
         _tableStorageProvider.Save();

         var upsertedItem = new DecoratedItem { Id = "foo2", Name = "bar2", Age = 34 };
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
         var item = new DecoratedItem { Id = "foo", Name = "bar", Age = 42 };
         _tableStorageProvider.Add( _tableName, item );
         _tableStorageProvider.Save();

         var updatedItem = new DecoratedItem { Id = "foo", Name = "bar", Age = 34 };
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
      public void AddItemWithNotSerializedPropertyDoesntSerializeThatProperty()
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
         var item = new TypeWithStringProperty { FirstType = "a" };
         _tableStorageProvider.Add<TypeWithStringProperty>( _tableName, item, _partitionKey, "there" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 0, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void GetRangeByRowKey_OneItemInStore_EnumerableWithNoItemsReturned()
      {
         var item = new TypeWithStringProperty { FirstType = "a" };
         _tableStorageProvider.Add<TypeWithStringProperty>( _tableName, item, _partitionKey, "hithere" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 1, result.Count() );
      }

      [TestMethod, TestCategory( "Integration" )]
      public void GetRangeByRowKey_ManyItemsInStore_EnumerableWithAppropriateItemsReturned()
      {
         var item1 = new TypeWithStringProperty { FirstType = "a" };
         var item2 = new TypeWithStringProperty { FirstType = "b" };
         var item3 = new TypeWithStringProperty { FirstType = "c" };
         var item4 = new TypeWithStringProperty { FirstType = "d" };

         _tableStorageProvider.Add<TypeWithStringProperty>( _tableName, item1, _partitionKey, "asdf" );
         _tableStorageProvider.Add<TypeWithStringProperty>( _tableName, item2, _partitionKey, "hithere" );
         _tableStorageProvider.Add<TypeWithStringProperty>( _tableName, item3, _partitionKey, "jklh" );
         _tableStorageProvider.Add<TypeWithStringProperty>( _tableName, item4, _partitionKey, "hi" );
         _tableStorageProvider.Save();

         var result = _tableStorageProvider.GetRangeByRowKey<TypeWithStringProperty>( _tableName, _partitionKey, "hi", "hj" );

         Assert.AreEqual( 2, result.Count() );
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

      // TODO: Create a test that leverages filters (such as timestamp > or a Take(10)).
   }
}
