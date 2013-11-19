﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table.Memory
{
   internal class MemoryTableContext : ITableContext
   {
      private class Partition
      {
         private readonly Dictionary<string, GenericTableEntity> _entities = new Dictionary<string, GenericTableEntity>();

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
               entity.ETag = GetNewETag();
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
               if ( EntityHasBeenChanged( entity ) )
               {
                  throw new EntityHasBeenChangedException();
               }
               entity.ETag = GetNewETag();
               _entities[entity.RowKey] = entity;
            }
         }

         private static string GetNewETag()
         {
            return DateTime.UtcNow.Ticks.ToString( CultureInfo.InvariantCulture );
         }

         private bool EntityHasBeenChanged( GenericTableEntity entity )
         {
            var hasETagProperty = !string.IsNullOrEmpty( entity.ETag );
            var entityHasChanged = false;
            if ( hasETagProperty )
            {
               entityHasChanged = !entity.ETag.Equals( _entities[entity.RowKey].ETag );
            }
            return entityHasChanged;
         }

         public void Upsert( GenericTableEntity entity )
         {
            lock ( _entities )
            {
               entity.ETag = GetNewETag();
               _entities[entity.RowKey] = entity;
            }
         }

         public void Merge( GenericTableEntity entity )
         {
            lock ( _entities )
            {
               if ( !_entities.ContainsKey( entity.RowKey ) )
               {
                  throw new EntityDoesNotExistException();
               }
               if ( EntityHasBeenChanged( entity ) )
               {
                  throw new EntityHasBeenChangedException();
               }

               var currentEntity = _entities[entity.RowKey];
               foreach ( var property in entity.WriteEntity( null ) )
               {
                  currentEntity.SetProperty( property.Key, property.Value );
               }
               currentEntity.ETag = GetNewETag();
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

      public void Merge( string tableName, TableItem tableItem )
      {
         var genericTableEntity = GenericTableEntity.HydrateFrom( tableItem );
         Action<StorageAccount> action = tables => tables.GetTable( tableName ).GetPartition( tableItem.PartitionKey ).Merge( genericTableEntity );
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
            SaveInternal( executeMethod, _pendingActions );
         }
         finally
         {
            _pendingActions.Clear();
         }
      }

      public Task SaveAsync( Execute executeMethod )
      {
         var actions = new Queue<TableAction>( _pendingActions );
         _pendingActions.Clear();
         return Task.Factory.StartNew( () => SaveInternal( executeMethod, actions ) );
      }

      private static void SaveInternal( Execute executeMethod, Queue<TableAction> actions )
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

      private static void SaveAtomically( Queue<TableAction> actions )
      {
         if ( actions.Count > 100 )
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
