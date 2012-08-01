using TechSmith.CloudServices.DataModel.Core;

namespace TechSmith.CloudServices.DataModel.CoreIntegrationTests
{
   /// <summary>
   /// Class with decorated partition and row key properties, for testing purposes.
   /// </summary>
   class DecoratedItem
   {
      [PartitionKey]
      public string Id
      {
         get;
         set;
      }

      [RowKey]
      public string Name
      {
         get;
         set;
      }

      public int Age
      {
         get;
         set;
      }
   }

   class DecoratedItemEntity : Microsoft.WindowsAzure.StorageClient.TableServiceEntity
   {
      public string Id
      {
         get;
         set;
      }

      public string Name
      {
         get;
         set;
      }

      public int Age
      {
         get;
         set;
      }
   }
}
