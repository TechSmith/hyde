using System;
using System.Collections.Generic;

namespace TechSmith.Hyde.Table
{
   public interface ITableContext
   {
      void AddNewItem<T>( string tableName, T itemToAdd, string partitionKey, string rowKey ) where T : new();
      T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new();
      IEnumerable<T> GetCollection<T>( string tableName ) where T : new();
      IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new();
      IEnumerable<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
      IEnumerable<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new();
      void Save();
      void Upsert<T>( string tableName, T itemToUpsert, string partitionKey, string rowKey ) where T : new();
      void Update<T>( string tableName, T item, string partitionKey, string rowKey ) where T : new();
      void DeleteItem( string tableName, string partitionKey, string rowKey );
      void DeleteCollection( string tableName, string partitionKey );

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
   }
}
