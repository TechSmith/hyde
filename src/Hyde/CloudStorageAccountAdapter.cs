﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

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
   }
}
