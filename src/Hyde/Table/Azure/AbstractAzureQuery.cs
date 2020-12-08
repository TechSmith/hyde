using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace TechSmith.Hyde.Table.Azure
{
   public abstract class AbstractAzureQuery<T> : AbstractQuery<T>
   {
      private readonly CloudTable _table;

      protected AbstractAzureQuery( CloudTable table )
      {
         _table = table;
      }

      protected AbstractAzureQuery( AbstractAzureQuery<T> previous )
         : base( previous )
      {
         _table = previous._table;
      }

      public override Task<IPartialResult<T>> PartialAsync()
      {
         return GetPartialResultAsync( CreateTableQuery(), new TableContinuationToken() );
      }

      private async Task<IPartialResult<T>> GetPartialResultAsync( TableQuery<GenericTableEntity> query,
                                           TableContinuationToken token )
      {
         TableQuerySegment<GenericTableEntity> result = await _table.ExecuteQuerySegmentedAsync( query, token ).ConfigureAwait( false );
         return new PartialResult( this, query, result );  
      }

      private class PartialResult : IPartialResult<T>
      {
         private readonly AbstractAzureQuery<T> _parent;
         private readonly TableQuery<GenericTableEntity> _query;
         private readonly TableQuerySegment<GenericTableEntity> _segment;

         public PartialResult( AbstractAzureQuery<T> parent,
                               TableQuery<GenericTableEntity> query,
                               TableQuerySegment<GenericTableEntity> segment )
         {
            _parent = parent;
            _query = query;
            _segment = segment;
         }

         public QueryDescriptor Query { get { return _parent._query; } }

         public bool HasMoreResults { get { return _segment.ContinuationToken != null; } }

         public async Task<IPartialResult<T>> GetNextAsync()
         {
            if ( !HasMoreResults )
            {
               throw new InvalidOperationException( "Cannot get next when there are no more results" );
            }
            return await _parent.GetPartialResultAsync( _query, _segment.ContinuationToken ).ConfigureAwait( false );
         }

         public IEnumerator<T> GetEnumerator()
         {
            return _segment.Select( _parent.ConvertResult ).GetEnumerator();
         }

         IEnumerator IEnumerable.GetEnumerator()
         {
            return GetEnumerator();
         }
      }

      internal TableQuery<GenericTableEntity> CreateTableQuery()
      {
         return new TableQuery<GenericTableEntity>().Where( GetFilterExpression() ).Take( _query.TopCount );
      }

      internal abstract T ConvertResult( GenericTableEntity e );

      private string GetFilterExpression()
      {
         var filterStr = BuildFilterExpr( "PartitionKey", _query.PartitionKeyRange );
         var rkFilterStr = BuildFilterExpr( "RowKey", _query.RowKeyRange );
         if ( !string.IsNullOrEmpty( rkFilterStr ) )
         {
            filterStr = string.IsNullOrEmpty( filterStr )
                           ? rkFilterStr
                           : TableQuery.CombineFilters( filterStr, TableOperators.And, rkFilterStr );
         }
         return filterStr;
      }

      private static string BuildFilterExpr( string keyName, KeyRange keyRange )
      {
         if ( keyRange.IsSingleValue() )
         {
            return TableQuery.GenerateFilterCondition( keyName, QueryComparisons.Equal, keyRange.SingleValue() );
         }

         var expr = "";
         if ( keyRange.Lower.HasValue )
         {
            var op = keyRange.Lower.Value.IsInclusive
                        ? QueryComparisons.GreaterThanOrEqual
                        : QueryComparisons.GreaterThan;
            expr = TableQuery.GenerateFilterCondition( keyName, op, keyRange.Lower.Value.Value );
         }
         if ( keyRange.Upper.HasValue )
         {
            var op = keyRange.Upper.Value.IsInclusive
                        ? QueryComparisons.LessThanOrEqual
                        : QueryComparisons.LessThan;
            var ltExpr = TableQuery.GenerateFilterCondition( keyName, op, keyRange.Upper.Value.Value );
            expr = string.IsNullOrEmpty( expr ) ? ltExpr : TableQuery.CombineFilters( expr, TableOperators.And, ltExpr );
         }
         return expr;
      }
   }
}
