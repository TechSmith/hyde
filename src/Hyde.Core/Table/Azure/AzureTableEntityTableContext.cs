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

      public IFilterable<dynamic> CreateQuery( string tableName, bool includeETagForDynamic )
      {
         return new AzureDynamicQuery( Table( tableName ), includeETagForDynamic );
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

      public void Update( string tableName, TableItem tableItem, ConflictHandling conflictHandling )
      {
         GenericTableEntity genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         if ( ShouldForceOverwrite( conflictHandling, genericTableEntity ) )
         {
            genericTableEntity.ETag = "*";
         }

         var operation = TableOperation.Replace( genericTableEntity );
         _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.Replace, tableItem.PartitionKey, tableItem.RowKey ) );
      }

      private static bool ShouldForceOverwrite( ConflictHandling conflictHandling, GenericTableEntity genericTableEntity )
      {
         return string.IsNullOrEmpty( genericTableEntity.ETag ) || conflictHandling.Equals( ConflictHandling.Overwrite );
      }

      public void Merge( string tableName, TableItem tableItem, ConflictHandling conflictHandling )
      {
         GenericTableEntity genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         if ( ShouldForceOverwrite( conflictHandling, genericTableEntity ) )
         {
            genericTableEntity.ETag = "*";
         }

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

      public void DeleteItem( string tableName, TableItem tableItem, ConflictHandling conflictHandling )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         if ( ShouldForceOverwrite( conflictHandling, genericTableEntity ) )
         {
            genericTableEntity.ETag = "*";
         }
         var operation = TableOperation.Delete( genericTableEntity );
         _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.Delete, tableItem.PartitionKey, tableItem.RowKey ) );
      }

      public async Task DeleteCollectionAsync( string tableName, string partitionKey )
      {
         var query = new AzureQuery<TableEntity>( Table( tableName ) );
         var entitiesToDelete = await query.PartitionKeyEquals( partitionKey ).Async().ConfigureAwait( false );
         foreach ( var entity in entitiesToDelete )
         {
            var operation = TableOperation.Delete( entity );
            _operations.Enqueue( new ExecutableTableOperation( tableName, operation, TableOperationType.Delete, partitionKey, entity.RowKey ) );
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

      private Task SaveIndividualAsync( IEnumerable<ExecutableTableOperation> operations )
      {
         // We construct a continuation chain of actions, each one asyncnronously executing
         // an operation. This is complicated by the need to bridge the Begin/End style async programming
         // model to the TAP model.

         // For each operation, construct a function that returns a task representing an async execution
         // of that operation. Note that the operation isn't executed until the function is called!.
         var taskFuncs = operations.Select<ExecutableTableOperation, Func<Task>>( op => () => ToTask( op ) ).ToArray();

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

      private Task ToTask( ExecutableTableOperation operation )
      {
         // Adapt the old-style Begin/End async programming model to the new TAP model,
         // with task chaining.
         var table = Table( operation.Table );

         return HandleTableStorageExceptions( TableOperationType.Delete == operation.OperationType, table.ExecuteAsync( operation.Operation, _retriableTableRequest, null ) );
      }

      private static async Task HandleTableStorageExceptions( bool isUnbatchedDelete, Task action )
      {
         try
         {
            await action.ConfigureAwait( false );
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
            if ( ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.PreconditionFailed )
            {
               throw new EntityHasBeenChangedException( "Entity has been changed", ex );
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
         foreach ( var nextOperation in operations )
         {
            // start a new batch if the current batch is full, or if any operation in the current
            // batch conflicts with the next operation.
            if ( batches.Last().Count == 100 || batches.Last().Any( op => !canBatch( op, nextOperation ) ) )
            {
               batches.Add( new List<ExecutableTableOperation>() );
            }
            batches.Last().Add( nextOperation );
         }
         return batches;
      }

      private Task SaveBatchAsync( IEnumerable<ExecutableTableOperation> operations )
      {
         var batches = ValidateAndSplitIntoBatches( operations );
         Func<List<ExecutableTableOperation>, Func<Task>> toAsyncFunc = ops => () => SaveAtomicallyAsync( ops );
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

      private TableBatchOperation ValidateAndCreateBatchOperation( IEnumerable<ExecutableTableOperation> operations, out CloudTable table )
      {
         var operationsList = operations.ToList();
         var partitionKeys = operationsList.Select( op => op.PartitionKey ).Distinct();
         if ( partitionKeys.Count() > 1 )
         {
            throw new InvalidOperationException( "Cannot atomically execute operations on different partitions" );
         }

         var tables = operationsList.Select( op => op.Table ).Distinct().ToList();
         if ( tables.Count() > 1 )
         {
            throw new InvalidOperationException( "Cannot atomically execute operations on multiple tables" );
         }

         var batchOperation = new TableBatchOperation();
         foreach ( var tableOperation in operationsList )
         {
            batchOperation.Add( tableOperation.Operation );
         }
         table = batchOperation.Count == 0 ? null : Table( tables[0] );
         return batchOperation;
      }

      private Task SaveAtomicallyAsync( IEnumerable<ExecutableTableOperation> operations )
      {
         CloudTable table;
         var batchOperation = ValidateAndCreateBatchOperation( operations, out table );
         if ( batchOperation.Count == 0 )
         {
            return CreateCompletedTask();
         }

         return HandleTableStorageExceptions( false, table.ExecuteBatchAsync( batchOperation, _retriableTableRequest, null ) );
      }

      private CloudTable Table( string tableName )
      {

         bool hasSecondaryEndpoint = !string.IsNullOrWhiteSpace( _storageAccount.ReadonlyFallbackTableEndpoint );
         Uri primaryEndpoint = new Uri( _storageAccount.TableEndpoint );
         StorageUri storageUri = hasSecondaryEndpoint ? new StorageUri( primaryEndpoint, new Uri( _storageAccount.ReadonlyFallbackTableEndpoint ) ) : new StorageUri( primaryEndpoint );

         var cloudTableClient = new CloudTableClient( storageUri, _storageAccount.Credentials );

         if ( hasSecondaryEndpoint )
         {
            cloudTableClient.DefaultRequestOptions.LocationMode = LocationMode.PrimaryThenSecondary;
         }
         return cloudTableClient.GetTableReference( tableName );
      }
   }
}