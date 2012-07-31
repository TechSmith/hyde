using Microsoft.WindowsAzure;

namespace TechSmith.CloudServices.DataModel.Core
{
   public interface ICloudStorageAccount
   {
      string TableEndpoint
      {
         get;
      }

      StorageCredentials Credentials
      {
         get;
      }
   }
}