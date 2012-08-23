using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace TechSmith.Hyde.Table.Azure
{
   internal class AzureGenericTableWriter
   {
      private static readonly Dictionary<Type, string> _typeToEdmMapping = new Dictionary<Type, string>
      {
        { typeof( int ), "Edm.Int32" },
        { typeof( double ), "Edm.Double" },
        { typeof( byte[] ), "Edm.Binary" },
        { typeof( Guid ), "Edm.Guid" },
        { typeof( DateTime ), "Edm.DateTime" },
        { typeof( bool ), "Edm.Boolean" },
        { typeof( long ), "Edm.Int64" },
        { typeof( string ), "Edm.String" },
        { typeof( int? ), "Edm.Int32" },
        { typeof( double? ), "Edm.Double" },
        { typeof( Guid? ), "Edm.Guid" },
        { typeof( DateTime? ), "Edm.DateTime"},
        { typeof( bool? ), "Edm.Boolean" },
        { typeof( long? ), "Edm.Int64" },
        { typeof( Uri ), "Edm.String"  }
      };

      private static readonly Dictionary<Type, Func<object, string>> _typeToXmlConverterFunction = new Dictionary<Type, Func<object, string>>()
      {
         { typeof( byte[] ), i => i == null ? null : Convert.ToBase64String( (byte[]) i ) },
         { typeof( DateTime ), i => DateTimeToString( (DateTime) i ) },
         { typeof( DateTime? ), i => i == null ? null : DateTimeToString( (DateTime) i ) },
         { typeof( bool ), i => XmlConvert.ToString( (bool) i ) },
         { typeof( bool? ), i => i == null ? null : XmlConvert.ToString( (bool) i ) },
         { typeof( double ), i => XmlConvert.ToString( (double) i ) },
         { typeof( Uri ), i => i == null ? null : ( (Uri) i ).AbsoluteUri },
      };

      private static string DateTimeToString( DateTime value )
      {
         return XmlConvert.ToString( value.ToUniversalTime(), XmlDateTimeSerializationMode.RoundtripKind );
      }

      public static void HandleWritingEntity( object sender, System.Data.Services.Client.ReadingWritingEntityEventArgs e )
      {
         XElement properties = e.Data.Descendants( AzureNamespaceProvider.AstoriaMetadataNamespace + "properties" ).First();

         var entity = e.Entity as GenericEntity;
         if ( entity != null )
         {
            foreach ( string key in entity.GetProperties().Keys )
            {
               var value = entity[key];
               Type propertyType = value.ObjectType;
               ValidatePropertyType( propertyType );
               var property = GenericEntityPropertyToXElement( propertyType, key, value );
               MapType( AzureNamespaceProvider.AstoriaMetadataNamespace, propertyType, property );
               properties.Add( property );
            }
         }
      }

      private static void MapType( XNamespace m, Type propertyType, XElement property )
      {
         if ( _typeToEdmMapping.ContainsKey( propertyType ) )
         {
            property.Add( new XAttribute( m + "type", _typeToEdmMapping[propertyType] ) );
         }
      }

      private static void ValidatePropertyType( Type type )
      {
         if ( !_typeToEdmMapping.ContainsKey( type ) && type != typeof( string ) )
         {
            throw new NotSupportedException( "The type " + type + " is not supported." );
         }
      }

      private static XElement GenericEntityPropertyToXElement( Type type, string key, EntityPropertyInfo entityInfo )
      {
         string elementValue;
         if ( _typeToXmlConverterFunction.ContainsKey( type ) )
         {
            elementValue = _typeToXmlConverterFunction[type]( entityInfo.Value );
         }
         else
         {
            elementValue = entityInfo.IsNull ? null : entityInfo.Value.ToString();
         }

         var element = new XElement( AzureNamespaceProvider.AstoriaDataNamespace + key, elementValue );
         if ( entityInfo.IsNull )
         {
            element.Add( new XAttribute( AzureNamespaceProvider.AstoriaMetadataNamespace + "null", "true" ) );
         }

         return element;
      }
   }
}