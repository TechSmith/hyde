using System.Collections.Generic;
using System.Dynamic;
using System.IO;

namespace TechSmith.Hyde.IntegrationTest
{
   class DynamicPropertyBag : DynamicObject
   {
      private readonly Dictionary<string, object> _storage = new Dictionary<string, object>();

      public override bool TryGetMember( GetMemberBinder binder, out object result )
      {
         if ( _storage.ContainsKey( binder.Name ) )
         {
            result = _storage[binder.Name];
            return true;
         }
         result = null;
         return false;
      }

      public override bool TrySetMember( SetMemberBinder binder, object value )
      {
         string key = binder.Name;
         if ( _storage.ContainsKey( key ) )
         {
            _storage[key] = value;
         }
         else
         {
            _storage.Add( key, value );
         }
         return true;
      }

      public override string ToString()
      {
         var message = new StringWriter();
         foreach ( var item in _storage )
         {
            message.WriteLine( "{0}:\t{1}", item.Key, item.Value );
         }
         return message.ToString();
      }

      public override IEnumerable<string> GetDynamicMemberNames()
      {
         return _storage.Keys;
      }
   }
}