using System.IO;
using Microsoft.Extensions.Configuration;

namespace TechSmith.Hyde.IntegrationTest
{
   public static class Configuration
   {
      public static IConfigurationRoot Current
      {
         get;
      } = GetConfiguration();

      private static IConfigurationRoot GetConfiguration()
      {
         var configBuilder = new ConfigurationBuilder().AddJsonFile( "appsettings.json" );
         configBuilder.SetBasePath( Directory.GetCurrentDirectory() );
         return configBuilder.Build();
      }

      public static ICloudStorageAccount GetTestStorageAccount()
      {
         return new ConnectionStringCloudStorageAccount( Current["storageConnectionString"] );
      }

      public static ICloudStorageAccount GetTestCosmosStorageAccount()
      {
         return new ConnectionStringCloudStorageAccount( Current["cosmosConnectionString"] );
      }
   }
}