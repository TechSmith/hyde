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

      private static bool _setConcurrentConnectionLimit = true;

      /// <summary>
      /// If true (the default), each AzureTableStorageProvider sets the default connection limit for the
      /// ServicePoint corresponding to its table storage endpoint to TableStorageConcurrentConnectionLimit.
      /// </summary>
      public static bool SetConcurrentConnectionLimit
      {
         get
         {
            return _setConcurrentConnectionLimit;
         }
         set
         {
            _setConcurrentConnectionLimit = value;
            if ( value == false )
            {
               _servicePointsUpdated.Clear();
            }
         }
      }

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
            foreach ( var uriStr in _servicePointsUpdated.Keys )
            {
               UpdateServicePointConnectionLimit( new Uri( uriStr ), value );
            }
         }
      }

      private static void UpdateServicePointConnectionLimit( Uri uri, int newLimit )
      {
         ServicePoint servicePoint = ServicePointManager.FindServicePoint( uri );
         servicePoint.Expect100Continue = false;
         servicePoint.ConnectionLimit = newLimit;
      }

      public AzureTableStorageProvider( ICloudStorageAccount cloudStorageAccount )
         : base ( new AzureTableEntityTableContext( cloudStorageAccount ) )
      {
         _cloudStorageAccount = cloudStorageAccount;

         if ( !_setConcurrentConnectionLimit || _servicePointsUpdated.ContainsKey( _cloudStorageAccount.TableEndpoint ) )
         {
            return;
         }

         UpdateServicePointConnectionLimit( new Uri( _cloudStorageAccount.TableEndpoint ), _tableStorageConcurrentConnectionLimit );
         _servicePointsUpdated.AddOrUpdate( _cloudStorageAccount.TableEndpoint, true, (s,v) => true );
      }
   }
}