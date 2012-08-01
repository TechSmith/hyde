using System;
using Microsoft.WindowsAzure.StorageClient;

namespace TechSmith.CloudServices.DataModel.Core
{
   public class AzureQueueMessageAdapter : IQueueMessage
   {
      public CloudQueueMessage QueueMessage
      {
         get;
         private set;
      }

      public string Message
      {
         get
         {
            return QueueMessage.AsString;
         }
      }

      public CloudQueue CloudQueue
      {
         get;
         private set;
      }

      public AzureQueueMessageAdapter( CloudQueueMessage queueMessage, CloudQueue queue )
      {
         if ( queueMessage == null )
         {
            throw new ArgumentNullException( "queueMessage" );
         }
         if ( queue == null )
         {
            throw new ArgumentNullException( "queue" );
         }
         QueueMessage = queueMessage;
         CloudQueue = queue;
      }

      public bool IsPoison( IPoisonLimitProvider poisonLimitProvider )
      {
         return QueueMessage.DequeueCount > poisonLimitProvider.Limit;
      }
   }
}
