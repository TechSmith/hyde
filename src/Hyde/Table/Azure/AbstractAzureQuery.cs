using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

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

      public override IEnumerator<T> GetEnumerator()
      {
         var query = CreateTableQuery();
         return _table.ExecuteQuery( query ).Select( ConvertResult ).GetEnumerator();
      }

      public override Task<IPartialResult<T>> Async()
      {
         return GetPartialResultAsync( CreateTableQuery(), new TableContinuationToken() );
      }

      private Task<IPartialResult<T>> GetPartialResultAsync( TableQuery<GenericTableEntity> query,
                                           TableContinuationToken token )
      {
         var asyncResult = _table.BeginExecuteQuerySegmented( query, token, ar => { }, null );
         return Task.Factory.FromAsync( asyncResult, r =>
         {
            var segment = _table.EndExecuteQuerySegmented<GenericTableEntity>( r );
            return (IPartialResult<T>)new PartialResult( this, query, segment );
         } );
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

         public bool HasMoreResults { get { return _segment.ContinuationToken != null; } }

         public Task<IPartialResult<T>> GetNextAsync()
         {
            if ( !HasMoreResults )
            {
               throw new InvalidOperationException( "Cannot get next when there are no more results" );
            }
            return _parent.GetPartialResultAsync( _query, _segment.ContinuationToken );
         }

         public IPartialResult<T> GetNext()
         {
            if ( !HasMoreResults )
            {
               throw new InvalidOperationException( "Cannot get next when there are no more results" );
            }
            var nextSegment = _parent._table.ExecuteQuerySegmented( _query, _segment.ContinuationToken );
            return new PartialResult( _parent, _query, nextSegment );
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
