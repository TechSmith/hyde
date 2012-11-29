using System;
using System.Collections.Concurrent;
using System.Net;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Table
{
   public class AzureTableStorageProvider : TableStorageProvider
   {
      private readonly ICloudStorageAccount _cloudStorageAccount;

      private static readonly ConcurrentDictionary<string, bool> _servicePointsUpdated = new ConcurrentDictionary<string, bool>();

      private static int _tableStorageConcurrentConnectionLimit = 64;
      /// <summary>
      /// Sets the number of concurrent connections that are allowed to Table Storage.
      /// The default is 64.
      /// </summary>
      public static int TableStorageConcurrentConnectionLimit
      {
         get
         {
            return _tableStorageConcurrentConnectionLimit;
         }
         set
         {
            _tableStorageConcurrentConnectionLimit = value;
            _servicePointsUpdated.Clear();
         }
      }

      public AzureTableStorageProvider( ICloudStorageAccount cloudStorageAccount )
         : base ( new AzureTableEntityTableContext( cloudStorageAccount ) )
      {
         _cloudStorageAccount = cloudStorageAccount;

         if ( !_servicePointsUpdated.ContainsKey( _cloudStorageAccount.TableEndpoint ) || !_servicePointsUpdated[_cloudStorageAccount.TableEndpoint] )
         {
            ServicePoint servicePoint = ServicePointManager.FindServicePoint( new Uri( _cloudStorageAccount.TableEndpoint ) );
            servicePoint.Expect100Continue = false;
            servicePoint.ConnectionLimit = 48;
         }
      }
   }
}