using System;
using System.Runtime.Serialization;

namespace TechSmith.Hyde.Common
{
   [Serializable]
   public class EntityDoesNotExistException : Exception
   {
      public EntityDoesNotExistException()
      {
      }

      public EntityDoesNotExistException( string partitionKey, string rowKey, Exception inner )
         : base( string.Format( "Entity with partition key {0} and row key {1} was not found.", partitionKey, rowKey ), inner )
      {
      }

      public EntityDoesNotExistException( string message )
         : base( message )
      {
      }

      public EntityDoesNotExistException( string message, Exception inner )
         : base( message, inner )
      {
      }

      protected EntityDoesNotExistException( SerializationInfo info, StreamingContext context )
         : base( info, context )
      {
      }
   }
}
