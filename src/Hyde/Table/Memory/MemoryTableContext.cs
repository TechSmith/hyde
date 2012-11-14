using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table.Memory
{
   internal class MemoryTableContext : ITableContext
   {
      public static ConcurrentDictionary<string, MemoryTable> _memoryTables = new ConcurrentDictionary<string, MemoryTable>();

      private readonly Guid _instanceId = Guid.NewGuid();
      private Exception _exceptionToThrow;

      public static void ClearTables()
      {
         _memoryTables = new ConcurrentDictionary<string, MemoryTable>();
      }

      private static MemoryTable GetTable( string tableName )
      {
         if ( !_memoryTables.ContainsKey( tableName ) )
         {
            _memoryTables.TryAdd( tableName, new MemoryTable() );
         }

         return _memoryTables[tableName];
      }

      public void AddNewItem( string tableName, dynamic itemToAdd, string partitionKey, string rowKey )
      {
         if ( PendingSaveExceptionExists() )
         {
            return;
         }

         AzureKeyValidator.ValidatePartitionKey( partitionKey );
         AzureKeyValidator.ValidateRowKey( rowKey );

         MemoryTable table = GetTable( tableName );

         if ( table.HasEntity( partitionKey, rowKey ) )
         {
            _exceptionToThrow = new EntityAlreadyExistsException();
            return;
         }

         GenericTableEntity dataToStore = GenericTableEntity.HydrateFrom( itemToAdd, partitionKey, rowKey );

         table.Add( partitionKey, rowKey, _instanceId, dataToStore );
      }

      public T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         var tableEntry = GetTable( tableName ).Where( partitionKey, rowKey, _instanceId ).FirstOrDefault();

         if ( tableEntry.Value == null )
         {
            throw new EntityDoesNotExistException();
         }

         return ConvertTableEntryTo<T>( tableEntry );
      }

      public dynamic GetItem( string tableName, string partitionKey, string rowKey )
      {
         var tableEntry = GetTable( tableName ).Where( partitionKey, rowKey, _instanceId ).FirstOrDefault();

         if ( tableEntry.Value == null )
         {
            throw new EntityDoesNotExistException();
         }

         return ConvertTableEntryToDynamic( tableEntry );
      }

      public IEnumerable<T> GetCollection<T>( string tableName ) where T : new()
      {
         return GetTable( tableName ).TableEntries.Select( ConvertTableEntryTo<T> );
      }

      private T ConvertTableEntryTo<T>( KeyValuePair<TableServiceEntity, MemoryTableEntry> v ) where T : new()
      {
         var genericEntity = new GenericTableEntity
                             {
                                PartitionKey = v.Key.PartitionKey, RowKey = v.Key.RowKey
                             };
         genericEntity.ReadEntity( v.Value.EntryProperties( _instanceId ), null );
         return genericEntity.ConvertTo<T>();
      }

      private dynamic ConvertTableEntryToDynamic( KeyValuePair<TableServiceEntity, MemoryTableEntry> tableEntry )
      {
         var genericEntity = new GenericTableEntity
                             {
                                PartitionKey = tableEntry.Key.PartitionKey, RowKey = tableEntry.Key.RowKey
                             };
         genericEntity.ReadEntity( tableEntry.Value.EntryProperties( _instanceId ), null );
         return genericEntity.ConvertToDynamic();
      }

      public IEnumerable<dynamic> GetCollection( string tableName )
      {
         return GetTable( tableName ).TableEntries.Select( ConvertTableEntryToDynamic );
      }

      public IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         return GetTable( tableName ).Where( partitionKey, _instanceId ).Select( ConvertTableEntryTo<T> );
      }

      public IEnumerable<dynamic> GetCollection( string tableName, string partitionKey )
      {
         return GetTable( tableName ).Where( partitionKey, _instanceId ).Select( ConvertTableEntryToDynamic );
      }

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      public IEnumerable<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetTable( tableName ).WhereRangeByPartitionKey( partitionKeyLow, partitionKeyHigh, _instanceId )
                   .Select( ConvertTableEntryTo<T> );
      }

      public IEnumerable<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         return GetTable( tableName ).WhereRangeByPartitionKey( partitionKeyLow, partitionKeyHigh, _instanceId ).Select( ConvertTableEntryToDynamic );
      }

      public IEnumerable<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         return GetTable( tableName ).WhereRangeByRowKey( partitionKey, rowKeyLow, rowKeyHigh, _instanceId ).Select( ConvertTableEntryTo<T> );
      }

      public IEnumerable<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         return GetTable( tableName ).WhereRangeByRowKey( partitionKey, rowKeyLow, rowKeyHigh, _instanceId ).Select( ConvertTableEntryToDynamic );
      }

      public void Save()
      {
         if ( PendingSaveExceptionExists() )
         {
            throw _exceptionToThrow;
         }

         foreach ( KeyValuePair<string, MemoryTable> table in _memoryTables )
         {
            foreach ( KeyValuePair<TableServiceEntity, MemoryTableEntry> memoryTableEntry in table.Value.TableEntries.ToArray() )
            {
               memoryTableEntry.Value.Save( _instanceId );

               if ( memoryTableEntry.Value.IsDeleted && memoryTableEntry.Value.InstancesWhoHaveDeleted.Contains( _instanceId ) )
               {
                  MemoryTableEntry deletedValue;
                  int attempts = 0;
                  while ( !table.Value.TableEntries.TryRemove( memoryTableEntry.Key, out deletedValue ) )
                  {
                     if ( !table.Value.TableEntries.ContainsKey( memoryTableEntry.Key ) )
                     {
                        break;
                     }

                     if ( attempts++ > 20 )
                     {
                        throw new InvalidOperationException( string.Format( "Failed to delete entry with key {0}", memoryTableEntry.Key ) );
                     }
                  }
               }
            }
         }
      }

      public void Upsert( string tableName, dynamic itemToUpsert, string partitionKey, string rowKey )
      {
         if ( PendingSaveExceptionExists() )
         {
            return;
         }
         var table = GetTable( tableName );

         if ( table.HasEntity( partitionKey, rowKey ) )
         {
            GenericTableEntity dataToStore = GenericTableEntity.HydrateFrom( itemToUpsert, partitionKey, rowKey );
            table.Update( partitionKey, rowKey, _instanceId, dataToStore );
         }
         else
         {
            AddNewItem( tableName, itemToUpsert, partitionKey, rowKey );
         }
      }

      public void DeleteItem( string tableName, string partitionKey, string rowKey )
      {
         var tableEntries = GetTable( tableName );

         var itemToDelete = tableEntries.Where( partitionKey, rowKey, _instanceId ).ToArray();

         foreach ( KeyValuePair<TableServiceEntity, MemoryTableEntry> keyValuePair in itemToDelete )
         {
            keyValuePair.Value.IsDeleted = true;
            keyValuePair.Value.InstancesWhoHaveDeleted.Add( _instanceId );
         }
      }

      public void DeleteCollection( string tableName, string partitionKey )
      {
         var tableEntries = GetTable( tableName );

         var itemToDelete = tableEntries.Where( partitionKey, _instanceId ).ToArray();

         foreach ( KeyValuePair<TableServiceEntity, MemoryTableEntry> keyValuePair in itemToDelete )
         {
            keyValuePair.Value.IsDeleted = true;
            keyValuePair.Value.InstancesWhoHaveDeleted.Add( _instanceId );
         }
      }

      public void Update( string tableName, dynamic item, string partitionKey, string rowKey )
      {
         if ( PendingSaveExceptionExists() )
         {
            return;
         }

         try
         {
            GetItem( tableName, partitionKey, rowKey );
         }
         catch ( EntityDoesNotExistException )
         {
            _exceptionToThrow = new EntityDoesNotExistException();
         }

         Upsert( tableName, item, partitionKey, rowKey );
      }

      private bool PendingSaveExceptionExists()
      {
         return _exceptionToThrow != null;
      }
   }
}
