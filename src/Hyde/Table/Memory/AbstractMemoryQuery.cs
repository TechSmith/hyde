using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table.Memory
{
   internal abstract class AbstractMemoryQuery<T> : AbstractQuery<T>
   {
      private readonly IEnumerable<GenericTableEntity> _entities;

      internal AbstractMemoryQuery( IEnumerable<GenericTableEntity> entities )
      {
         _entities = entities;
      }

      internal AbstractMemoryQuery( AbstractMemoryQuery<T> previous )
         : base( previous )
      {
         _entities = previous._entities;
      }

      internal abstract T Convert( GenericTableEntity e );

      public override Task<IPartialResult<T>> Async()
      {
         return Task.Factory.StartNew<IPartialResult<T>>( () => new PartialResult( this ) );
      }

      private class PartialResult : IPartialResult<T>
      {
         private readonly AbstractMemoryQuery<T> _parent;

         public PartialResult( AbstractMemoryQuery<T> parent )
         {
            _parent = parent;
         }

         public bool HasMoreResults { get { return false; } }

         public IEnumerator<T> GetEnumerator()
         {
            return _parent.GetEnumerator();
         }

         IEnumerator IEnumerable.GetEnumerator()
         {
            return GetEnumerator();
         }

         public Task<IPartialResult<T>> GetNextAsync()
         {
            throw new InvalidOperationException();
         }

         public IPartialResult<T> GetNext()
         {
            throw new InvalidOperationException();
         }
      }

      private IEnumerable<T> ExecuteQuery()
      {
         var pkFilter = FilterByKeyRange( e => e.PartitionKey, _query.PartitionKeyRange );
         var rkFilter = FilterByKeyRange( e => e.RowKey, _query.RowKeyRange );
         var result = _entities.Where( e => pkFilter( e ) && rkFilter( e ) );
         if ( _query.TopCount.HasValue )
         {
            result = result.Take( _query.TopCount.Value );
         }
         return result.Select( Convert );
      }

      public override IEnumerator<T> GetEnumerator()
      {
         return ExecuteQuery().GetEnumerator();
      }

      private static Func<GenericTableEntity, bool> FilterByKeyRange( Func<GenericTableEntity,string> getKey, KeyRange keyRange )
      {
         if ( keyRange.IsSingleValue() )
         {
            return e => getKey( e ) == keyRange.SingleValue();
         }
         Func<GenericTableEntity, bool> lowerFilter = e => true;
         if ( keyRange.Lower.HasValue )
         {
            var bound = keyRange.Lower.Value;
            lowerFilter = e => bound.IsInclusive ? Compare( getKey( e ), bound ) >= 0 : Compare( getKey( e ), bound ) > 0;
         }
         Func<GenericTableEntity, bool> upperFilter = e => true;
         if ( keyRange.Upper.HasValue )
         {
            var bound = keyRange.Upper.Value;
            upperFilter = e => bound.IsInclusive ? Compare( getKey( e ), bound ) <= 0 : Compare( getKey( e ), bound ) < 0;
         }
         return e => lowerFilter( e ) && upperFilter( e );
      }

      private static int Compare( string val, KeyBound bound )
      {
         return String.Compare( val, bound.Value, StringComparison.Ordinal );
      }
   }
}
