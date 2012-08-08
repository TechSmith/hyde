using Microsoft.WindowsAzure;

namespace TechSmith.CloudServices.DataModel.Core
{
   /// <summary>
   /// Adapts Microsoft.WindowsAzure.CloudStorageAccount to the ICloudStorageAccount interface.
   /// </summary>
   public class CloudStorageAccountAdapter : ICloudStorageAccount
   {
      private readonly CloudStorageAccount _account;

      public CloudStorageAccountAdapter( CloudStorageAccount account )
      {
         _account = account;
      }

      public string TableEndpoint
      {
         get
         {
            return _account.TableEndpoint.AbsoluteUri;
         }
      }

      public StorageCredentials Credentials
      {
         get
         {
            return _account.Credentials;
         }
      }
   }
}
