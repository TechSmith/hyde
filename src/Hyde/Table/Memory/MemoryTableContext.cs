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
      }

      private static StorageAccount _tables = new StorageAccount();

      private readonly Queue<Action<StorageAccount>> _pendingActions = new Queue<Action<StorageAccount>>();

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

      public IEnumerable<T> GetCollection<T>( string tableName ) where T : new()
      {
         return GetEntities( tableName ).Select( e => e.ConvertTo<T>() );
      }

      private IEnumerable<GenericTableEntity> GetEntities( string tableName, string partitionKey)
      {
         return _tables.GetTable( tableName ).GetPartition( partitionKey ).GetAll()
                       .OrderBy( e => e.PartitionKey ).ThenBy( e => e.RowKey );
      }

      public IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         return GetEntities( tableName, partitionKey)  .Select( e => e.ConvertTo<T>() );
      }

      private IEnumerable<GenericTableEntity> GetEntitiesByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         var entities = _tables.GetTable( tableName ).GetAllPartitions().SelectMany( p => p.GetAll() );
         Func<string,bool> isInRange = pk => pk.CompareTo( partitionKeyLow ) >= 0 && pk.CompareTo( partitionKeyHigh ) <= 0;
         return entities.Where( e => isInRange( e.PartitionKey ) ).OrderBy( e => e.PartitionKey ).ThenBy( e => e.RowKey );
      }

      public IEnumerable<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetEntitiesByPartitionKey( tableName, partitionKeyLow, partitionKeyHigh ).Select( e => e.ConvertTo<T>() );
      }

      private IEnumerable<GenericTableEntity> GetEntitiesByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         var entities = _tables.GetTable( tableName ).GetPartition( partitionKey ).GetAll();
         Func<string, bool> isInRange = rk => rk.CompareTo( rowKeyLow ) >= 0 && rk.CompareTo( rowKeyHigh ) <= 0;
         return entities.Where( e => isInRange( e.RowKey ) ).OrderBy( e => e.RowKey );
      }

      public IEnumerable<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         return GetEntitiesByRowKey( tableName, partitionKey, rowKeyLow, rowKeyHigh ).Select( e => e.ConvertTo<T>() );
      }

      public dynamic GetItem( string tableName, string partitionKey, string rowKey )
      {
         return _tables.GetTable( tableName ).GetPartition( partitionKey ).GetEntity( rowKey ).ConvertToDynamic();
      }

      public IEnumerable<dynamic> GetCollection( string tableName )
      {
         return GetEntities( tableName ).Select( e => e.ConvertToDynamic() );
      }

      public IEnumerable<dynamic> GetCollection( string tableName, string partitionKey )
      {
         return GetEntities( tableName, partitionKey ).Select( e => e.ConvertToDynamic() );
      }

      public IEnumerable<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         return GetEntitiesByPartitionKey( tableName, partitionKeyLow, partitionKeyHigh ).Select( e => e.ConvertToDynamic() );
      }

      public IEnumerable<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         return GetEntitiesByRowKey( tableName, partitionKey, rowKeyLow, rowKeyHigh ).Select( e => e.ConvertToDynamic() );
      }

      public void AddNewItem( string tableName, dynamic itemToAdd, string partitionKey, string rowKey )
      {
         var entity = GenericTableEntity.HydrateFrom( itemToAdd, partitionKey, rowKey );
         _pendingActions.Enqueue( tables => tables.GetTable( tableName ).GetPartition( entity.PartitionKey ).Add( entity ) );
      }

      public void Upsert( string tableName, dynamic itemToUpsert, string partitionKey, string rowKey )
      {
         var entity = GenericTableEntity.HydrateFrom( itemToUpsert, partitionKey, rowKey );
         _pendingActions.Enqueue( tables => tables.GetTable( tableName ).GetPartition( entity.PartitionKey ).Upsert( entity ) );
      }

      public void Update( string tableName, dynamic item, string partitionKey, string rowKey )
      {
         var entity = GenericTableEntity.HydrateFrom( item, partitionKey, rowKey );
         _pendingActions.Enqueue( tables => tables.GetTable( tableName ).GetPartition( entity.PartitionKey ).Update( entity ) );
      }

      public void DeleteItem( string tableName, string partitionKey, string rowKey )
      {
         _pendingActions.Enqueue( tables => tables.GetTable( tableName ).GetPartition( partitionKey ).Delete( rowKey ) );
      }

      public void DeleteCollection( string tableName, string partitionKey )
      {
         foreach ( var entity in _tables.GetTable( tableName ).GetPartition( partitionKey ).GetAll() )
         {
            DeleteItem( tableName, partitionKey, entity.RowKey );
         }
      }

      public void Save()
      {
         foreach ( var action in _pendingActions )
         {
            action( _tables );
         }
         _pendingActions.Clear();
      }

      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }
   }
}
