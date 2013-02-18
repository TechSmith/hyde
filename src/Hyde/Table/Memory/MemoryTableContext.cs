using System;
using System.Collections.Generic;
using System.Linq;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table.Memory
{
   internal class MemoryTableContext : ITableContext
   {
      private class Partition
      {
         private readonly Dictionary<string, GenericTableEntity> _entities = new Dictionary<string, GenericTableEntity>();

         public GenericTableEntity GetEntity( string rowKey )
         {
            lock ( _entities )
            {
               if ( ! _entities.ContainsKey( rowKey ) )
               {
                  throw new EntityDoesNotExistException();
               }
               return _entities[rowKey];
            }
         }

         public void Add( GenericTableEntity entity )
         {
            AzureKeyValidator.ValidatePartitionKey( entity.PartitionKey );
            AzureKeyValidator.ValidateRowKey( entity.RowKey );
            lock ( _entities )
            {
               if ( _entities.ContainsKey( entity.RowKey ) )
               {
                  throw new EntityAlreadyExistsException();
               }
               _entities[entity.RowKey] = entity;
            }
         }

         public void Update( GenericTableEntity entity )
         {
            lock ( _entities )
            {
               if ( !_entities.ContainsKey( entity.RowKey ) )
               {
                  throw new EntityDoesNotExistException();
               }
               _entities[entity.RowKey] = entity;
            }
         }

         public void Upsert( GenericTableEntity entity ) {
            lock ( _entities )
            {
               _entities[entity.RowKey] = entity;
            }
         }

         public void Delete( string rowKey )
         {
            lock ( _entities )
            {
               if ( _entities.ContainsKey( rowKey ) )
               {
                  _entities.Remove( rowKey );
               }
            }
         }

         public IEnumerable<GenericTableEntity> GetAll()
         {
            lock ( _entities )
            {
               return new List<GenericTableEntity>( _entities.Values );
            }
         }

         public Partition DeepCopy()
         {
            var result = new Partition();
            lock ( _entities )
            {
               foreach ( var e in _entities )
               {
                  result._entities.Add( e.Key, e.Value );
               }
            }
            return result;
         }
      }

      private class Table
      {
         private readonly Dictionary<string, Partition> _partitions = new Dictionary<string, Partition>();

         public Partition GetPartition( string partitionKey )
         {
            lock ( _partitions )
            {
               if ( !_partitions.ContainsKey( partitionKey ) )
               {
                  _partitions[partitionKey] = new Partition();
               }
               return _partitions[partitionKey];
            }
         }

         public IEnumerable<Partition> GetAllPartitions()
         {
            lock ( _partitions )
            {
               return new List<Partition>( _partitions.Values );
            }
         }

         public Table DeepCopy()
         {
            var result = new Table();
            lock ( _partitions )
            {
               foreach ( var p in _partitions )
               {
                  result._partitions.Add( p.Key, p.Value.DeepCopy() );
               }
            }
            return result;
         }
      }

      private class StorageAccount
      {
         private readonly Dictionary<string, Table> _tables = new Dictionary<string, Table>();

         public Table GetTable( string tableName )
         {
            lock ( _tables )
            {
               if ( !_tables.ContainsKey( tableName ) )
               {
                  _tables.Add( tableName, new Table() );
               }
               return _tables[tableName];
            }
         }

         public StorageAccount DeepCopy()
         {
            var result = new StorageAccount();
            lock ( _tables )
            {
               foreach ( var tableEntry in _tables )
               {
                  result._tables.Add( tableEntry.Key, tableEntry.Value.DeepCopy() );
               }
            }
            return result;
         }
      }

      private class TableAction
      {
         public Action<StorageAccount> Action
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

         public TableAction( Action<StorageAccount> action, string partitionKey, string rowKey, string tableName )
         {
            Action = action;
            PartitionKey = partitionKey;
            RowKey = rowKey;
            TableName = tableName;
         }
      }

      private static StorageAccount _tables = new StorageAccount();

      private readonly Queue<TableAction> _pendingActions = new Queue<TableAction>();

      public static void ResetAllTables()
      {
         _tables = new StorageAccount();
      }

      public T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         return _tables.GetTable( tableName ).GetPartition( partitionKey ).GetEntity( rowKey ).ConvertTo<T>();
      }

      private IEnumerable<GenericTableEntity> GetEntities( string tableName )
      {
         return _tables.GetTable( tableName ).GetAllPartitions().SelectMany( p => p.GetAll() )
                       .OrderBy( e => e.PartitionKey ).ThenBy( e => e.RowKey );
      }

      public IQuery<T> GetCollection<T>( string tableName ) where T : new()
      {
         return new Query<T>( GetEntities( tableName ).Select( e => e.ConvertTo<T>() ) );
      }

      private IEnumerable<GenericTableEntity> GetEntities( string tableName, string partitionKey)
      {
         return _tables.GetTable( tableName ).GetPartition( partitionKey ).GetAll()
                       .OrderBy( e => e.PartitionKey ).ThenBy( e => e.RowKey );
      }

      public IQuery<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         return new Query<T>( GetEntities( tableName, partitionKey)  .Select( e => e.ConvertTo<T>() ) );
      }

      private IEnumerable<GenericTableEntity> GetEntitiesByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         var entities = _tables.GetTable( tableName ).GetAllPartitions().SelectMany( p => p.GetAll() );
         Func<string,bool> isInRange = pk => pk.CompareTo( partitionKeyLow ) >= 0 && pk.CompareTo( partitionKeyHigh ) <= 0;
         return entities.Where( e => isInRange( e.PartitionKey ) ).OrderBy( e => e.PartitionKey ).ThenBy( e => e.RowKey );
      }

      public IQuery<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return new Query<T>( GetEntitiesByPartitionKey( tableName, partitionKeyLow, partitionKeyHigh ).Select( e => e.ConvertTo<T>() ) );
      }

      private IEnumerable<GenericTableEntity> GetEntitiesByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         var entities = _tables.GetTable( tableName ).GetPartition( partitionKey ).GetAll();
         Func<string, bool> isInRange = rk => rk.CompareTo( rowKeyLow ) >= 0 && rk.CompareTo( rowKeyHigh ) <= 0;
         return entities.Where( e => isInRange( e.RowKey ) ).OrderBy( e => e.RowKey );
      }

      public IQuery<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         return new Query<T>( GetEntitiesByRowKey( tableName, partitionKey, rowKeyLow, rowKeyHigh ).Select( e => e.ConvertTo<T>() ) );
      }

      public dynamic GetItem( string tableName, string partitionKey, string rowKey )
      {
         return _tables.GetTable( tableName ).GetPartition( partitionKey ).GetEntity( rowKey ).ConvertToDynamic();
      }

      public IQuery<dynamic> GetCollection( string tableName )
      {
         return new Query<dynamic>( GetEntities( tableName ).Select( e => e.ConvertToDynamic() ) );
      }

      public IQuery<dynamic> GetCollection( string tableName, string partitionKey )
      {
         return new Query<dynamic>( GetEntities( tableName, partitionKey ).Select( e => e.ConvertToDynamic() ) );
      }

      public IQuery<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         return new Query<dynamic>( GetEntitiesByPartitionKey( tableName, partitionKeyLow, partitionKeyHigh ).Select( e => e.ConvertToDynamic() ) );
      }

      public IQuery<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         return new Query<dynamic>( GetEntitiesByRowKey( tableName, partitionKey, rowKeyLow, rowKeyHigh ).Select( e => e.ConvertToDynamic() ) );
      }

      public void AddNewItem( string tableName, TableItem tableItem )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         Action<StorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Add( genericTableEntity );
         _pendingActions.Enqueue( new TableAction( action, tableItem.PartitionKey, tableItem.RowKey, tableName ) );
      }

      public void Upsert( string tableName, TableItem tableItem )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         Action<StorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Upsert( genericTableEntity );
         _pendingActions.Enqueue( new TableAction( action, tableItem.PartitionKey, tableItem.RowKey, tableName ) );
      }

      public void Update( string tableName, TableItem tableItem )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         Action<StorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Update( genericTableEntity );
         _pendingActions.Enqueue( new TableAction( action, tableItem.PartitionKey, tableItem.RowKey, tableName ) );
      }

      public void DeleteItem( string tableName, string partitionKey, string rowKey )
      {
         Action<StorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( partitionKey ).Delete( rowKey );
         _pendingActions.Enqueue( new TableAction( action, partitionKey, rowKey, tableName ) );
      }

      public void DeleteCollection( string tableName, string partitionKey )
      {
         foreach ( var entity in _tables.GetTable( tableName ).GetPartition( partitionKey ).GetAll() )
         {
            DeleteItem( tableName, partitionKey, entity.RowKey );
         }
      }

      public void Save( Execute executeMethod )
      {
         try
         {
            if ( executeMethod == Execute.Atomically )
            {
               SaveAtomically();
               return;
            }

            foreach ( var action in _pendingActions )
            {
               lock ( _tables )
               {
                  action.Action( _tables );
               }
            }
         }
         finally
         {
            _pendingActions.Clear();
         }
      }

      private void SaveAtomically()
      {
         if ( _pendingActions.Count > 100 )
         {
            throw new InvalidOperationException( "Cannot atomically execute more than 100 operations" );
         }

         var partitionKeys = _pendingActions.Select( op => op.PartitionKey ).Distinct();
         if ( partitionKeys.Count() > 1 )
         {
            throw new InvalidOperationException( "Cannot atomically execute operations on different partitions" );
         }

         var groupedByEntity = _pendingActions.GroupBy( op => Tuple.Create( op.PartitionKey, op.RowKey ) );
         if ( groupedByEntity.Any( g => g.Count() > 1 ) )
         {
            throw new InvalidOperationException( "Cannot atomically execute two operations on the same entity" );
         }

         var tables = _pendingActions.Select( op => op.TableName ).Distinct();
         if ( tables.Count() > 1 )
         {
            throw new InvalidOperationException( "Cannot atomically execute operations on multiple tables" );
         }

         lock ( _tables )
         {
            var resultingTables = _tables.DeepCopy();
            foreach ( var action in _pendingActions )
            {
               action.Action( resultingTables );
            }
            _tables = resultingTables;
         }
      }

      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }
   }
}
