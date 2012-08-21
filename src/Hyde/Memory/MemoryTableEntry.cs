using System;
using System.Collections.Generic;

namespace TechSmith.Hyde.Memory
{
   internal class MemoryTableEntry
   {
      private Dictionary<string, object> _entryProperties;

      private readonly Dictionary<Guid, Dictionary<string, object>> _tempEntryProperties;

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

      public MemoryTableEntry( Dictionary<string, object> properties, Guid creatorId )
      {
         _entryProperties = new Dictionary<string, object>();
         _tempEntryProperties = new Dictionary<Guid, Dictionary<string, object>>
                                {
                                   {
                                      creatorId, properties
                                   }
                                };

         InstancesWhoHaveDeleted = new List<Guid>();

         IsDeleted = false;
      }

      public Dictionary<string, object> EntryProperties( Guid callerInstanceId )
      {
         if ( _tempEntryProperties.ContainsKey( callerInstanceId ) )
         {
            return _tempEntryProperties[callerInstanceId];
         }
         return _entryProperties;
      }

      public void Modify( Guid instanceId, Dictionary<string, object> serializedData )
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