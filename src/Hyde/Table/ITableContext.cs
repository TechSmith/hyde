using System;
using System.Collections.Generic;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table
{
   public interface ITableContext
   {
      // Implementation using generics and reflection.
      T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new();
      IQuery<T> GetCollection<T>( string tableName ) where T : new();
      IQuery<T> GetCollection<T>( string tableName, string partitionKey ) where T : new();
      IQuery<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
      IQuery<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new();

      // Implemntation using dynamics.
      dynamic GetItem( string tableName, string partitionKey, string rowKey );
      IQuery<dynamic> GetCollection( string tableName );
      IQuery<dynamic> GetCollection( string tableName, string partitionKey );
      IQuery<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh );
      IQuery<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh );
      void AddNewItem( string tableName, TableItem tableItem );
      void Upsert( string tableName, TableItem tableItem );
      void Update( string tableName, TableItem tableItem );

      // Shared implementation between generics and dynamics.
      void DeleteItem( string tableName, string partitionKey, string rowKey );
      void DeleteCollection( string tableName, string partitionKey );
      void Save( Execute executeMethod );

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
   }
}
