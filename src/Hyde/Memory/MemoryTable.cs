using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TechSmith.Hyde.Common;

namespace TechSmith.Hyde.Memory
{
   internal class MemoryTable
   {
      private readonly ConcurrentDictionary<TableServiceEntity, MemoryTableEntry> _tableEntries = new ConcurrentDictionary<TableServiceEntity, MemoryTableEntry>();

      public ConcurrentDictionary<TableServiceEntity, MemoryTableEntry> TableEntries
      {
         get
         {
            return _tableEntries;
         }
      }

      public void Add( string partitionKey, string rowKey, Guid callerInstanceId, Dictionary<string, object> properties )
      {
         var tableServiceEntity = new TableServiceEntity( partitionKey, rowKey );

         var entry = new MemoryTableEntry( properties, callerInstanceId );

         if ( HasEntity( partitionKey, rowKey ) )
         {
            throw new EntityAlreadyExistsException();
         }
         _tableEntries[tableServiceEntity] = entry;
      }

      public bool HasEntity( string partitionKey, string rowKey )
      {
         return TableEntries.Any( e => EntryKeysMatch( partitionKey, rowKey, e ) );
      }

      public IEnumerable<KeyValuePair<TableServiceEntity, MemoryTableEntry>> Where( string partitionKey, string rowKey, Guid callerInstanceId )
      {
         return TableEntries.Where( p => EntryKeysMatch( partitionKey, rowKey, p ) && p.Value.IsVisibleTo( callerInstanceId ) );
      }

      public IEnumerable<KeyValuePair<TableServiceEntity, MemoryTableEntry>> Where( string partitionKey, Guid callerInstanceId )
      {
         return TableEntries.Where( p => p.Key.PartitionKey == partitionKey && p.Value.IsVisibleTo( callerInstanceId ) );
      }

      public IEnumerable<KeyValuePair<TableServiceEntity, MemoryTableEntry>> WhereRange( string partitionKeyLow, string partitionKeyHigh, Guid callerInstanceId )
      {
         return TableEntries.Where( p => p.Key.PartitionKey.CompareTo( partitionKeyLow ) >= 0 && p.Key.PartitionKey.CompareTo( partitionKeyHigh ) <= 0 && p.Value.IsVisibleTo( callerInstanceId ) );
      }

      private static bool EntryKeysMatch( string partitionKey, string rowKey, KeyValuePair<TableServiceEntity, MemoryTableEntry> p )
      {
         return p.Key.PartitionKey == partitionKey && p.Key.RowKey == rowKey;
      }

      public void Update( string partitionKey, string rowKey, Guid callerInstanceId, Dictionary<string, object> serializedData )
      {
         bool entityExists = HasEntity( partitionKey, rowKey );
         if ( !entityExists )
         {
            throw new EntityDoesNotExistException();
         }

         var actualEntity = TableEntries.Single( e => EntryKeysMatch( partitionKey, rowKey, e ) );
         actualEntity.Value.Modify( callerInstanceId, serializedData );
      }
   }
}