using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace TechSmith.Hyde.Table.Memory
{
   internal class MemoryTableEntry
   {
      private IDictionary<string, EntityProperty> _entryProperties;

      private readonly Dictionary<Guid, IDictionary<string, EntityProperty>> _tempEntryProperties;

      public bool IsDeleted
      {
         get;
         set;
      }

      public List<Guid> InstancesWhoHaveDeleted
      {
         get;
         private set;
      }

      public MemoryTableEntry( IDictionary<string, EntityProperty> properties, Guid creatorId )
      {
         _entryProperties = new Dictionary<string, EntityProperty>();
         _tempEntryProperties = new Dictionary<Guid, IDictionary<string, EntityProperty>>
                                {
                                   {
                                      creatorId, properties
                                   }
                                };

         InstancesWhoHaveDeleted = new List<Guid>();

         IsDeleted = false;
      }

      public IDictionary<string, EntityProperty> EntryProperties( Guid callerInstanceId )
      {
         if ( _tempEntryProperties.ContainsKey( callerInstanceId ) )
         {
            return _tempEntryProperties[callerInstanceId];
         }
         return _entryProperties;
      }

      public void Modify( Guid instanceId, IDictionary<string, EntityProperty> serializedData )
      {
         _tempEntryProperties.Add( instanceId, serializedData );
      }

      public void Save( Guid instanceId )
      {
         if ( _tempEntryProperties.ContainsKey( instanceId ) )
         {
            _entryProperties = _tempEntryProperties[instanceId];
            _tempEntryProperties.Remove( instanceId );
         }
      }

      public bool IsVisibleTo( Guid callerInstanceId )
      {
         bool isCommitted = _entryProperties.Count > 0;
         bool hasTempEntryForInstance = _tempEntryProperties.ContainsKey( callerInstanceId );
         if ( !isCommitted && !hasTempEntryForInstance )
         {
            return false;
         }

         if ( IsDeleted && InstancesWhoHaveDeleted.Contains( callerInstanceId ) )
         {
            return false;
         }

         return true;
      }
   }
}