using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechSmith.Hyde.Common;

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
         var result = _context.CreateQuery<T>( tableName ).PartitionKeyEquals( partitionKey ).RowKeyEquals( rowKey ).ToArray();
         if ( result.Length == 0 )
         {
            throw new EntityDoesNotExistException( partitionKey, rowKey, null );
         }
         return result[0];
      }

      public Task<T> GetAsync<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         return _context.CreateQuery<T>( tableName )
                        .PartitionKeyEquals( partitionKey )
                        .RowKeyEquals( rowKey )
                        .Async()
                        .ContinueWith( task => EndGetAsync( task, partitionKey, rowKey ) );
      }

      public Task<dynamic> GetAsync( string tableName, string partitionKey, string rowKey )
      {
         return _context.CreateQuery( tableName )
                        .PartitionKeyEquals( partitionKey )
                        .RowKeyEquals( rowKey )
                        .Async()
                        .ContinueWith( task => EndGetAsync( task, partitionKey, rowKey ),
                                       TaskContinuationOptions.OnlyOnRanToCompletion );
      }

      private static T EndGetAsync<T>( Task<IPartialResult<T>> task, string pk, string rk )
      {
         var result = task.Result.ToArray();
         if ( result.Length == 0 )
         {
            throw new EntityDoesNotExistException( pk, rk, null );
         }
         return result[0];
      }

      public dynamic Get( string tableName, string partitionKey, string rowKey )
      {
         var result = _context.CreateQuery( tableName ).PartitionKeyEquals( partitionKey ).RowKeyEquals( rowKey ).ToArray();
         if ( result.Length == 0 )
         {
            throw new EntityDoesNotExistException( partitionKey, rowKey, null );
         }
         return result[0];
      }

      [Obsolete( "Use CreateQuery<T>" )]
      public IQuery<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         return _context.CreateQuery<T>( tableName ).PartitionKeyEquals( partitionKey );
      }

      [Obsolete( "Use CreateQuery" )]
      public IQuery<dynamic> GetCollection( string tableName, string partitionKey )
      {
         return _context.CreateQuery( tableName ).PartitionKeyEquals( partitionKey );
      }

      /// <summary>
      /// Return the entire contents of tableName.
      /// </summary>
      /// <typeparam name="T">type of the instances to return</typeparam>
      /// <param name="tableName">name of the table</param>
      /// <returns>all rows in tableName</returns>
      [Obsolete( "Use CreateQuery<T>" )]
      public IQuery<T> GetCollection<T>( string tableName ) where T : new()
      {
         return _context.CreateQuery<T>( tableName );
      }

      /// <summary>
      /// Return the entire contents of tableName.
      /// </summary>
      /// <param name="tableName">name of the table</param>
      /// <returns>all rows in tableName</returns>
      [Obsolete( "Use CreateQuery" )]
      public IQuery<dynamic> GetCollection( string tableName )
      {
         return _context.CreateQuery( tableName );
      }

      /// <summary>
      /// Create a query object that allows fluent filtering on partition and row keys.
      /// </summary>
      /// <typeparam name="T">type of the instances to return</typeparam>
      /// <param name="tableName">name of the table</param>
      /// <returns>a fluent query object</returns>
      public IFilterable<T> CreateQuery<T>( string tableName ) where T : new()
      {
         return _context.CreateQuery<T>( tableName );
      }

      /// <summary>
      /// Create a query object that allows fluent filtering on partition and row keys.
      /// </summary>
      /// <param name="tableName">name of the table</param>
      /// <returns>a fluent query object</returns>
      public IFilterable<dynamic> CreateQuery( string tableName )
      {
         return _context.CreateQuery( tableName );
      }

      [Obsolete( "Use CreateQuery<T>" )]
      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return GetRangeByPartitionKey<T>( tableName, partitionKeyLow, partitionKeyHigh );
      }

      [Obsolete( "Use CreateQuery<T>" )]
      public IQuery<T> GetRangeByPartitionKey<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return _context.CreateQuery<T>( tableName )
                        .PartitionKeyFrom( partitionKeyLow ).Inclusive()
                        .PartitionKeyTo( partitionKeyHigh ).Inclusive();
      }

      [Obsolete( "Use CreateQuery" )]
      public IQuery<dynamic> GetRangeByPartitionKey( string tableName, string partitionKeyLow, string partitionKeyHigh )
      {
         return _context.CreateQuery( tableName )
                        .PartitionKeyFrom( partitionKeyLow ).Inclusive()
                        .PartitionKeyTo( partitionKeyHigh ).Inclusive();
      }

      [Obsolete( "Use CreateQuery<T>" )]
      public IQuery<T> GetRangeByRowKey<T>( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh ) where T : new()
      {
         return _context.CreateQuery<T>( tableName )
                        .PartitionKeyEquals( partitionKey )
                        .RowKeyFrom( rowKeyLow ).Inclusive()
                        .RowKeyTo( rowKeyHigh ).Inclusive();
      }

      [Obsolete( "Use CreateQuery" )]
      public IQuery<dynamic> GetRangeByRowKey( string tableName, string partitionKey, string rowKeyLow, string rowKeyHigh )
      {
         return _context.CreateQuery( tableName )
                        .PartitionKeyEquals( partitionKey )
                        .RowKeyFrom( rowKeyLow ).Inclusive()
                        .RowKeyTo( rowKeyHigh ).Inclusive();
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
