using TechSmith.Hyde.Azure;

namespace TechSmith.Hyde
{
   public class AzureTableStorageProvider : TableStorageProvider
   {
      private readonly ICloudStorageAccount _cloudStorageAccount;

      public AzureTableStorageProvider( ICloudStorageAccount cloudStorageAccount )
      {
         _cloudStorageAccount = cloudStorageAccount;
      }

      protected override ITableContext GetContext()
      {
         return new AzureTableContext( _cloudStorageAccount );
      }
   }
}