using Microsoft.Azure.Cosmos.Table;

namespace TechSmith.Hyde.Table.Azure
{
   public class AzureDynamicQuery : AbstractAzureQuery<object>
   {
      public AzureDynamicQuery( CloudTable table, bool shouldIncludeETagForDynamic )
         : base( table )
      {
         IncludeETagForDynamic = shouldIncludeETagForDynamic;
      }

      private AzureDynamicQuery( AzureDynamicQuery previous, bool shouldIncludeETagForDynamic )
         : base( previous )
      {
         IncludeETagForDynamic = shouldIncludeETagForDynamic;
      }

      protected override AbstractQuery<object> CreateCopy()
      {
         return new AzureDynamicQuery( this, IncludeETagForDynamic );
      }

      internal override object ConvertResult( GenericTableEntity e )
      {
         return e.ConvertToDynamic( IncludeETagForDynamic );
      }
   }
}
