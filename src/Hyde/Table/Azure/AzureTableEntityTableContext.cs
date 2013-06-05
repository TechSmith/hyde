using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

      public IFilterable<T> CreateQuery<T>( string tableName ) where T : new()
      {
         return new AzureQuery<T>( Table( tableName ) );
      }

      public IFilterable<dynamic> CreateQuery( string tableName )
      {
         return new AzureDynamicQuery( Table( tableName ) );
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

      public Task SaveAsync( Execute executeMethod )
      {
         if ( !_operations.Any() )
         {
            // return completed task
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult( new object() );
            return tcs.Task;
         }

         try
         {
            switch ( executeMethod )
            {
               case Execute.Individually:
               {
                  return SaveIndividualAsync( new List<ExecutableTableOperation>( _operations ) );
               }
               case Execute.InBatches:
               {
                  return SaveBatchAsync( new List<ExecutableTableOperation>( _operations ) );
               }
               case Execute.Atomically:
               {
                  return SaveAtomicallyAsync( new List<ExecutableTableOperation>( _operations ) );
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

      private Task SaveIndividualAsync( IEnumerable<ExecutableTableOperation> operations )
      {
         // We construct a continuation chain of actions, each one asyncnronously executing
         // an operation. This is complicated by the need to bridge the Begin/End style async programming
         // model to the TAP model.

         // For each operation, construct a function that returns a task representing an async execution
         // of that operation. Note that the operation isn't executed until the function is called!.
         var taskFuncs = operations.Select<ExecutableTableOperation,Func<Task>>( o => () => ToTask( o ) ).ToArray();

         // Start asynchronously executing the first operation.
         var priorTask = taskFuncs[0]();

         // Chain the remaining operations onto the first task.
         for ( int i = 1; i < taskFuncs.Length; ++i )
         {
            var taskFuncNum = i;
            // There is no overload of Task.ContinueWith that fits together with
            // Task.FromAsync or TaskCompletionSource. To get the desired behavior,
            // we ContinueWith an action that returns a Task, and then call
            // Task<Task>.Unwrap() to flatten things out.
            // See http://stackoverflow.com/questions/3660760/how-do-i-chain-asynchronous-operations-with-the-task-parallel-library-in-net-4.
            priorTask = priorTask.ContinueWith( t => t.Status == TaskStatus.RanToCompletion
                                                     ? taskFuncs[taskFuncNum]()
                                                     : CreateCompletedTask() )
                                 .Unwrap();
         }
         return priorTask;
      }

      private static Task CreateCompletedTask()
      {
         var taskSource = new TaskCompletionSource<object>();
         taskSource.SetResult( new object() );
         return taskSource.Task;
      }

      private Task ToTask( ExecutableTableOperation op )
      {
         // Adapt the old-style Begin/End async programming model to the new TAP model,
         // with task chaining.
         var table = Table( op.Table );
         var asyncResult = Table( op.Table ).BeginExecute( op.Operation, _retriableTableRequest, null, null, null );
         return Task.Factory.FromAsync( asyncResult,
            r => HandleTableStorageExceptions(
               TableOperationType.Delete == op.OperationType,
               () => table.EndExecute( r ) ) );
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

      private static List<List<ExecutableTableOperation>> ValidateAndSplitIntoBatches(
         IEnumerable<ExecutableTableOperation> operations )
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
         return batches;
      }

      private void SaveBatch( IEnumerable<ExecutableTableOperation> operations )
      {
         foreach ( var batch in ValidateAndSplitIntoBatches( operations ) )
         {
            // No need to use an EGT for a single operation.
            if ( batch.Count == 1 )
            {
               SaveIndividual( new [] { batch[0] } );
               continue;
            }

            CloudTable table;
            var batchOperation = ValidateAndCreateBatchOp( batch, out table );
            ExecuteBatchHandlingExceptions( table, batchOperation );
         }
      }

      private void ExecuteBatchHandlingExceptions( CloudTable table, TableBatchOperation batchOperation )
      {
         HandleTableStorageExceptions( false, () =>
            table.ExecuteBatch( batchOperation, _retriableTableRequest ) );
      }

      private Task SaveBatchAsync( IEnumerable<ExecutableTableOperation> operations )
      {
         var batches = ValidateAndSplitIntoBatches( operations );
         Func<List<ExecutableTableOperation>, Func<Task>> toAsyncFunc = ops =>
            () => SaveAtomicallyAsync(ops);
         var asyncFuncs = batches.Select( toAsyncFunc ).ToList();

         var task = asyncFuncs[0]();
         for ( int i = 1; i < asyncFuncs.Count; ++i )
         {
            var funcNum = i;
            task = task.ContinueWith( t => t.Status == TaskStatus.RanToCompletion
                                           ? asyncFuncs[funcNum]()
                                           : CreateCompletedTask() )
                       .Unwrap();
         }
         return task;
      }

      private void SaveAtomically( IEnumerable<ExecutableTableOperation> ops )
      {
         CloudTable table;
         var batchOp = ValidateAndCreateBatchOp( ops, out table );
         if ( batchOp.Count > 0 )
         {
            ExecuteBatchHandlingExceptions( table, batchOp );
         }
      }

      private TableBatchOperation ValidateAndCreateBatchOp( IEnumerable<ExecutableTableOperation> ops, out CloudTable table )
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
         table = batchOp.Count == 0 ? null : Table( tables[0] );
         return batchOp;
      }

      private Task SaveAtomicallyAsync( IEnumerable<ExecutableTableOperation> ops )
      {
         CloudTable table;
         var batchOp = ValidateAndCreateBatchOp( ops, out table );
         if ( batchOp.Count == 0 )
         {
            return CreateCompletedTask();
         }

         var asyncResult = table.BeginExecuteBatch( batchOp, null, null );

         return Task.Factory.FromAsync( asyncResult,
            result => HandleTableStorageExceptions( false, () =>
               table.EndExecuteBatch( result ) ) );
      }

      private CloudTable Table( string tableName )
      {
         return new CloudTableClient( new Uri( _storageAccount.TableEndpoint ), _storageAccount.Credentials ).GetTableReference( tableName );
      }
   }
}