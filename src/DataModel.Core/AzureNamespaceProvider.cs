using System.Xml.Linq;

namespace TechSmith.CloudServices.DataModel.Core
{
   internal static class AzureNamespaceProvider
   {
      public static XNamespace AstoriaMetadataNamespace
      {
         get
         {
            return "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
         }
      }

      public static XNamespace AstoriaDataNamespace
      {
         get
         {
            return "http://schemas.microsoft.com/ado/2007/08/dataservices";
         }
      }

      public static XNamespace AtomNamespace
      {
         get
         {
            return "http://www.w3.org/2005/Atom";
         }
      }
   }
}