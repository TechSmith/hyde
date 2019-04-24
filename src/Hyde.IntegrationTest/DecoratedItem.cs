using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.IntegrationTest
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

   class DecoratedItemEntity : Microsoft.Azure.Cosmos.Table.TableEntity
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
