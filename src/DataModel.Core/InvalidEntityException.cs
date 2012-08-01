using System;

namespace TechSmith.CloudServices.DataModel.Core
{
   public class InvalidEntityException : Exception
   {
      public InvalidEntityException( string message )
         : base( message )
      {
      }

      public InvalidEntityException( string message, Exception innerException )
         : base( message, innerException )
      {
      }
   }
}
