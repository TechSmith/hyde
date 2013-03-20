using System.Collections.Generic;
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
         return e.ConvertToDynamic();
      }
   }
}
