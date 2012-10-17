using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table.Memory
{
   internal class MemoryTableContext : ITableContext
   {
      public static ConcurrentDictionary<string, MemoryTable> _memoryTables = new ConcurrentDictionary<string, MemoryTable>();

      private readonly Guid _instanceId = Guid.NewGuid();

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
         AzureKeyValidator.ValidatePartitionKey( partitionKey );
         AzureKeyValidator.ValidateRowKey( rowKey );

         MemoryTable table = GetTable( tableName );

         if ( table.HasEntity( partitionKey, rowKey ) )
         {
            throw new EntityAlreadyExistsException();
         }

         GenericEntity dataToStore = GenericEntity.HydrateFrom( itemToAdd, partitionKey, rowKey );

         table.Add( partitionKey, rowKey, _instanceId, dataToStore );
      }

      public T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         var tableEntry = GetTable( tableName ).Where( partitionKey, rowKey, _instanceId ).FirstOrDefault().Value;

         if ( tableEntry == null )
         {
            throw new EntityDoesNotExistException();
         }

         return HydrateItemFromData<T>( tableEntry.EntryProperties( _instanceId ), partitionKey, rowKey );
      }

      public dynamic GetItem( string tableName, string partitionKey, string rowKey )
      {
         var tableEntry = GetTable( tableName ).Where( partitionKey, rowKey, _instanceId ).FirstOrDefault().Value;

         if ( tableEntry == null )
         {
            throw new EntityDoesNotExistException();
         }

         return HydrateItemFromData( tableEntry.EntryProperties( _instanceId ), partitionKey, rowKey );
      }

      private static dynamic HydrateItemFromData( Dictionary<string, object> dataForItem, string partitionKey, string rowKey )
      {
         dynamic result = new ExpandoObject();
         foreach ( var property in dataForItem )
         {
            ( (IDictionary<string, object>) result ).Add( property.Key, property.Value );
         }
         result.PartitionKey = partitionKey;
         result.RowKey = rowKey;

         return result;
      }


      private static T HydrateItemFromData<T>( Dictionary<string, object> dataForItem, string partitionKey, string rowKey ) where T : new()
      {
         var result = new T();
         var typeToHydrate = result.GetType();

         var partitionKeyProperty = result.FindPropertyDecoratedWith<PartitionKeyAttribute>();
         if ( partitionKeyProperty != null )
         {
            partitionKeyProperty.SetValue( result, partitionKey, null );
         }

         var rowKeyProperty = result.FindPropertyDecoratedWith<RowKeyAttribute>();
         if ( rowKeyProperty != null )
         {
            rowKeyProperty.SetValue( result, rowKey, null );
         }

         foreach ( var key in dataForItem.Keys )
         {
            typeToHydrate.GetProperty( key ).SetValue( result, dataForItem[key], null );
         }

         // TODO: Play with IgnoreMissingProperties == false here... In other words, get mad if properties are missing.

         return result;
      }

      public IEnumerable<T> GetCollection<T>( string tableName ) where T : new()
      {
         return GetTable( tableName ).TableEntries.Select( v => HydrateItemFromData<T>( v.Value.EntryProperties( _instanceId ), v.Key.PartitionKey, v.Key.RowKey ) );
      }

      public IEnumerable<dynamic> GetCollection( string tableName )
      {
         return GetTable( tableName ).TableEntries.Select( v => HydrateItemFromData( v.Value.EntryProperties( _instanceId ), v.Key.PartitionKey, v.Key.RowKey ) );
      }

      public IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         return GetTable( tableName ).Where( partitionKey, _instanceId )
                   .Select( v => HydrateItemFromData<T>( v.Value.EntryProperties( _instanceId ), v.Key.PartitionKey, v.Key.RowKey ) );
      }

      public IEnumerable<dynamic> GetCollection( string tableName, string partitionKey )
      {
         return GetTable( tableName ).Where( partitionKey, _instanceId )
                   .Select( v => HydrateItemFromData( v.Value.EntryProperties( _instanceId ), v.Key.PartitionKey, v.Key.RowKey ) );
      }

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      public IEnumerable<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetTable( tableName ).WhereRangeByPartitionKey( partitionKeyLow, partitionKeyHigh, _instanceId )
                   .Select( v => HydrateItemFromData<T>( v.Value.EntryProperties( _instanceId ), v.Key.PartitionKey, v.Key.RowKey ) );
      }

      public IEnumerable<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         return GetTable( tableName ).WhereRangeByPartitionKey( partitionKeyLow, partitionKeyHigh, _instanceId )
                   .Select( v => HydrateItemFromData( v.Value.EntryProperties( _instanceId ), v.Key.PartitionKey, v.Key.RowKey ) );
      }

      public IEnumerable<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         return GetTable( tableName ).WhereRangeByRowKey( partitionKey, rowKeyLow, rowKeyHigh, _instanceId )
                   .Select( v => HydrateItemFromData<T>( v.Value.EntryProperties( _instanceId ), v.Key.PartitionKey, v.Key.RowKey ) );
      }

      public IEnumerable<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         return GetTable( tableName ).WhereRangeByRowKey( partitionKey, rowKeyLow, rowKeyHigh, _instanceId )
                   .Select( v => HydrateItemFromData( v.Value.EntryProperties( _instanceId ), v.Key.PartitionKey, v.Key.RowKey ) );
      }

      public void Save()
      {
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
         var table = GetTable( tableName );

         if ( table.HasEntity( partitionKey, rowKey ) )
         {
            GenericEntity dataToStore = GenericEntity.HydrateFrom( itemToUpsert, partitionKey, rowKey );
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
         GetItem( tableName, partitionKey, rowKey );

         Upsert( tableName, item, partitionKey, rowKey );
      }
   }
}
