using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechSmith.Hyde.Table
{
   public abstract class AbstractQuery<T> : IFilterable<T>
   {
      protected QueryDescriptor _query;

      protected AbstractQuery()
      {
         _query = new QueryDescriptor();
      }

      protected abstract AbstractQuery<T> CreateCopy();

      protected AbstractQuery( AbstractQuery<T> previous )
      {
         _query = previous._query;
      }

      public IRowKeyFilterable<T> PartitionKeyEquals( string value )
      {
         var result = CreateCopy();
         result._query.PartitionKeyRange.Lower = new KeyBound { Value = value, IsInclusive = true };
         result._query.PartitionKeyRange.Upper = new KeyBound { Value = value, IsInclusive = true };
         return result;
      }

      public IBoundChoice<IPartitionKeyLowBoundedFilterable<T>> PartitionKeyFrom( string value )
      {
         var result = CreateCopy();
         return new BoundChoice<IPartitionKeyLowBoundedFilterable<T>>( isInclusive =>
         {
            result._query.PartitionKeyRange.Lower = new KeyBound { Value = value, IsInclusive = isInclusive };
            return result;
         } );
      }

      private class BoundChoice<S> : IBoundChoice<S>
      {
         private readonly Func<bool, S> _applyChoice;

         public BoundChoice( Func<bool, S> applyChoice )
         {
            _applyChoice = applyChoice;
         }

         public S Exclusive()
         {
            return _applyChoice( false );
         }

         public S Inclusive()
         {
            return _applyChoice( true );
         }
      }

      public IBoundChoice<IRowKeyFilterable<T>> PartitionKeyTo( string value )
      {
         var result = CreateCopy();
         return new BoundChoice<IRowKeyFilterable<T>>( isInclusive =>
         {
            result._query.PartitionKeyRange.Upper = new KeyBound { Value = value, IsInclusive = isInclusive };
            return result;
         } );
      }

      public IQuery<T> RowKeyEquals( string value )
      {
         var result = CreateCopy();
         result._query.RowKeyRange.Lower = new KeyBound { Value = value, IsInclusive = true };
         result._query.RowKeyRange.Upper = new KeyBound { Value = value, IsInclusive = true };
         return result;
      }

      public IBoundChoice<IRowKeyLowBoundedFilterable<T>> RowKeyFrom( string value )
      {
         var result = CreateCopy();
         return new BoundChoice<IRowKeyLowBoundedFilterable<T>>( isInclusive =>
         {
            result._query.RowKeyRange.Lower = new KeyBound { Value = value, IsInclusive = isInclusive };
            return result;
         } );
      }

      public IBoundChoice<IQuery<T>> RowKeyTo( string value )
      {
         var result = CreateCopy();
         return new BoundChoice<IQuery<T>>( isInclusive =>
         {
            result._query.RowKeyRange.Upper = new KeyBound { Value = value, IsInclusive = isInclusive };
            return result;
         } );
      }

      public IQueryAsync<T> Top( int count )
      {
         var result = CreateCopy();
         result._query.TopCount = count;
         return result;
      }

      public abstract IEnumerator<T> GetEnumerator();

      public abstract Task<IPartialResult<T>> Async();

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }
   }
}
