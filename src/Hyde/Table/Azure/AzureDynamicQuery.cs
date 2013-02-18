using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace TechSmith.Hyde.Table.Azure
{
   public class AzureDynamicQuery : IQuery<object>
   {
      private readonly CloudTable _table;
      private TableQuery<GenericTableEntity> _query;

      public AzureDynamicQuery( CloudTable table, string filter )
      {
         _table = table;
         _query = new TableQuery<GenericTableEntity>().Where( filter );
      }

      public IQuery<object> Top( int count )
      {
         _query = _query.Take( count );
         return this;
      }

      public IEnumerator<object> GetEnumerator()
      {
         return _table.ExecuteQuery( _query ).Select( e => e.ConvertToDynamic() ).GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }
   }
}
