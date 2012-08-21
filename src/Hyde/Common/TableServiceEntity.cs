using System;

namespace TechSmith.Hyde.Common
{
   internal class TableServiceEntity
   {
      public TableServiceEntity( string partitionKey, string rowKey )
      {
         PartitionKey = partitionKey;
         RowKey = rowKey;
      }

      public string PartitionKey
      {
         get;
         set;
      }
      public string RowKey
      {
         get;
         set;
      }
      public DateTime TimeStamp
      {
         get;
         set;
      }
   }
}
