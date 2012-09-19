using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Table.Azure
{
   // Modified from Jai Haridas: http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/481afa1b-03a9-42d9-ae79-9d5dc33b9297/
   internal class GenericEntity : Microsoft.WindowsAzure.StorageClient.TableServiceEntity
   {
      private static readonly HashSet<string> InvalidPropertyNames = new HashSet<string>
                                                                     {
                                                                        "PartitionKey",
                                                                        "RowKey",
                                                                     };

      private readonly Dictionary<string, EntityPropertyInfo> _properties = new Dictionary<string, EntityPropertyInfo>();

      private static readonly Dictionary<Type, Func<string, object>> _typeToConverterFunction = new Dictionary<Type, Func<string, object>>
      {
         { typeof( string ), i => i },
         { typeof( int ), i => Int32.Parse( i ) },
         { typeof( int? ), i => Int32.Parse( i ) },
         { typeof( double ), i => Double.Parse( i ) },
         { typeof( double? ), i => Double.Parse( i ) },
         { typeof( byte[] ), Convert.FromBase64String },
         { typeof( Guid ), i => Guid.Parse( i ) },
         { typeof( Guid? ), i => Guid.Parse( i ) },
         { typeof( DateTime ), i => DateTime.Parse( i, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal ) },
         { typeof( DateTime? ), i => DateTime.Parse( i, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal ) },
         { typeof( bool ), i => Boolean.Parse( i ) },
         { typeof( bool? ), i => Boolean.Parse( i ) },
         { typeof( long ), i => Int64.Parse( i ) },
         { typeof( long? ), i => Int64.Parse( i ) },
         { typeof( Uri ), i => new Uri( i ) },
      };

      internal EntityPropertyInfo this[string key]
      {
         get
         {
            return _properties.ContainsKey( key ) ? _properties[key] : new EntityPropertyInfo( null, typeof( object ), true );
         }

         set
         {
            _properties[key] = value;
         }
      }

      public Dictionary<string, EntityPropertyInfo> GetProperties()
      {
         return _properties;
      }

      public override string ToString()
      {
         return string.Empty;
      }

      public T CreateInstanceFromProperties<T>() where T : new()
      {
         var newItem = new T();

         var partitionKeyProperty = newItem.FindPropertyDecoratedWith<PartitionKeyAttribute>();
         if ( partitionKeyProperty != null )
         {
            partitionKeyProperty.SetValue( newItem, PartitionKey, null );
         }

         var rowKeyProperty = newItem.FindPropertyDecoratedWith<RowKeyAttribute>();
         if ( rowKeyProperty != null )
         {
            rowKeyProperty.SetValue( newItem, RowKey, null );
         }

         foreach ( var property in newItem.GetType().GetProperties().Where( p => p.ShouldSerialize() ) )
         {
            var valueToConvert = this[property.Name];
            var convertedValue = TypeConverter( valueToConvert, property.PropertyType );
            property.SetValue( newItem, convertedValue, null );
         }

         return newItem;
      }

      public dynamic CreateInstanceFromProperties()
      {
         dynamic newItem = new ExpandoObject();

         foreach ( var entityPropertyInfo in GetProperties() )
         {
            var value = TypeConverter( entityPropertyInfo.Value, entityPropertyInfo.Value.ObjectType );

            ( (IDictionary<string, object>) newItem ).Add( entityPropertyInfo.Key, value );
         }

         return newItem;
      }

      private static object TypeConverter( EntityPropertyInfo itemToConvert, Type type )
      {
         if ( itemToConvert.IsNull )
         {
            return null;
         }

         var funcToCall = _typeToConverterFunction[type];

         return funcToCall( itemToConvert.Value.ToString() );
      }

      public static GenericEntity HydrateFrom<T>( T sourceItem ) where T : new()
      {
         string partitionKey = sourceItem.ReadPropertyDecoratedWith<PartitionKeyAttribute, string>();
         string rowKey = sourceItem.ReadPropertyDecoratedWith<RowKeyAttribute, string>();

         return HydrateFrom( sourceItem, partitionKey, rowKey );
      }

      public static GenericEntity HydrateFrom<T>( T sourceItem, string partitionKey, string rowKey ) where T : new()
      {
         var genericEntity = new GenericEntity();
         foreach ( var property in sourceItem.GetType().GetProperties().Where( p => p.ShouldSerialize() ) )
         {
            if ( InvalidPropertyNames.Contains( property.Name ) )
            {
               throw new InvalidEntityException( string.Format( "Invalid property name {0}", property.Name ) );
            }
            var valueOfProperty = property.GetValue( sourceItem, null );
            genericEntity[property.Name] = new EntityPropertyInfo( valueOfProperty, property.PropertyType, valueOfProperty == null );
         }

         genericEntity.PartitionKey = partitionKey;
         genericEntity.RowKey = rowKey;

         return genericEntity;
      }

      public bool AreTheseEqual( GenericEntity rightSide )
      {
         return GetProperties()
            .All( propertyInfo => propertyInfo.Value == rightSide.GetProperties()[propertyInfo.Key].Value );
      }
   }
}