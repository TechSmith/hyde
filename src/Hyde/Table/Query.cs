using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TechSmith.Hyde.Table
{
   public class Query<T> : IQuery<T> where T : new()
   {
      private IEnumerable<T> _results;

      public Query( IEnumerable<T> results )
      {
         _results = results;
      }

      public IQuery<T> Top( int count )
      {
         return new Query<T>( _results.Take( count ) );
      }

      public IEnumerator<T> GetEnumerator()
      {
         return _results.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }
   }
}
