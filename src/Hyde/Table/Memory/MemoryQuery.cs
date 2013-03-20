using System.Collections.Generic;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table.Memory
{
   internal class MemoryQuery<T> : AbstractMemoryQuery<T> where T : new()
   {
      public MemoryQuery( IEnumerable<GenericTableEntity> entities )
         : base( entities )
      {
      }

      private MemoryQuery( MemoryQuery<T> previous )
         : base( previous )
      {
      }

      protected override AbstractQuery<T> CreateCopy()
      {
         return new MemoryQuery<T>( this );
      }

      internal override T Convert( GenericTableEntity e )
      {
         return e.ConvertTo<T>();
      }
   }
}
