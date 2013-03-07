using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using TechSmith.Hyde.Common;

namespace TechSmith.Hyde.Table.Azure
{
   internal class AzureTableEntityTableContext : ITableContext
   {
      private readonly ICloudStorageAccount _storageAccount;
      private readonly Queue<ExecutableTableOperation> _operations = new Queue<ExecutableTableOperation>();
      private readonly TableRequestOptions _retriableTableRequest = new TableRequestOptions
      {
         RetryPolicy = new ExponentialRetry( TimeSpan.FromSeconds( 1 ), 4 )
      };

      public AzureTableEntityTableContext( ICloudStorageAccount storageAccount )
      {
         _storageAccount = storageAccount;
      }

      public T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         return Get( tableName, partitionKey, rowKey ).ConvertTo<T>();
      }

      public IQuery<T> GetCollection<T>( string tableName ) where T : new()
      {
         string allPartitionAndRowsFilter = string.Empty;
         return new AzureQuery<T>( Table( tableName ), allPartitionAndRowsFilter );
      }

      public IQuery<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         var allRowsInPartitonFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         return new AzureQuery<T>( Table( tableName ), allRowsInPartitonFilter );
      }

      public IQuery<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         var lowerRangePartitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.GreaterThanOrEqual, partitionKeyLow );
         var higherRangePartitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.LessThanOrEqual, partitionKeyHigh );

         var rangePartitionFilter = TableQuery.CombineFilters( lowerRangePartitionFilter, TableOperators.And, higherRangePartitionFilter );

         return new AzureQuery<T>( Table( tableName ), rangePartitionFilter );
      }

      public IQuery<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         var partitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         var lowerRangeRowFilter = TableQuery.GenerateFilterCondition( "RowKey", QueryComparisons.GreaterThanOrEqual, rowKeyLow );
         var higherRangeRowFilter = TableQuery.GenerateFilterCondition( "RowKey", QueryComparisons.LessThanOrEqual, rowKeyHigh );

         var rangeRowFilter = TableQuery.CombineFilters( lowerRangeRowFilter, TableOperators.And, higherRangeRowFilter );

         var fullRangeFilter = TableQuery.CombineFilters( partitionFilter, TableOperators.And, rangeRowFilter );

         return new AzureQuery<T>( Table( tableName ), fullRangeFilter );
      }

      public dynamic GetItem( string tableName, string partitionKey, string rowKey )
      {
         return Get( tableName, partitionKey, rowKey ).ConvertToDynamic();
      }

      public IQuery<dynamic> GetCollection( string tableName )
      {
         string allPartitionAndRowsFilter = string.Empty;
         return new AzureDynamicQuery( Table( tableName ), allPartitionAndRowsFilter );
      }

      public IQuery<dynamic> GetCollection( string tableName, string partitionKey )
      {
         var allRowsInPartitonFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         return new AzureDynamicQuery( Table( tableName ), allRowsInPartitonFilter );
      }

      public IQuery<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         var lowerRangePartitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.GreaterThanOrEqual, partitionKeyLow );
         var higherRangePartitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.LessThanOrEqual, partitionKeyHigh );

         var rangePartitionFilter = TableQuery.CombineFilters( lowerRangePartitionFilter, TableOperators.And, higherRangePartitionFilter );

         return new AzureDynamicQuery( Table( tableName ), rangePartitionFilter );
      }

      public IQuery<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         var partitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         var lowerRangeRowFilter = TableQuery.GenerateFilterCondition( "RowKey", QueryComparisons.GreaterThanOrEqual, rowKeyLow );
         var higherRangeRowFilter = TableQuery.GenerateFilterCondition( "RowKey", QueryComparisons.LessThanOrEqual, rowKeyHigh );

         var rangeRowFilter = TableQuery.CombineFilters( lowerRangeRowFilter, TableOperators.And, higherRangeRowFilter );

         var fullRangeFilter = TableQuery.CombineFilters( partitionFilter, TableOperators.And, rangeRowFilter );

         return new AzureDynamicQuery( Table( tableName ), fullRangeFilter );
      }

      public void AddNewItem( string tableName, TableItem tableItem )
      {
         GenericTableEntity genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         var operation = TableOperation.Insert( genericTableEntity );
         _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.Insert, tableItem.PartitionKey, tableItem.RowKey ) );
      }

      public void Upsert( string tableName, TableItem tableItem )
      {
         // Upsert does not use an ETag (If-Match header) - http://msdn.microsoft.com/en-us/library/windowsazure/hh452242.aspx
         GenericTableEntity genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         var operation = TableOperation.InsertOrReplace( genericTableEntity );
         _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.InsertOrReplace, tableItem.PartitionKey, tableItem.RowKey ) );
      }

      public void Update( string tableName, TableItem tableItem )
      {
         GenericTableEntity genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         genericTableEntity.ETag = "*";

         var operation = TableOperation.Replace( genericTableEntity );
         _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.Replace, tableItem.PartitionKey, tableItem.RowKey ) );
      }

      public void Merge( string tableName, TableItem tableItem )
      {
         GenericTableEntity genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         genericTableEntity.ETag = "*";

         var operation = TableOperation.Merge( genericTableEntity );
         _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.Merge, tableItem.PartitionKey, tableItem.RowKey ) );
      }

      public void DeleteItem( string tableName, string partitionKey, string rowKey )
      {
         var operation = TableOperation.Delete( new GenericTableEntity
         {
            ETag = "*",
            PartitionKey = partitionKey,
            RowKey = rowKey
         } );
         _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.Delete, partitionKey, rowKey ) );
      }

      public void DeleteCollection( string tableName, string partitionKey )
      {
         var allRowsInPartitonFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         var getAllInPartitionQuery = new TableQuery<TableEntity>().Where( allRowsInPartitonFilter );
         var entitiesToDelete = Table( tableName ).ExecuteQuery( getAllInPartitionQuery );
         foreach ( var entity in entitiesToDelete )
         {
            var operation = TableOperation.Delete( entity );
            _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.Delete, partitionKey, entity.RowKey ) );
         }
      }

      public void Save( Execute executeMethod )
      {
         if ( !_operations.Any() )
         {
            return;
         }

         try
         {
            switch ( executeMethod )
            {
               case Execute.Individually:
               {
                  SaveIndividual( new Queue<ExecutableTableOperation>( _operations ) );
                  break;
               }
               case Execute.InBatches:
               {
                  SaveBatch( new Queue<ExecutableTableOperation>( _operations ) );
                  break;
               }
               case Execute.Atomically:
               {
                  SaveAtomically( new Queue<ExecutableTableOperation>( _operations ) );
                  break;
               }
               default:
               {
                  throw new ArgumentException( "Unsupported execution method: " + executeMethod );
               }
            }
         }
         finally
         {
            _operations.Clear();
         }
      }

      private void SaveIndividual( IEnumerable<ExecutableTableOperation> operations )
      {
         foreach ( var op in operations )
         {
            var operation = op;
            HandleTableStorageExceptions( TableOperationType.Delete == operation.OperationType, () =>
               Table( operation.Table ).Execute( operation.Operation, _retriableTableRequest ) );
         }
      }

      private static void HandleTableStorageExceptions( bool isUnbatchedDelete, Action action )
      {
         try
         {
            action();
         }
         catch ( StorageException ex )
         {
            if ( ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.NotFound && isUnbatchedDelete )
            {
               return;
            }

            if ( ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.BadRequest &&
                 isUnbatchedDelete &&
                 ex.RequestInformation.ExtendedErrorInformation.ErrorCode == "OutOfRangeInput" )
            {
               // The table does not exist.
               return;
            }

            if ( ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.Conflict )
            {
               throw new EntityAlreadyExistsException( "Entity already exists", ex );
            }
            if ( ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.NotFound )
            {
               throw new EntityDoesNotExistException( "Entity does not exist", ex );
            }
            if ( ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.BadRequest )
            {
               throw new InvalidOperationException( "Table storage returned 'Bad Request'", ex );
            }

            throw;
         }
      }

      private void SaveBatch( IEnumerable<ExecutableTableOperation> operations )
      {
         // For two operations to appear in the same batch...
         Func<ExecutableTableOperation, ExecutableTableOperation, bool> canBatch = ( op1, op2 ) =>
            // they must be on the same table
            op1.Table == op2.Table
               // and the same partition
            && op1.PartitionKey == op2.PartitionKey
               // and neither can be a delete,
            && !( op1.OperationType == TableOperationType.Delete || op2.OperationType == TableOperationType.Delete )
               // and the row keys must be different.
            && op1.RowKey != op2.RowKey;

         // Group consecutive batchable operations
         var batches = new List<List<ExecutableTableOperation>> { new List<ExecutableTableOperation>() };
         foreach ( var nextOp in operations )
         {
            // start a new batch if the current batch is full, or if any op in the current
            // batch conflicts with the next op.
            if ( batches.Last().Count == 100 || batches.Last().Any( op => !canBatch( op, nextOp ) ) )
            {
               batches.Add( new List<ExecutableTableOperation>() );
            }
            batches.Last().Add( nextOp );
         }

         foreach ( var batch in batches )
         {
            // No need to use an EGT for a single operation.
            if ( batch.Count == 1 )
            {
               SaveIndividual( new [] { batch[0] } );
               continue;
            }

            var batchOperation = new TableBatchOperation();
            var table = batch.First().Table;
            foreach ( var op in batch )
            {
               batchOperation.Add( op.Operation );
            }
            ExecuteBatchHandlingExceptions( table, batchOperation );
         }
      }

      private void ExecuteBatchHandlingExceptions( string table, TableBatchOperation batchOperation )
      {
         HandleTableStorageExceptions( false, () =>
            Table( table ).ExecuteBatch( batchOperation, _retriableTableRequest ) );
      }

      private void SaveAtomically( IEnumerable<ExecutableTableOperation> ops )
      {
         var operations = ops.ToList();
         var partitionKeys = operations.Select( op => op.PartitionKey ).Distinct();
         if ( partitionKeys.Count() > 1 )
         {
            throw new InvalidOperationException( "Cannot atomically execute operations on different partitions" );
         }

         var tables = operations.Select( op => op.Table ).Distinct().ToList();
         if ( tables.Count() > 1 )
         {
            throw new InvalidOperationException( "Cannot atomically execute operations on multiple tables" );
         }

         var batchOp = new TableBatchOperation();
         foreach ( var op in operations )
         {
            batchOp.Add( op.Operation );
         }
         if ( batchOp.Count > 0 )
         {
            ExecuteBatchHandlingExceptions( tables[0], batchOp );
         }
      }

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      private GenericTableEntity Get( string tableName, string partitionKey, string rowKey )
      {
         var retrieveOperation = TableOperation.Retrieve<GenericTableEntity>( partitionKey, rowKey );

         var result = Table( tableName ).Execute( retrieveOperation, _retriableTableRequest );

         if ( result.Result == null )
         {
            throw new EntityDoesNotExistException( partitionKey, rowKey, null );
         }

         return (GenericTableEntity)result.Result;
      }

      private CloudTable Table( string tableName )
      {
         return new CloudTableClient( new Uri( _storageAccount.TableEndpoint ), _storageAccount.Credentials ).GetTableReference( tableName );
      }
   }
}