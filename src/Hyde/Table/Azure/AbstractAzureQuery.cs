using System.Collections.Generic;
using System.Linq;
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
