using System;
using System.Runtime.Serialization;

namespace TechSmith.Hyde.Common
{
   [Serializable]
   public class EntityAlreadyExistsException : Exception
   {
      public EntityAlreadyExistsException()
      {
      }

      public EntityAlreadyExistsException( string partitionKey, string rowKey, Exception inner )
         : base( string.Format( "Entity with partition key {0} and row key {1} was not found.", partitionKey, rowKey ), inner )
      {
      }

      public EntityAlreadyExistsException( string message )
         : base( message )
      {
      }

      public EntityAlreadyExistsException( string message, Exception inner )
         : base( message, inner )
      {
      }

      protected EntityAlreadyExistsException( SerializationInfo info, StreamingContext context )
         : base( info, context )
      {
      }
   }
}
