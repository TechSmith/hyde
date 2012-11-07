using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;

namespace TechSmith.Hyde.Table.Azure
{
   internal class AzureTableEntityTableContext : ITableContext
   {
      private readonly CloudStorageAccount _storageAccount;
      private readonly List<KeyValuePair<string, TableOperation>> _operations = new List<KeyValuePair<string, TableOperation>>(); 
      private readonly TableRequestOptions _retriableTableRequest = new TableRequestOptions { RetryPolicy = new ExponentialRetry( TimeSpan.FromSeconds( 1 ), 4 ) };

      public AzureTableEntityTableContext( ICloudStorageAccount storageAccount )
      {
         _storageAccount = new CloudStorageAccount( storageAccount.Credentials, false );
      }

      public T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         var retrieveOperation = TableOperation.Retrieve<GenericTableEntity>( partitionKey, rowKey );

         TableResult result = Table( tableName ).Execute( retrieveOperation, _retriableTableRequest );

         return ( (GenericTableEntity) result.Result ).ConvertTo<T>();
      }

      public IEnumerable<T> GetCollection<T>( string tableName ) where T : new()
      {
         var allRowsFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.GreaterThanOrEqual, "a" );
         return ExecuteFilterOnTable<T>( tableName, allRowsFilter );
      }

      public IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         var allRowsInPartitonFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         return ExecuteFilterOnTable<T>( tableName, allRowsInPartitonFilter );
      }

      public IEnumerable<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         var lowerRangePartitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.GreaterThanOrEqual, partitionKeyLow );
         var higherRangePartitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.LessThanOrEqual, partitionKeyHigh );

         var rangePartitionFilter = TableQuery.CombineFilters( lowerRangePartitionFilter, TableOperators.And, higherRangePartitionFilter );

         return ExecuteFilterOnTable<T>( tableName, rangePartitionFilter );
      }

      public IEnumerable<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         var partitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         var lowerRangeRowFilter = TableQuery.GenerateFilterCondition( "RowKey", QueryComparisons.GreaterThanOrEqual, rowKeyLow );
         var higherRangeRowFilter = TableQuery.GenerateFilterCondition( "RowKey", QueryComparisons.LessThanOrEqual, rowKeyHigh );

         var rangeRowFilter = TableQuery.CombineFilters( lowerRangeRowFilter, TableOperators.And, higherRangeRowFilter );

         var fullRangeFilter = TableQuery.CombineFilters( partitionFilter, TableOperators.And, rangeRowFilter );

         return ExecuteFilterOnTable<T>( tableName, fullRangeFilter );
      }

      public dynamic GetItem( string tableName, string partitionKey, string rowKey )
      {
         var retrieveOperation = TableOperation.Retrieve<GenericTableEntity>( partitionKey, rowKey );

         TableResult result = Table( tableName ).Execute( retrieveOperation, _retriableTableRequest );

         return ( (GenericTableEntity) result.Result ).ConvertToDynamic();
      }

      public IEnumerable<dynamic> GetCollection( string tableName )
      {
         var allRowsFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.GreaterThanOrEqual, "a" );
         return ExecuteFilterOnTable( tableName, allRowsFilter );
      }

      public IEnumerable<dynamic> GetCollection( string tableName, string partitionKey )
      {
         var allRowsInPartitonFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         return ExecuteFilterOnTable( tableName, allRowsInPartitonFilter );
      }

      public IEnumerable<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         var lowerRangePartitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.GreaterThanOrEqual, partitionKeyLow );
         var higherRangePartitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.LessThanOrEqual, partitionKeyHigh );

         var rangePartitionFilter = TableQuery.CombineFilters( lowerRangePartitionFilter, TableOperators.And, higherRangePartitionFilter );

         return ExecuteFilterOnTable( tableName, rangePartitionFilter );
      }

      public IEnumerable<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         var partitionFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         var lowerRangeRowFilter = TableQuery.GenerateFilterCondition( "RowKey", QueryComparisons.GreaterThanOrEqual, rowKeyLow );
         var higherRangeRowFilter = TableQuery.GenerateFilterCondition( "RowKey", QueryComparisons.LessThanOrEqual, rowKeyHigh );

         var rangeRowFilter = TableQuery.CombineFilters( lowerRangeRowFilter, TableOperators.And, higherRangeRowFilter );

         var fullRangeFilter = TableQuery.CombineFilters( partitionFilter, TableOperators.And, rangeRowFilter );

         return ExecuteFilterOnTable( tableName, fullRangeFilter );
      }

      public void AddNewItem( string tableName, dynamic itemToAdd, string partitionKey, string rowKey )
      {
         GenericTableEntity entity = GenericTableEntity.HydrateFrom( itemToAdd, partitionKey, rowKey );
         var operation = TableOperation.Insert( entity );
         _operations.Add( new KeyValuePair<string, TableOperation>( tableName, operation ) );
      }

      public void Upsert( string tableName, dynamic itemToUpsert, string partitionKey, string rowKey )
      {
         GenericTableEntity entity = GenericTableEntity.HydrateFrom( itemToUpsert, partitionKey, rowKey );
         var operation = TableOperation.InsertOrReplace( entity );
         _operations.Add( new KeyValuePair<string, TableOperation>( tableName, operation ) );
      }

      public void Update( string tableName, dynamic item, string partitionKey, string rowKey )
      {
         GenericTableEntity entity = GenericTableEntity.HydrateFrom( item, partitionKey, rowKey );
         var operation = TableOperation.Replace( entity );
         _operations.Add( new KeyValuePair<string, TableOperation>( tableName, operation ) );
      }

      public void DeleteItem( string tableName, string partitionKey, string rowKey )
      {
         var operation = TableOperation.Delete( new TableEntity( partitionKey, rowKey ) );
         _operations.Add( new KeyValuePair<string, TableOperation>( tableName, operation ) );
      }

      public void DeleteCollection( string tableName, string partitionKey )
      {
         var allRowsInPartitonFilter = TableQuery.GenerateFilterCondition( "PartitionKey", QueryComparisons.Equal, partitionKey );
         var getAllInPartitionQuery = new TableQuery<TableEntity>().Where( allRowsInPartitonFilter );
         var entitiesToDelete = Table( tableName ).ExecuteQuery( getAllInPartitionQuery );
         foreach ( var operation in entitiesToDelete.Select( TableOperation.Delete ) )
         {
            _operations.Add( new KeyValuePair<string, TableOperation>( tableName, operation ) );
         }
      }

      public void Save()
      {
         //TODO: error handling?
         foreach ( var operation in _operations )
         {
            Table( operation.Key ).Execute( operation.Value, _retriableTableRequest );
         }
      }

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      private IEnumerable<T> ExecuteFilterOnTable<T>( string tableName, string filter ) where T : new()
      {
         var query = new TableQuery<GenericTableEntity>().Where( filter );
         return Table( tableName ).ExecuteQuery( query ).Select( e => e.ConvertTo<T>() );
      }

      private IEnumerable<dynamic> ExecuteFilterOnTable( string tableName, string filter )
      {
         var query = new TableQuery<GenericTableEntity>().Where( filter );
         return Table( tableName ).ExecuteQuery( query ).Select( e => e.ConvertToDynamic() );
      }

      private CloudTable Table( string tableName )
      {
         return _storageAccount.CreateCloudTableClient().GetTableReference( tableName );
      }
   }
}