using System;
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
      /// Sets whether or not the ETag property should be included when reading a dynamic object from table storage
      /// </summary>
      public bool ShouldIncludeETagWithDynamics
      {
         get;
         set;
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

      public async Task<T> GetAsync<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         T[] result = ( await _context.CreateQuery<T>( tableName )
                        .PartitionKeyEquals( partitionKey )
                        .RowKeyEquals( rowKey )
                        .PartialAsync()
                        .ConfigureAwait( false ) )
                        .ToArray();
         if ( result.Length == 0 )
         {
            throw new EntityDoesNotExistException( partitionKey, rowKey, null );
         }

         return result[0];
      }

      public async Task<dynamic> GetAsync( string tableName, string partitionKey, string rowKey )
      {
         var result = ( await _context.CreateQuery( tableName, ShouldIncludeETagWithDynamics )
                        .PartitionKeyEquals( partitionKey )
                        .RowKeyEquals( rowKey )
                        .PartialAsync()
                        .ConfigureAwait( false ) )
                        .ToArray();

         if ( result.Length == 0 )
         {
            throw new EntityDoesNotExistException( partitionKey, rowKey, null );
         }

         return result[0];
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
         return _context.CreateQuery( tableName, ShouldIncludeETagWithDynamics );
      }

      public async Task SaveAsync()
      {
         await SaveAsync( Execute.Individually ).ConfigureAwait( false );
      }

      public async Task SaveAsync( Execute executeMethod )
      {
         await _context.SaveAsync( executeMethod ).ConfigureAwait( false );
      }

      /// <summary>
      /// Insert or replace the instance with specified partition key and row key
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to store</param>
      /// <param name="partitionKey">The partition key to use when storing or replacing the entity</param>
      /// <param name="rowKey">The row key to use when storing or replacing the entity</param>
      public void Upsert( string tableName, dynamic instance, string partitionKey, string rowKey )
      {
         _context.Upsert( tableName, TableItem.Create( instance, partitionKey, rowKey, _reservedPropertyBehavior ) );
      }

      /// <summary>
      /// Insert or replace the instance in table storage
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to insert or replace</param>
      public void Upsert( string tableName, dynamic instance )
      {
         _context.Upsert( tableName, TableItem.Create( instance, _reservedPropertyBehavior ) );
      }

      /// <summary>
      /// Remove the instance from table storage
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to delete</param>
      public void Delete( string tableName, dynamic instance )
      {
         Delete( tableName, instance, ConflictHandling.Throw );
      }

      /// <summary>
      /// Remove the instance from table storage
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to delete</param>
      /// <param name="conflictHandling">Method for handling ETag conflicts</param>
      public void Delete( string tableName, dynamic instance, ConflictHandling conflictHandling )
      {
         TableItem tableItem = TableItem.Create( instance, _reservedPropertyBehavior );
         if ( tableItem.ETag == null )
         {
            Delete( tableName, tableItem.PartitionKey, tableItem.RowKey );            
         }
         else
         {
            _context.DeleteItem( tableName, tableItem, conflictHandling );
         }
      }
      
      /// <summary>
      /// Remove the entity from table storage at the specified partition key and row key
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="partitionKey">The partition key to use when deleting the entity</param>
      /// <param name="rowKey">The row key to use when deleting the entity</param>
      public void Delete( string tableName, string partitionKey, string rowKey )
      {
         _context.DeleteItem( tableName, partitionKey, rowKey );
      }

      /// <summary>
      /// Update the entity with specified partition key and row key in table storage
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to update</param>
      /// <param name="partitionKey">The partition key to use when storing the entity</param>
      /// <param name="rowKey">The row key to use when storing the entity</param>
      public void Update( string tableName, dynamic instance, string partitionKey, string rowKey )
      {
         Update( tableName, instance, partitionKey, rowKey, ConflictHandling.Throw );
      }

      /// <summary>
      /// Update the entity with specified partition key and row key in table storage
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to update</param>
      /// <param name="partitionKey">The partition key to use when storing the entity</param>
      /// <param name="rowKey">The row key to use when storing the entity</param>
      /// <param name="conflictHandling">Method for handling ETag conflicts</param>
      public void Update( string tableName, dynamic instance, string partitionKey, string rowKey, ConflictHandling conflictHandling )
      {
         _context.Update( tableName, TableItem.Create( instance, partitionKey, rowKey, _reservedPropertyBehavior ), conflictHandling );
      }

      /// <summary>
      /// Update the entity
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to update</param>
      public void Update( string tableName, dynamic instance )
      {
         Update( tableName, instance, ConflictHandling.Throw );
      }

      /// <summary>
      /// Update the entity
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to update</param>
      /// <param name="conflictHandling">Method for handling ETag conflicts</param>
      public void Update( string tableName, dynamic instance, ConflictHandling conflictHandling )
      {
         _context.Update( tableName, TableItem.Create( instance, _reservedPropertyBehavior ), conflictHandling );
      }

      /// <summary>
      /// Merge the entity with specified partition key and row key
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to merge</param>
      /// <param name="partitionKey">The partition key to use when merging the entity</param>
      /// <param name="rowKey">The row key to use when merging the entity</param>
      public void Merge( string tableName, dynamic instance, string partitionKey, string rowKey )
      {
         Merge( tableName, instance, partitionKey, rowKey, ConflictHandling.Throw );
      }

      /// <summary>
      /// Merge the entity with specified partition key and row key
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to merge</param>
      /// <param name="partitionKey">The partition key to use when merging the entity</param>
      /// <param name="rowKey">The row key to use when merging the entity</param>
      /// <param name="conflictHandling">Method for handling ETag conflicts</param>
      public void Merge( string tableName, dynamic instance, string partitionKey, string rowKey, ConflictHandling conflictHandling )
      {
         _context.Merge( tableName, TableItem.Create( instance, partitionKey, rowKey, _reservedPropertyBehavior ), conflictHandling );
      }

      /// <summary>
      /// Merge the entity
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to merge</param>
      public void Merge( string tableName, dynamic instance )
      {
         Merge( tableName, instance, ConflictHandling.Throw );
      }

      /// <summary>
      /// Merge the entity
      /// </summary>
      /// <param name="tableName">Name of the table</param>
      /// <param name="instance">the instance to merge</param>
      /// <param name="conflictHandling">Method for handling ETag conflicts</param>
      public void Merge( string tableName, dynamic instance, ConflictHandling conflictHandling )
      {
         _context.Merge( tableName, TableItem.Create( instance, _reservedPropertyBehavior ), conflictHandling );
      }
   }
}
