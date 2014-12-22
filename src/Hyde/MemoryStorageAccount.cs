using System;
using System.Collections.Generic;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde
{
   public class MemoryStorageAccount
   {
      internal class Partition
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
			   entity.Timestamp = DateTimeOffset.UtcNow;
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
			   entity.Timestamp = DateTimeOffset.UtcNow;
               _entities[entity.RowKey] = entity;
            }
         }

         private static string GetNewETag()
         {
            return Guid.NewGuid().ToString();
         }

         private bool EntityHasBeenChanged( GenericTableEntity entity )
         {
            var hasETagProperty = !string.IsNullOrEmpty( entity.ETag );
            if ( hasETagProperty && entity.ETag.Equals( "*" ) )
            {
               return false;
            }
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
			   entity.Timestamp = DateTimeOffset.UtcNow;
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
			   currentEntity.Timestamp = DateTimeOffset.UtcNow;
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

         public void Delete( GenericTableEntity entity )
         {
            lock ( _entities )
            {
               if ( _entities.ContainsKey( entity.RowKey ) )
               {
                  if ( EntityHasBeenChanged( entity ) )
                  {
                     throw new EntityHasBeenChangedException();
                  }
                  _entities.Remove( entity.RowKey );
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

      internal class Table
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

      private readonly Dictionary<string, Table> _tables = new Dictionary<string, Table>();

      internal Table GetTable( string tableName )
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

      internal MemoryStorageAccount DeepCopy()
      {
         var result = new MemoryStorageAccount();
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
}
