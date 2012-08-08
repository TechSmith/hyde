using System.Collections.Generic;

namespace TechSmith.CloudServices.DataModel.Core
{
   public interface ITableContext
   {
      void AddNewItem<T>( string tableName, T itemToAdd, string partitionKey, string rowKey ) where T : new();
      T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new();
      IEnumerable<T> GetCollection<T>( string tableName ) where T : new();
      IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new();
      IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new();
      void Save();
      void Upsert<T>( string tableName, T itemToUpsert, string partitionKey, string rowKey ) where T : new();
      void Update<T>( string tableName, T item, string partitionKey, string rowKey ) where T : new();
      void DeleteItem( string tableName, string partitionKey, string rowKey );
      void DeleteCollection( string tableName, string partitionKey );
   }
}
