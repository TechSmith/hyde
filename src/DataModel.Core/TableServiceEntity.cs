using System;

namespace TechSmith.CloudServices.DataModel.Core
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
