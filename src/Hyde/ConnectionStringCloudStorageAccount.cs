using Microsoft.Azure.Cosmos.Table;

namespace TechSmith.Hyde
{
   public class ConnectionStringCloudStorageAccount : ICloudStorageAccount
   {
      private readonly CloudStorageAccount _cloudStorageAccount;

      public ConnectionStringCloudStorageAccount( string connectionString )
      {
         _cloudStorageAccount = CloudStorageAccount.Parse( connectionString );
      }

      public ConnectionStringCloudStorageAccount( string connectionString, TableClientConfiguration tableClientConfiguration ) : this( connectionString )
      {
         TableClientConfiguration = tableClientConfiguration;
      }

      public string TableEndpoint
      {
         get
         {
            return _cloudStorageAccount.TableStorageUri.PrimaryUri.AbsoluteUri;
         }
      }

      public string ReadonlyFallbackTableEndpoint
      {
         get
         {
            return _cloudStorageAccount.TableStorageUri.SecondaryUri == null ? null : _cloudStorageAccount.TableStorageUri.SecondaryUri.AbsoluteUri;
         }
      }

      public StorageCredentials Credentials
      {
         get
         {
            return _cloudStorageAccount.Credentials;
         }
      }

      public TableClientConfiguration TableClientConfiguration
      {
         get;
      }
   }
}
