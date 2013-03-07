using System;
using System.Collections.Generic;

namespace TechSmith.Hyde.Table
{
   public abstract class TableStorageProvider
   {
      // The maximum key value for a partition or row key is the largest unicode-16 character (\uFFFF) repeated 1024 times.
      public readonly string MaximumKeyValue;
      public const char HighestTableStorageUnicodeCharacter = '\uFFFF';

      // The minimum key value for a partition or row key is a single space character ' '
      public readonly string MinimumKeyValue = new string( new[] { LowestTableStorageUnicodeCharacter } );
      public const char LowestTableStorageUnicodeCharacter = '\u0020';

      private readonly ITableContext _context;

      protected TableStorageProvider( ITableContext context )
      {
         _context = context;

         var charArray = new char[1024];
         for ( int i = 0; i < charArray.Length; i++ )
         {
            charArray[ i ] = HighestTableStorageUnicodeCharacter;
         }
         MaximumKeyValue = new string( charArray );
      }

      private TableItem.ReservedPropertyBehavior _reservedPropertyBehavior = TableItem.ReservedPropertyBehavior.Throw;
      /// <summary>
      /// Sets how reserved property names are handled.  The default is true.
      /// If true an InvalidEntityException will be thrown when reserved property names are encountered.
      /// If false the PartitionKey and RowKey properties will be used when available, ignoring all other reserved properties.
      /// The reserved properties are "PartitionKey", "RowKey", "Timestamp", and "ETag".
      /// </summary>
      public bool ShouldThrowForReservedPropertyNames
      {
         get
         {
            return _reservedPropertyBehavior == TableItem.ReservedPropertyBehavior.Throw ? true : false;
         }
         set
         {
            _reservedPropertyBehavior = value ? TableItem.ReservedPropertyBehavior.Throw : TableItem.ReservedPropertyBehavior.Ignore;
         }
      }

      /// <summary>
      /// Add entity to the given table
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to store</param>
      /// <param name="partitionKey">The partition key to use when storing the entity</param>
      /// <param name="rowKey">The row key to use when storing the entity</param>
      public void Add( string tableName, dynamic instance, string partitionKey, string rowKey )
      {
         _context.AddNewItem( tableName, TableItem.Create( instance, partitionKey, rowKey, _reservedPropertyBehavior ) );
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
      /// <exception cref="ArgumentException">if T does not have properties PartitionKey and or RowKey</exception>
      public void Add( string tableName, dynamic instance )
      {
         _context.AddNewItem( tableName, TableItem.Create( instance, _reservedPropertyBehavior ) );
      }

      public T Get<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         return _context.GetItem<T>( tableName, partitionKey, rowKey );
      }

      public dynamic Get( string tableName, string partitionKey, string rowKey )
      {
         return _context.GetItem( tableName, partitionKey, rowKey );
      }

      public IQuery<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         return _context.GetCollection<T>( tableName, partitionKey );
      }

      public IQuery<dynamic> GetCollection( string tableName, string partitionKey )
      {
         return _context.GetCollection( tableName, partitionKey );
      }

      /// <summary>
      /// Return the entire contents of tableName.
      /// </summary>
      /// <typeparam name="T">type of the instances to return</typeparam>
      /// <param name="tableName">name of the table</param>
      /// <returns>all rows in tableName</returns>
      public IQuery<T> GetCollection<T>( string tableName ) where T : new()
      {
         return _context.GetCollection<T>( tableName );
      }

      /// <summary>
      /// Return the entire contents of tableName.
      /// </summary>
      /// <param name="tableName">name of the table</param>
      /// <returns>all rows in tableName</returns>
      public IQuery<dynamic> GetCollection( string tableName )
      {
         return _context.GetCollection( tableName );
      }

      [Obsolete( "Use GetRangeByPartitionKey instead." )]
      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      public IQuery<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return _context.GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      public IQuery<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         return _context.GetRangeByPartitionKey( tableName, partitionKeyLow, partitionKeyHigh );
      }

      public IQuery<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         return _context.GetRangeByRowKey<T>( tableName, partitionKey, rowKeyLow, rowKeyHigh );
      }

      public IQuery<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         return _context.GetRangeByRowKey( tableName, partitionKey, rowKeyLow, rowKeyHigh );
      }

      public void Save()
      {
         Save( Execute.Individually );
      }

      public void Save( Execute executeMethod )
      {
         _context.Save( executeMethod );
      }

      public void Upsert( string tableName, dynamic instance, string partitionKey, string rowKey )
      {
         _context.Upsert( tableName, TableItem.Create( instance, partitionKey, rowKey, _reservedPropertyBehavior ) );
      }

      public void Upsert( string tableName, dynamic instance )
      {
         _context.Upsert( tableName, TableItem.Create( instance, _reservedPropertyBehavior ) );
      }

      public void Delete( string tableName, dynamic instance )
      {
         TableItem tableItem = TableItem.Create( instance, _reservedPropertyBehavior );
         Delete( tableName, tableItem.PartitionKey, tableItem.RowKey );
      }

      public void Delete( string tableName, string partitionKey, string rowKey )
      {
         _context.DeleteItem( tableName, partitionKey, rowKey );
      }

      public void DeleteCollection( string tableName, string partitionKey )
      {
         _context.DeleteCollection( tableName, partitionKey );
      }

      public void Update( string tableName, dynamic instance, string partitionKey, string rowKey )
      {
         _context.Update( tableName, TableItem.Create( instance, partitionKey, rowKey, _reservedPropertyBehavior ) );
      }

      public void Update( string tableName, dynamic instance )
      {
         _context.Update( tableName, TableItem.Create( instance, _reservedPropertyBehavior ) );
      }

      public void Merge( string tableName, dynamic instance, string partitionKey, string rowKey )
      {
         _context.Merge( tableName, TableItem.Create( instance, partitionKey, rowKey, _reservedPropertyBehavior ) );
      }

      public void Merge( string tableName, dynamic instance )
      {
         _context.Merge( tableName, TableItem.Create( instance, _reservedPropertyBehavior ) );
      }
   }
}
