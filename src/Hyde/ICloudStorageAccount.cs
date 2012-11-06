using Microsoft.WindowsAzure.Storage.Auth;

namespace TechSmith.Hyde
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