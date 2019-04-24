using Microsoft.Azure.Cosmos.Table;

namespace TechSmith.Hyde.Table.Azure
{
   internal class AzureQuery<T> : AbstractAzureQuery<T> where T : new()
   {
      internal AzureQuery( CloudTable table )
         : base( table )
      {
      }

      private AzureQuery( AzureQuery<T> previous )
         : base( previous )
      {
      }

      protected override AbstractQuery<T> CreateCopy()
      {
         return new AzureQuery<T>( this );
      }

      internal override T ConvertResult( GenericTableEntity e )
      {
         return e.ConvertTo<T>();
      }
   }
}
