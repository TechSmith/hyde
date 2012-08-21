using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TechSmith.Hyde.Azure;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Memory
{
   internal class MemoryTableContext : ITableContext
   {
      public static ConcurrentDictionary<string, MemoryTable> _memoryTables = new ConcurrentDictionary<string, MemoryTable>();

      // We store partition/row keys using the following dictionary keys.
      private const string _partitionKeyName = "PartitionKey";
      private const string _rowKeyName = "RowKey";

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

      public void AddNewItem<T>( string tableName, T itemToAdd, string partitionKey, string rowKey ) where T : new()
      {
         AzureKeyValidator.ValidatePartitionKey( partitionKey );
         AzureKeyValidator.ValidateRowKey( rowKey );

         // Hydrate the item, which performs validation that would also be done by the AzureTableContext.
         GenericEntity.HydrateFrom( itemToAdd, partitionKey, rowKey );

         var table = GetTable( tableName );

         if ( table.HasEntity( partitionKey, rowKey ) )
         {
            throw new EntityAlreadyExistsException();
         }

         var dataToStore = SerializeItemToData( itemToAdd );

         table.Add( partitionKey, rowKey, _instanceId, dataToStore );
      }

      private static Dictionary<string, object> SerializeItemToData<T>( T itemToAdd ) where T : new()
      {
         var dataToStore = new Dictionary<string, object>();

         if ( itemToAdd.HasPropertyDecoratedWith<PartitionKeyAttribute>() )
         {
            dataToStore.Add( _partitionKeyName, itemToAdd.ReadPropertyDecoratedWith<PartitionKeyAttribute,string>() );
         }

         if ( itemToAdd.HasPropertyDecoratedWith<RowKeyAttribute>() )
         {
            dataToStore.Add( _rowKeyName, itemToAdd.ReadPropertyDecoratedWith<RowKeyAttribute,string>() );
         }

         foreach ( var propertyToStore in itemToAdd.GetType().GetProperties().Where( p => p.ShouldSerialize() ) )
         {
            dataToStore.Add( propertyToStore.Name, propertyToStore.GetValue( itemToAdd, null ) );
         }
         return dataToStore;
      }

      public T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         var tableEntry = GetTable( tableName ).Where( partitionKey, rowKey, _instanceId ).FirstOrDefault().Value;

         if ( tableEntry == null )
         {
            throw new EntityDoesNotExistException();
         }

         return HydrateItemFromData<T>( tableEntry.EntryProperties( _instanceId ) );
      }

      private static T HydrateItemFromData<T>( Dictionary<string, object> dataForItem ) where T : new()
      {
         var result = new T();
         var typeToHydrate = result.GetType();

         foreach ( var key in dataForItem.Keys )
         {
            switch ( key )
            {
               case _partitionKeyName:
                  result.WritePropertyDecoratedWith<PartitionKeyAttribute,string>( (string)dataForItem[key] );
                  break;
               case _rowKeyName:
                  result.WritePropertyDecoratedWith<RowKeyAttribute,string>( (string)dataForItem[key] );
                  break;
               default:
                  typeToHydrate.GetProperty( key ).SetValue( result, dataForItem[key], null );
                  break;
            }
         }

         // TODO: Play with IgnoreMissingProperties == false here... In other words, get mad if properties are missing.
         //foreach ( var property in typeToHydrate.GetProperties() )
         //{
         //   property.SetValue( result, dataForItem[property.Name], null );
         //}

         return result;
      }

      public IEnumerable<T> GetCollection<T>( string tableName ) where T : new()
      {
         return GetTable( tableName ).TableEntries.Select( v => HydrateItemFromData<T>( v.Value.EntryProperties( _instanceId ) ) );
      }

      public IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         return GetTable( tableName ).Where( partitionKey, _instanceId )
                   .Select( v => HydrateItemFromData<T>( v.Value.EntryProperties( _instanceId ) ) );
      }

      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetTable( tableName ).WhereRange( partitionKeyLow, partitionKeyHigh, _instanceId )
                   .Select( v => HydrateItemFromData<T>( v.Value.EntryProperties( _instanceId ) ) );
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

      public void Upsert<T>( string tableName, T itemToUpsert, string partitionKey, string rowKey ) where T : new()
      {
         var table = GetTable( tableName );

         if ( table.HasEntity( partitionKey, rowKey ) )
         {
            var serializedData = SerializeItemToData( itemToUpsert );
            table.Update( partitionKey, rowKey, _instanceId, serializedData );
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

      public void Update<T>( string tableName, T item, string partitionKey, string rowKey ) where T : new()
      {
         GetItem<T>( tableName, partitionKey, rowKey );

         Upsert( tableName, item, partitionKey, rowKey );
      }
   }
}
