using System;

namespace TechSmith.CloudServices.DataModel.Core
{
   public abstract class AbstractQueue
   {
      public abstract void AddMessage( string message );
      public abstract void DeleteMessage( IQueueMessage message );

      public virtual IQueueMessage GetMessage( TimeSpan? visibilityTimeout )
      {
         IQueueMessage message;

         do
         {
            message = TryGetMessage( visibilityTimeout );

            if ( message == null )
            {
               return null;
            }

            if ( IsMessagePoisoned( message ) )
            {
               HandlePoisonMessage( message );

               message = null;
            }
         }
         while ( message == null );

         return message;
      }

      protected abstract IQueueMessage TryGetMessage( TimeSpan? visibilityTimeout );
      protected abstract bool IsMessagePoisoned( IQueueMessage message );
      protected abstract void HandlePoisonMessage( IQueueMessage message );
   }
}
