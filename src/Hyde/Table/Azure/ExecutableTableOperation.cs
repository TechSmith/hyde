using Microsoft.WindowsAzure.Storage.Table;

namespace TechSmith.Hyde.Table.Azure
{
   internal class ExecutableTableOperation
   {
      public string Table
      {
         get;
         private set;
      }

      public TableOperation Operation
      {
         get;
         private set;
      }

      public TableOperationType OperationType
      {
         get;
         private set;
      }

      public string PartitionKey
      {
         get;
         private set;
      }

      public ExecutableTableOperation( string table, TableOperation operation, TableOperationType operationType, string partitionKey )
      {
         Table = table;
         Operation = operation;
         OperationType = operationType;
         PartitionKey = partitionKey;
      }
   }
}