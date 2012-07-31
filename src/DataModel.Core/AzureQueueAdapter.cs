using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace TechSmith.CloudServices.DataModel.Core
{
   public class AzureQueueAdapter : AbstractQueue
   {
      private readonly List<CloudQueue> _theQueues;
      private readonly IPoisonLimitProvider _poisonLimitProvider;

      public AzureQueueAdapter( IQueueNameProvider queueNameProvider, IPoisonLimitProvider poisonLimitProvider )
      {
         var storageAccount = CloudStorageAccount.FromConfigurationSetting( "StorageAccountConnectionString" );
         var queueAccount = storageAccount.CreateCloudQueueClient();

         queueAccount.RetryPolicy = RetryPolicies.RetryExponential( 4, TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 5 ), TimeSpan.FromSeconds( 1 ) );

         _theQueues = new List<CloudQueue>();

         foreach ( var queueName in queueNameProvider.Names )
         {
            var tempQueue = queueAccount.GetQueueReference( queueName );
            tempQueue.CreateIfNotExist();

            _theQueues.Add( tempQueue  );
         }

         _poisonLimitProvider = poisonLimitProvider;
      }

      public override void AddMessage( string message )
      {
         var rand = new Random();

         _theQueues[rand.Next( 0, _theQueues.Count - 1 )].AddMessage( new CloudQueueMessage( message ) );
      }

      public override void DeleteMessage( IQueueMessage message )
      {
         var azureQueueMessage = message as AzureQueueMessageAdapter;

         if ( azureQueueMessage != null )
         {
            azureQueueMessage.CloudQueue.DeleteMessage( azureQueueMessage.QueueMessage );
         }
      }

      protected override bool IsMessagePoisoned( IQueueMessage message )
      {
         return message.IsPoison( _poisonLimitProvider );
      }

      protected override void HandlePoisonMessage( IQueueMessage message )
      {
         // TODO: Could put this message on a "poison" queue to handle later.
         // Delete this message.
         DeleteMessage( message );
      }

      protected override IQueueMessage TryGetMessage( TimeSpan? visibilityTimeout )
      {
         foreach ( var cloudQueue in GetRandomDistributionOfQueues() )
         {
            CloudQueueMessage message;
            if ( visibilityTimeout != null )
            {
               message = cloudQueue.GetMessage( (TimeSpan) visibilityTimeout );
            }
            else
            {
               // Use the default timeout.
               message = cloudQueue.GetMessage();
            }

            if ( message != null )
            {
               return new AzureQueueMessageAdapter( message, cloudQueue );
            }
         }

         return null;
      }

      private IEnumerable<CloudQueue> GetRandomDistributionOfQueues()
      {
         var rand = new Random();
         var shuffledList = new List<CloudQueue>( _theQueues );

         for ( int i = shuffledList.Count; i > 1; i-- )
         {
            int j = rand.Next( i );
            var tmp = shuffledList[j];
            shuffledList[j] = shuffledList[i - 1];
            shuffledList[i - 1] = tmp;
         }

         return shuffledList;
      }
   }
}