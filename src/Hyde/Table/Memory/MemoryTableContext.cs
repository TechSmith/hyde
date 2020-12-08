using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table.Memory
{
   internal class MemoryTableContext : ITableContext
   {
      private class TableAction
      {
         public Action<MemoryStorageAccount> Action
         {
            get;
            private set;
         }

         public string PartitionKey
         {
            get;
            private set;
         }

         public string RowKey
         {
            get;
            private set;
         }

         public string TableName
         {
            get;
            private set;
         }

         public TableAction( Action<MemoryStorageAccount> action, string partitionKey, string rowKey, string tableName )
         {
            Action = action;
            PartitionKey = partitionKey;
            RowKey = rowKey;
            TableName = tableName;
         }
      }

      private static readonly MemoryStorageAccount _sharedTables = new MemoryStorageAccount();

      private MemoryStorageAccount _tables;

      private ConcurrentQueue<TableAction> _pendingActions = new ConcurrentQueue<TableAction>();

      public MemoryTableContext( MemoryStorageAccount account = null )
      {
         _tables = account ?? _sharedTables;
      }

      public static void ResetAllTables()
      {
         _sharedTables.Clear();
      }

      public void ResetTables()
      {
         if ( _tables == _sharedTables )
         {
            ResetAllTables();
         }
         else
         {
            _tables.Clear();
         }
      }

      private IEnumerable<GenericTableEntity> GetEntities( string tableName )
      {
         return _tables.GetTable( tableName ).GetAllPartitions().SelectMany( p => p.GetAll() )
                       .OrderBy( e => e.PartitionKey ).ThenBy( e => e.RowKey );
      }

      public IFilterable<T> CreateQuery<T>( string tableName ) where T : new()
      {
         return new MemoryQuery<T>( GetEntities( tableName ) );
      }

      public IFilterable<dynamic> CreateQuery( string tableName, bool shouldIncludeETagForDynamic )
      {
         return (IFilterable<dynamic>) new DynamicMemoryQuery( GetEntities( tableName ), shouldIncludeETagForDynamic );
      }

      public void AddNewItem( string tableName, TableItem tableItem )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         Action<MemoryStorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Add( genericTableEntity );
         _pendingActions.Enqueue( new TableAction( action, tableItem.PartitionKey, tableItem.RowKey, tableName ) );
      }

      public void Upsert( string tableName, TableItem tableItem )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         Action<MemoryStorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Upsert( genericTableEntity );
         _pendingActions.Enqueue( new TableAction( action, tableItem.PartitionKey, tableItem.RowKey, tableName ) );
      }

      public void Update( string tableName, TableItem tableItem, ConflictHandling conflictHandling )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         if ( ShouldForceOverwrite( conflictHandling, genericTableEntity ) )
         {
            genericTableEntity.ETag = "*";
         }
         Action<MemoryStorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Update( genericTableEntity );
         _pendingActions.Enqueue( new TableAction( action, tableItem.PartitionKey, tableItem.RowKey, tableName ) );
      }

      private static bool ShouldForceOverwrite( ConflictHandling conflictHandling, GenericTableEntity genericTableEntity )
      {
         return string.IsNullOrEmpty( genericTableEntity.ETag ) || conflictHandling.Equals( ConflictHandling.Overwrite );
      }

      public void Merge( string tableName, TableItem tableItem, ConflictHandling conflictHandling )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         if ( ShouldForceOverwrite( conflictHandling, genericTableEntity ) )
         {
            genericTableEntity.ETag = "*";
         }
         Action<MemoryStorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Merge( genericTableEntity );
         _pendingActions.Enqueue( new TableAction( action, tableItem.PartitionKey, tableItem.RowKey, tableName ) );
      }

      public void DeleteItem( string tableName, string partitionKey, string rowKey )
      {
         Action<MemoryStorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( partitionKey ).Delete( rowKey );
         _pendingActions.Enqueue( new TableAction( action, partitionKey, rowKey, tableName ) );
      }

      public void DeleteItem( string tableName, TableItem tableItem, ConflictHandling conflictHandling )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         if ( ShouldForceOverwrite( conflictHandling, genericTableEntity ) )
         {
            genericTableEntity.ETag = "*";
         }
         Action<MemoryStorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Delete( genericTableEntity );
         _pendingActions.Enqueue( new TableAction( action, tableItem.PartitionKey, tableItem.RowKey, tableName ) );
      }

      public Task SaveAsync( Execute executeMethod )
      {
         var pendingActions = _pendingActions.ToArray();
         _pendingActions = new ConcurrentQueue<TableAction>();

         SaveInternal( executeMethod, pendingActions );
         return Task.CompletedTask;
      }

      private void SaveInternal( Execute executeMethod, TableAction[] actions )
      {
         if ( executeMethod == Execute.Atomically )
         {
            SaveAtomically( actions );
            return;
         }

         foreach ( var action in actions )
         {
            lock ( _tables )
            {
               action.Action( _tables );
            }
         }
      }

      private void SaveAtomically( TableAction[] actions )
      {
         if ( actions.Count() > 100 )
         {
            throw new InvalidOperationException( "Cannot atomically execute more than 100 operations" );
         }

         var partitionKeys = actions.Select( op => op.PartitionKey ).Distinct();
         if ( partitionKeys.Count() > 1 )
         {
            throw new InvalidOperationException( "Cannot atomically execute operations on different partitions" );
         }

         var groupedByEntity = actions.GroupBy( op => Tuple.Create( op.PartitionKey, op.RowKey ) );
         if ( groupedByEntity.Any( g => g.Count() > 1 ) )
         {
            throw new InvalidOperationException( "Cannot atomically execute two operations on the same entity" );
         }

         var tables = actions.Select( op => op.TableName ).Distinct();
         if ( tables.Count() > 1 )
         {
            throw new InvalidOperationException( "Cannot atomically execute operations on multiple tables" );
         }

         lock ( _tables )
         {
            var resultingTables = _tables.DeepCopy();
            foreach ( var action in actions )
            {
               action.Action( resultingTables );
            }
            _tables = resultingTables;
         }
      }
   }
}
