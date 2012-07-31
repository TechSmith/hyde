using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Net;
using Microsoft.WindowsAzure.StorageClient;

namespace TechSmith.CloudServices.DataModel.Core
{
   // Modified from Jai Haridas: http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/481afa1b-03a9-42d9-ae79-9d5dc33b9297/
   internal class AzureTableContext : TableServiceContext, ITableContext
   {
      public AzureTableContext( ICloudStorageAccount provider )
         : base( provider.TableEndpoint, provider.Credentials )
      {
         SendingRequest += SendingRequestWithNewVersion;
         ReadingEntity += AzureGenericTableReader.HandleReadingEntity;
         WritingEntity += AzureGenericTableWriter.HandleWritingEntity;

         RetryPolicy = RetryPolicies.RetryExponential( 4, TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 5 ), TimeSpan.FromSeconds( 1 ) );
      }

      private static void SendingRequestWithNewVersion( object sender, SendingRequestEventArgs e )
      {
         var request = e.Request as HttpWebRequest;

         if ( IsLocalDevRequest( request.Host ) )
         {
            // Local storage doesn't support this header version which is required for Upsert, so do not set it.
            // This means upsert doesn't work locally
            return;
         }

         const string StorageVersionHeader = "x-ms-version";
         const string August2011Version = "2011-08-18";
         request.Headers[StorageVersionHeader] = August2011Version;
      }

      private static bool IsLocalDevRequest( string hostName )
      {
         return hostName.Contains( "127.0.0.1" ) || hostName.Contains( "localhost" );
      }

      public void AddNewItem<T>( string tableName, T itemToAdd, string partitionKey, string rowKey ) where T : new()
      {
         try
         {
            AddObject( tableName, GenericEntity.HydrateGenericEntityFromItem( itemToAdd, partitionKey, rowKey ) );
         }
         catch ( InvalidOperationException ex )
         {
            throw new EntityAlreadyExistsException( "Entity already exists", ex );
         }
      }

      public T GetItem<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         var valueAsGeneric = GetItemAsGenericEntity<T>( tableName, partitionKey, rowKey );

         return valueAsGeneric.CreateInstanceFromProperties<T>();
      }

      private GenericEntity GetItemAsGenericEntity<T>( string tableName, string partitionKey, string rowKey ) where T : new()
      {
         GenericEntity valueAsGeneric;
         try
         {
            valueAsGeneric = CreateQuery<GenericEntity>( tableName )
                       .Where( p => p.PartitionKey == partitionKey && p.RowKey == rowKey )
                       .FirstOrDefault();
         }
         catch ( DataServiceQueryException ex )
         {
            if ( ex.Response.StatusCode == 404 )
            {
               throw new EntityDoesNotExistException( partitionKey, rowKey, ex );
            }
            throw;
         }
         return valueAsGeneric;
      }

      public IEnumerable<T> GetCollection<T>( string tableName, string partitionKey ) where T : new()
      {
         return CreateQuery<GenericEntity>( tableName )
            .Where( p => p.PartitionKey == partitionKey )
            .AsTableServiceQuery()
            .ToArray()
            .Select( g => g.CreateInstanceFromProperties<T>() );
      }

      public IEnumerable<T> GetRange<T>( string tableName, string partitionKeyLow, string partitionKeyHigh ) where T : new()
      {
         return CreateQuery<GenericEntity>( tableName )
            .Where( p => p.PartitionKey.CompareTo( partitionKeyLow ) >= 0 &&
                         p.PartitionKey.CompareTo( partitionKeyHigh ) <= 0 )
            .AsTableServiceQuery()
            .ToArray()
            .Select( g => g.CreateInstanceFromProperties<T>() );
      }

      public void Save()
      {
         try
         {
            SaveChangesWithRetries( SaveChangesOptions.ReplaceOnUpdate );
         }
         catch ( DataServiceRequestException ex )
         {
            var innerException = ex.InnerException as DataServiceClientException;
            if ( innerException != null )
            {
               if ( innerException.StatusCode == 404 )
               {
                  throw new EntityDoesNotExistException( "Entity did not exist when trying to update.", ex );
               }
               else if ( innerException.Message.Contains( "EntityAlreadyExists" ) )
               {
                  throw new EntityAlreadyExistsException( "Entity already exists", ex );
               }
            }
            throw;
         }
      }

      public void Upsert<T>( string tableName, T itemToUpsert, string partitionKey, string rowKey ) where T : new()
      {
         if ( IsLocalDevRequest( BaseUri.Host ) )
         {
            UpsertForDevStorage( tableName, itemToUpsert, partitionKey, rowKey );
         }
         else
         {
            var genericToUpsert = GenericEntity.HydrateGenericEntityFromItem( itemToUpsert, partitionKey, rowKey );
            AttachTo( tableName, genericToUpsert );
            UpdateObject( genericToUpsert );
         }
      }

      private void UpsertForDevStorage<T>( string tableName, T itemToUpsert, string partitionKey, string rowKey ) where T : new()
      {
         try
         {
            var genericToUpsert = GenericEntity.HydrateGenericEntityFromItem( itemToUpsert, partitionKey, rowKey );
            var genericInStorage = GetItemAsGenericEntity<T>( tableName, partitionKey, rowKey );
            if ( !genericToUpsert.AreTheseEqual( genericInStorage ) )
            {
               Detach( genericInStorage );
               Update( tableName, itemToUpsert, partitionKey, rowKey );
            }
         }
         catch ( EntityDoesNotExistException )
         {
            AddNewItem( tableName, itemToUpsert, partitionKey, rowKey );
         }
      }

      public void DeleteItem( string tableName, string partitionKey, string rowKey )
      {
         GenericEntity[] objectToDelete;
         try
         {
            objectToDelete = CreateQuery<GenericEntity>( tableName )
               .Where( p => p.PartitionKey == partitionKey && p.RowKey == rowKey ).ToArray();
         }
         catch ( DataServiceQueryException )
         {
            // table did not exist
            return;
         }

         if ( objectToDelete.Any() )
         {
            DeleteObject( objectToDelete.Single() );
         }
      }

      public void DeleteCollection( string tableName, string partitionKey )
      {
         GenericEntity[] objectsToDelete;
         try
         {
            objectsToDelete = CreateQuery<GenericEntity>( tableName )
               .Where( p => p.PartitionKey == partitionKey ).AsTableServiceQuery().ToArray();
         }
         catch ( DataServiceQueryException )
         {
            // table did not exist
            return;
         }

         if ( objectsToDelete.Any() )
         {
            foreach ( var genericEntity in objectsToDelete )
            {
               DeleteObject( genericEntity );
            }
         }
      }

      public void Update<T>( string tableName, T updatedItem, string partitionKey, string rowKey ) where T : new()
      {
         var genericToUpdate = GenericEntity.HydrateGenericEntityFromItem( updatedItem, partitionKey, rowKey );

         const string eTagThatSpecifiesWeShouldNotAddIfDoesNotExist = "*";
         AttachTo( tableName, genericToUpdate, eTagThatSpecifiesWeShouldNotAddIfDoesNotExist );
         UpdateObject( genericToUpdate );
      }
   }
}
