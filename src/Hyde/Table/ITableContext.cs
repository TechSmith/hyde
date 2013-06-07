using System.Threading.Tasks;

namespace TechSmith.Hyde.Table
{
   public interface ITableContext
   {
      // Implementation using generics and reflection.
      IFilterable<T> CreateQuery<T>( string tableName ) where T : new();

      // Implemntation using dynamics.
      IFilterable<dynamic> CreateQuery( string tableName );

      void AddNewItem( string tableName, TableItem tableItem );
      void Upsert( string tableName, TableItem tableItem );
      void Update( string tableName, TableItem tableItem );
      void Merge( string tableName, TableItem tableItem );

      // Shared implementation between generics and dynamics.
      void DeleteItem( string tableName, string partitionKey, string rowKey );
      void DeleteCollection( string tableName, string partitionKey );
      void Save( Execute executeMethod );
      Task SaveAsync( Execute executeMethod );
   }
}
