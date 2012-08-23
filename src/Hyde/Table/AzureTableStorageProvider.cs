using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table
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