using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table.Memory
{
   internal class DynamicMemoryQuery : AbstractMemoryQuery<dynamic>
   {
      public DynamicMemoryQuery( IEnumerable<GenericTableEntity> entities )
         : base( entities )
      {
      }

      private DynamicMemoryQuery( DynamicMemoryQuery previous )
         : base( previous )
      {
      }

      protected override AbstractQuery<dynamic> CreateCopy()
      {
         return new DynamicMemoryQuery( this );
      }

      internal override dynamic Convert( GenericTableEntity e )
      {
         return StripNullValues( e.ConvertToDynamic() );
      }

      private static dynamic StripNullValues( dynamic obj )
      {
         dynamic result = new ExpandoObject();
         foreach ( var p in ( obj as IDictionary<string, object> ).Where( p => p.Value != null ) )
         {
            ( (IDictionary<string, object>) result ).Add( p );
         }
         return result;
      }
   }
}
