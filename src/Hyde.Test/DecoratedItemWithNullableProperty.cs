using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Test
{
   /// <summary>
   /// Class with decorated partition and row key properties, for testing purposes.
   /// </summary>
   class DecoratedItemWithNullableProperty
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

      public string Description
      {
         get;
         set;
      }
   }
}
