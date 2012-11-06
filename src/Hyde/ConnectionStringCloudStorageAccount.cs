using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace TechSmith.Hyde
{
   public class ConnectionStringCloudStorageAccount : ICloudStorageAccount
   {
      private readonly CloudStorageAccount _cloudStorageAccount;

      public ConnectionStringCloudStorageAccount( string connectionString )
      {
         // HACK: This is a workaround for a bug in the Azure 2.0 version of the SDK.  Link: http://blogs.msdn.com/b/windowsazurestorage/archive/2012/11/01/known-issues-for-windows-azure-storage-client-library-2-0-for-net-and-windows-runtime.aspx
         // TODO: Remove when bug in parsing local connection string is fixed.
         if ( connectionString == "UseDevelopmentStorage=true" )
         {
            _cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
         }
         else
         {
            _cloudStorageAccount = CloudStorageAccount.Parse( connectionString );
         }
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
