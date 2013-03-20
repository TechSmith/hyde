using Microsoft.WindowsAzure.Storage.Table;

namespace TechSmith.Hyde.Table.Azure
{
   public class AzureDynamicQuery : AbstractAzureQuery<object>
   {
      public AzureDynamicQuery( CloudTable table )
         : base( table )
      {
      }

      private AzureDynamicQuery( AzureDynamicQuery previous )
         : base( previous )
      {
      }

      protected override AbstractQuery<object> CreateCopy()
      {
         return new AzureDynamicQuery( this );
      }

      internal override object ConvertResult( GenericTableEntity e )
      {
         return e.ConvertToDynamic();
      }
   }
}
