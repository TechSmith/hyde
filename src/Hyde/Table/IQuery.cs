using System.Collections.Generic;

namespace TechSmith.Hyde.Table
{
   public interface IQuery<T> : IEnumerable<T> where T : new()
   {
      IQuery<T> Top( int count );
   }
}
