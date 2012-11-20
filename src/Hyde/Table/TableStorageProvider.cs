using System;
using System.Collections.Generic;
using System.Linq;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Table
{
   public abstract class TableStorageProvider
   {
      private readonly Dictionary<string, ITableContext> _tableNameToContext = new Dictionary<string, ITableContext>();
      private readonly object _syncObject = new object();

      protected abstract ITableContext GetContext();

      /// <summary>
      /// Add entity to the given table
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="entity">Entity to store</param>
      /// <param name="partitionKey">The partition key to use when storing the entity</param>
      /// <param name="rowKey">The row key to use when storing the entity</param>
      public void Add( string tableName, dynamic entity, string partitionKey, string rowKey )
      {
         ITableContext context = GetContext( tableName );

         context.AddNewItem( tableName, entity, partitionKey, rowKey );
      }

      /// <summary>
      /// Add instance to the given table
      /// </summary>
      /// <param name="tableName">name of the table</param>
      /// <param name="instance">instance to store</param>
      /// <remarks>
      /// This method assumes that T has string properties decorated by the
      /// PartitionKeyAttribute and RowKeyAttribute, which the framework uses to determine
      /// the partition and row keys for instance.
      /// </remarks>
      /// <exception cref="ArgumentException">if T does not have properties decorated with PartitionKey and RowKey</exception>
      public void Add( string tableName, dynamic instance )
      {
         var partitionKey = ((object)instance).ReadPropertyDecoratedWith<PartitionKeyAttribute, string>();
         var rowKey = ((object)instance).ReadPropertyDecoratedWith<RowKeyAttribute, string>();

         Add( tableName, instance, partitionKey, rowKey );
      }

      public T Get<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         var context = GetContext( tableName );

         return context.GetItem<T>( tableName, partitionKey, rowKey );
      }

      public dynamic Get( string tableName, string partitionKey, string rowKey )
      {
         var context = GetContext( tableName );
         return context.GetItem( tableName, partitionKey, rowKey );
      }

      public IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         var context = GetContext( tableName );

         return context.GetCollection<T>( tableName, partitionKey );
      }

      public IEnumerable<dynamic> GetCollection( string tableName, string partitionKey )
      {
         var context = GetContext( tableName );

         return context.GetCollection( tableName, partitionKey );
      }

      /// <summary>
      /// Return the entire contents of tableName.
      /// </summary>
      /// <typeparam name="T">type of the instances to return</typeparam>
      /// <param name="tableName">name of the table</param>
      /// <returns>all rows in tableName</returns>
      public IEnumerable<T> GetCollection<T>( string tableName ) where T : new()
      {
         var context = GetContext( tableName );

         return context.GetCollection<T>( tableName );
      }

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      public IEnumerable<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         var context = GetContext( tableName );

         return context.GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      public IEnumerable<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         var context = GetContext( tableName );

         return context.GetRangeByPartitionKey( tableName, partitionKeyLow, partitionKeyHigh );
      }

      public IEnumerable<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         var context = GetContext( tableName );

         return context.GetRangeByRowKey<T>( tableName, partitionKey, rowKeyLow, rowKeyHigh );
      }
      
      public IEnumerable<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         var context = GetContext( tableName );

         return context.GetRangeByRowKey( tableName, partitionKey, rowKeyLow, rowKeyHigh );
      }

      private ITableContext GetContext( string tableName )
      {
         lock ( _syncObject )
         {
            if ( !_tableNameToContext.Keys.Contains( tableName ) )
            {
               _tableNameToContext[tableName] = GetContext();
            }
         }

         return _tableNameToContext[tableName];
      }

      public void Save()
      {
         foreach ( var tableContext in _tableNameToContext )
         {
            tableContext.Value.Save();
         }
      }

      public void Upsert( string tableName, dynamic instance, string partitionKey, string rowKey )
      {
         var context = GetContext( tableName );
         context.Upsert( tableName, instance, partitionKey, rowKey );
      }

      public void Upsert( string tableName, dynamic instance )
      {
         var partitionKey = ((object)instance).ReadPropertyDecoratedWith<PartitionKeyAttribute, string>();
         var rowKey = ((object)instance).ReadPropertyDecoratedWith<RowKeyAttribute, string>();

         Upsert( tableName, instance, partitionKey, rowKey );
      }

      public void Delete( string tableName, dynamic instance )
      {
         var partitionKey = ((object)instance).ReadPropertyDecoratedWith<PartitionKeyAttribute, string>();
         var rowKey = ((object)instance).ReadPropertyDecoratedWith<RowKeyAttribute, string>();
         Delete( tableName, partitionKey, rowKey );
      }

      public void Delete( string tableName, string partitionKey, string rowKey )
      {
         var context = GetContext( tableName );
         context.DeleteItem( tableName, partitionKey, rowKey );
      }

      public void DeleteCollection( string tableName, string partitionKey )
      {
         var context = GetContext( tableName );

         context.DeleteCollection( tableName, partitionKey );
      }

      public void Update( string tableName, dynamic item, string partitionKey, string rowKey )
      {
         var context = GetContext( tableName );
         context.Update( tableName, item, partitionKey, rowKey );
      }

      public void Update( string tableName, dynamic item )
      {
         var partitionKey = ((object)item).ReadPropertyDecoratedWith<PartitionKeyAttribute, string>();
         var rowKey = ((object)item).ReadPropertyDecoratedWith<RowKeyAttribute, string>();

         Update( tableName, item, partitionKey, rowKey );
      }
   }
}
