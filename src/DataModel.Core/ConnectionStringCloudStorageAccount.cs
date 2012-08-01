
using Microsoft.WindowsAzure;

namespace TechSmith.CloudServices.DataModel.Core
{
   public class ConnectionStringCloudStorageAccount : ICloudStorageAccount
   {
      private readonly CloudStorageAccount _cloudStorageAccount;

      public ConnectionStringCloudStorageAccount( string connectionString )
      {
         _cloudStorageAccount = CloudStorageAccount.Parse( connectionString );
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
