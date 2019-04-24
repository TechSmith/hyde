using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde.Common.DataAnnotations;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.IntegrationTest
{
   [TestClass]
   public class CosmosTableStorageProviderTest
   {
      private const string _partitionKey = "pk";
      private const string _rowKey = "rk";

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
         _storageAccount = Configuration.GetTestCosmosStorageAccount();

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
         var storageAccountProvider = Configuration.GetTestCosmosStorageAccount();

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
   }
}
