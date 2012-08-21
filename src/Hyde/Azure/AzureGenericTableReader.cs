using System;
using System.Collections.Generic;
using System.Linq;

namespace TechSmith.Hyde.Azure
{
   internal class AzureGenericTableReader
   {
      private static readonly Dictionary<string, Type> _edmToTypeMapping = new Dictionary<string, Type>
      {
         {"Edm.Int32", typeof( int ) },
         {"Edm.Double", typeof( double ) },
         { "Edm.Binary", typeof( byte[] ) },
         {"Edm.Guid", typeof( Guid ) },
         {"Edm.DateTime", typeof( DateTime )},
         {"Edm.Boolean", typeof( bool ) },
         {"Edm.Int64", typeof( long ) },
         {"Edm.String", typeof( string ) },
      };

      // Taken from: http://msdn.microsoft.com/en-us/library/dd894027.aspx

      public static void HandleReadingEntity( object sender, System.Data.Services.Client.ReadingWritingEntityEventArgs e )
      {
         var entity = e.Entity as GenericEntity;
         if ( entity == null )
         {
            return;
         }

         var properties = from property in e.Data.Element( AzureNamespaceProvider.AtomNamespace + "content" )
                             .Element( AzureNamespaceProvider.AstoriaMetadataNamespace + "properties" )
                             .Elements()
                          select new
                                 {
                                    Name = property.Name.LocalName,
                                    IsNull = string.Equals( "true", property.Attribute( AzureNamespaceProvider.AstoriaMetadataNamespace + "null" ) == null ? null : property.Attribute( AzureNamespaceProvider.AstoriaMetadataNamespace + "null" ).Value, StringComparison.OrdinalIgnoreCase ),
                                    TypeName = property.Attribute( AzureNamespaceProvider.AstoriaMetadataNamespace + "type" ) == null ? null : property.Attribute( AzureNamespaceProvider.AstoriaMetadataNamespace + "type" ).Value,
                                    property.Value
                                 };

         foreach ( var property in properties )
         {
            entity[property.Name] = new EntityPropertyInfo( property.Value, GetType( property.TypeName ), property.IsNull );
         }
      }

      private static Type GetType( string type )
      {
         return type == null ? typeof( string ) : _edmToTypeMapping[type];
      }
   }
}