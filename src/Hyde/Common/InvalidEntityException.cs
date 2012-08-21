using System;
using System.Runtime.Serialization;

namespace TechSmith.Hyde.Common
{
   [Serializable]
   public class InvalidEntityException : Exception
   {
      public InvalidEntityException()
      {
      }

      public InvalidEntityException( string message )
         : base( message )
      {
      }

      public InvalidEntityException( string message, Exception inner )
         : base( message, inner )
      {
      }

      protected InvalidEntityException( SerializationInfo info, StreamingContext context )
         : base( info, context )
      {
      }
   }
}
