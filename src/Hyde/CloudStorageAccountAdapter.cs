using Microsoft.Azure.Cosmos.Table;

namespace TechSmith.Hyde
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

      public CloudStorageAccountAdapter( CloudStorageAccount account, TableClientConfiguration tableClientConfiguration ) : this( account )
      {
         TableClientConfiguration = tableClientConfiguration;
      }

      public string TableEndpoint
      {
         get
         {
            return _account.TableStorageUri.PrimaryUri.AbsoluteUri;
         }
      }

      public string ReadonlyFallbackTableEndpoint
      {
         get
         {
            return _account.TableStorageUri.SecondaryUri == null ? null : _account.TableStorageUri.SecondaryUri.AbsoluteUri;
         }
      }

      public StorageCredentials Credentials
      {
         get
         {
            return _account.Credentials;
         }
      }

      public TableClientConfiguration TableClientConfiguration
      {
         get;
      }
   }
}
