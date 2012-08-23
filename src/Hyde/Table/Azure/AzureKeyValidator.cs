using System.Data.Services.Client;

namespace TechSmith.Hyde.Table.Azure
{
   internal class AzureKeyValidator
   {
      public static void ValidatePartitionKey( string partitionKey )
      {
         ValidateKeyLength( partitionKey );
         ValidateKeyCharacters( partitionKey );
      }

      public static void ValidateRowKey( string rowKey )
      {
         ValidateKeyLength( rowKey );
         ValidateKeyCharacters( rowKey );
      }

      private static void ValidateKeyCharacters( string key )
      {
         // based on requirements from Azure: http://msdn.microsoft.com/en-us/library/windowsazure/dd179338.aspx
         bool invalid = false;
         invalid |= key.Contains( "/" );
         invalid |= key.Contains( "\\" );
         invalid |= key.Contains( "#" );
         invalid |= key.Contains( "?" );

         if ( invalid )
         {
            throw new DataServiceRequestException( "Invalid key specified. Invalid characters in key" );
         }
      }

      private static void ValidateKeyLength( string key )
      {
         // based on requirements from Azure: http://msdn.microsoft.com/en-us/library/windowsazure/dd179338.aspx
         // keys are limited to 1024 bytes which equals 512 UTF16 characters
         bool invalid = key.Length > 512;

         if ( invalid )
         {
            throw new DataServiceRequestException( "Invalid key specified. Key length is above 512 characters." );
         }
      }
   }
}