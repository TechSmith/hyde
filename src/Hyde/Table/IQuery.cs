using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechSmith.Hyde.Table
{
   public interface IFilterable<T> : IPartitionKeyLowBoundedFilterable<T>
   {
      IBoundChoice<IPartitionKeyLowBoundedFilterable<T>> PartitionKeyFrom( string value );
      IRowKeyFilterable<T> PartitionKeyEquals( string value );
   }

   public interface IPartitionKeyLowBoundedFilterable<T> : IRowKeyFilterable<T>
   {
      IBoundChoice<IRowKeyFilterable<T>> PartitionKeyTo( string value );
   }

   public interface IRowKeyFilterable<T> :IRowKeyLowBoundedFilterable<T>
   {
      IBoundChoice<IRowKeyLowBoundedFilterable<T>> RowKeyFrom( string value );
      IQuery<T> RowKeyEquals( string value );
   }

   public interface IRowKeyLowBoundedFilterable<T> : IQuery<T>
   {
      IBoundChoice<IQuery<T>> RowKeyTo( string value );
   }

   public interface IQuery<T> : IQueryAsync<T>
   {
      IQueryAsync<T> Top( int count );
   }

   public interface IQueryAsync<T> : IEnumerable<T>
   {
      Task<IPartialResult<T>> Async();
   }

   public interface IPartialResult<T> : IEnumerable<T>
   {
      Task<IPartialResult<T>> GetNextAsync();
      IPartialResult<T> GetNext();
      bool HasMoreResults { get; }
   }

   public interface IBoundChoice<T>
   {
      T Exclusive();
      T Inclusive();
   }
}
