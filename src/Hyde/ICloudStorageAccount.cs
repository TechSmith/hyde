using Microsoft.Azure.Cosmos.Table;

namespace TechSmith.Hyde
{
   public interface ICloudStorageAccount
   {
      string TableEndpoint
      {
         get;
      }

      string ReadonlyFallbackTableEndpoint
      {
         get;
      }

      StorageCredentials Credentials
      {
         get;
      }
   }
}