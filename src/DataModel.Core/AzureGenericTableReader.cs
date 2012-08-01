using System;
using System.Collections.Generic;
using System.Linq;

namespace TechSmith.CloudServices.DataModel.Core
{
   internal class AzureGenericTableReader
   {
      private static readonly Dictionary<string, Type> _edmToTypeMapping = new Dictionary<string, Type>();

      static AzureGenericTableReader()
      {
         _edmToTypeMapping.Add( "Edm.Int32", typeof( int ) );
         _edmToTypeMapping.Add( "Edm.Double", typeof( double ) );
         _edmToTypeMapping.Add( "Edm.Binary", typeof( byte[] ) );
         _edmToTypeMapping.Add( "Edm.Guid", typeof( Guid ) );
         _edmToTypeMapping.Add( "Edm.DateTime", typeof( DateTime ) );
         _edmToTypeMapping.Add( "Edm.Boolean", typeof( bool ) );
         _edmToTypeMapping.Add( "Edm.Int64", typeof( long ) );
         _edmToTypeMapping.Add( "Edm.String", typeof( string ) );
      }

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