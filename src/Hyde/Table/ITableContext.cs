using System;
using System.Collections.Generic;

namespace TechSmith.Hyde.Table
{
   public interface ITableContext
   {
      void AddNewItem<T>( string tableName, T itemToAdd, string partitionKey, string rowKey ) where T : new();
      T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new();
      dynamic GetItem( string tableName, string partitionKey, string rowKey );
      IEnumerable<T> GetCollection<T>( string tableName ) where T : new();
      IEnumerable<dynamic> GetCollection( string tableName );
      IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new();
      IEnumerable<dynamic> GetCollection( string tableName, string partitionKey );
      IEnumerable<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
      IEnumerable<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh );
      IEnumerable<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new();
      IEnumerable<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh );
      void Save();
      void Upsert<T>( string tableName, T itemToUpsert, string partitionKey, string rowKey ) where T : new();
      void Update<T>( string tableName, T item, string partitionKey, string rowKey ) where T : new();
      void DeleteItem( string tableName, string partitionKey, string rowKey );
      void DeleteCollection( string tableName, string partitionKey );

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
   }
}
