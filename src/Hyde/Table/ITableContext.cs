﻿using System;
using System.Collections.Generic;

namespace TechSmith.Hyde.Table
{
   public interface ITableContext
   {
      // Implementation using generics and reflection.
      T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new();
      dynamic GetItem( string tableName, string partitionKey, string rowKey );
      IEnumerable<T> GetCollection<T>( string tableName ) where T : new();
      IEnumerable<dynamic> GetCollection( string tableName );
      IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new();
      IEnumerable<dynamic> GetCollection( string tableName, string partitionKey );
      IEnumerable<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
      IEnumerable<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh );
      IEnumerable<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new();
      void AddNewItem<T>( string tableName, T itemToAdd, string partitionKey, string rowKey ) where T : new();
      void Upsert<T>( string tableName, T itemToUpsert, string partitionKey, string rowKey ) where T : new();
      void Update<T>( string tableName, T item, string partitionKey, string rowKey ) where T : new();

      // Implemntation using dynamics.
      dynamic GetItem( string tableName, string partitionKey, string rowKey );
      IEnumerable<dynamic> GetCollection( string tableName );
      IEnumerable<dynamic> GetCollection( string tableName, string partitionKey );
      IEnumerable<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh );
      IEnumerable<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh );
      void AddNewDynamicItem( string tableName, dynamic itemToAdd, string partitionKey, string rowKey );
      void UpsertDynamic( string tableName, dynamic itemToUpsert, string partitionKey, string rowKey );
      void UpdateDynamic( string tableName, dynamic item, string partitionKey, string rowKey );

      // Shared implementation between generics and dynamics.
      void DeleteItem( string tableName, string partitionKey, string rowKey );
      void DeleteCollection( string tableName, string partitionKey );
      void Save();

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
   }
}
