using System.Collections.Generic;

namespace TechSmith.Hyde.Table
{
   internal static class TableConstants
   {
      public const string PartitionKey = "PartitionKey";
      public const string RowKey = "RowKey";
      public const string Timestamp = "Timestamp";
      public const string ETag = "ETag";
      public static readonly HashSet<string> ReservedPropertyNames = new HashSet<string> { PartitionKey, RowKey, Timestamp, ETag };
   }
}
