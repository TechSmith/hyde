using Microsoft.WindowsAzure;

namespace TechSmith.CloudServices.DataModel.Core
{
   public class AzureCloudStorageAccount : ICloudStorageAccount
   {
      private readonly CloudStorageAccount _cloudStorageAccount;

      public AzureCloudStorageAccount()
      {
         _cloudStorageAccount = CloudStorageAccount.FromConfigurationSetting( "StorageAccountConnectionString" );
      }

      public string TableEndpoint
      {
         get
         {
            return _cloudStorageAccount.TableEndpoint.AbsoluteUri;
         }
      }

      public StorageCredentials Credentials
      {
         get
         {
            return _cloudStorageAccount.Credentials;
         }
      }
   }
}