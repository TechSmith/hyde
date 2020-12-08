using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.Common
{
   public static class IPartialResultTaskExtensions
   {
      /// <summary>
      /// Converts a partial result into a single enumerable by eagerly fetching more results. This may result in results being
      /// fetched and loaded into memory faster than they are being processed.
      /// </summary>
      /// <typeparam name="T">The type of the result being returned.</typeparam>
      /// <param name="asyncResult">The result being flattened.</param>
      /// <returns></returns>
      public static async Task<IEnumerable<T>> FlattenAsync<T>( this Task<IPartialResult<T>> asyncResult )
      {
         var resultSet = new List<T>();
         await MergeAndContinueIfNecessaryAsync( await asyncResult.ConfigureAwait( false ), resultSet ).ConfigureAwait( false );
         return resultSet;
      }

      private static async Task MergeAndContinueIfNecessaryAsync<T>( IPartialResult<T> partialResult, List<T> aggregator )
      {
         aggregator.AddRange( partialResult );
         if ( partialResult.HasMoreResults && !QueryHasBeenSatisfied( aggregator, partialResult.Query ) )
         {
            await MergeAndContinueIfNecessaryAsync<T>( await partialResult.GetNextAsync().ConfigureAwait( false ), aggregator ).ConfigureAwait( false );
         }
      }

      private static bool QueryHasBeenSatisfied<T>( List<T> aggregator, QueryDescriptor query )
      {
         return query.TopCount.HasValue && aggregator.Count() >= query.TopCount.Value;
      }
   }
}
