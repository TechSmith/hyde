using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;
using TechSmith.Hyde.Table.Azure.ObjectToTypeConverters;

namespace TechSmith.Hyde.Table.Azure
{
   internal class GenericTableEntity : ITableEntity
   {
      private IDictionary<string, EntityProperty> _properties;

      private static readonly Dictionary<EdmType, Func<EntityProperty, object>> _edmTypeToConverterFunction = new Dictionary<EdmType, Func<EntityProperty, object>>
        {
           { EdmType.Binary, p => p.BinaryValue },
           { EdmType.Boolean, p => p.BooleanValue.HasValue ? p.BooleanValue.Value : (bool?) null },
           { EdmType.DateTime, p => p.DateTimeOffsetValue.HasValue ? p.DateTimeOffsetValue.Value.UtcDateTime : (DateTime?) null  },
           { EdmType.Double, p => p.DoubleValue.HasValue ? p.DoubleValue.Value : (double?) null },
           { EdmType.Guid, p => p.GuidValue.HasValue ? p.GuidValue.Value : (Guid?) null },
           { EdmType.Int32, p => p.Int32Value.HasValue ? p.Int32Value.Value : (int?) null },
           { EdmType.Int64, p => p.Int64Value.HasValue ? p.Int64Value.Value : (long?) null },
           { EdmType.String, p => p.StringValue },
        };

      public string PartitionKey
      {
         get;
         set;
      }

      public string RowKey
      {
         get;
         set;
      }

      public DateTimeOffset Timestamp
      {
         get;
         set;
      }

      public string ETag
      {
         get;
         set;
      }

      public GenericTableEntity()
      {
         _properties = new Dictionary<string, EntityProperty>();
      }

      public void ReadEntity( IDictionary<string, EntityProperty> properties, OperationContext operationContext )
      {
         _properties = new Dictionary<string, EntityProperty>( properties );
      }

      public IDictionary<string, EntityProperty> WriteEntity( OperationContext operationContext )
      {
         return new Dictionary<string, EntityProperty>( _properties );
      }

      public void SetProperty( string name, EntityProperty newValue )
      {
         _properties[name] = newValue;
      }

      public static GenericTableEntity HydrateFrom( TableItem tableItem )
      {
         Dictionary<string, EntityProperty> properties = GetProperties( tableItem.Properties );

         var genericEntity = new GenericTableEntity();
         foreach ( KeyValuePair<string, EntityProperty> keyValuePair in properties )
         {
            genericEntity._properties.Add( keyValuePair.Key, keyValuePair.Value );
         }

         genericEntity.PartitionKey = tableItem.PartitionKey;
         genericEntity.RowKey = tableItem.RowKey;

         return genericEntity;
      }

      private static Dictionary<string, EntityProperty> GetProperties( Dictionary<string, Tuple<object, Type>> properties )
      {
         var entityProperties = new Dictionary<string, EntityProperty>();
         foreach ( KeyValuePair<string, Tuple<object, Type>> property in properties )
         {
            Type propertyType = property.Value.Item2;

            IObjectToTypeConverter converter = ObjectConverterFactory.GetConverterFor( propertyType );
            entityProperties[property.Key] = converter.ConvertToEntityProperty( property.Value.Item1, propertyType );
         }

         return entityProperties;
      }

      public dynamic ConvertToDynamic()
      {
         dynamic newItem = new ExpandoObject();

         if ( _properties == null )
         {
            return newItem;
         }

         foreach ( KeyValuePair<string, EntityProperty> property in _properties )
         {
            Func<EntityProperty, object> func = _edmTypeToConverterFunction[property.Value.PropertyType];
            ( (IDictionary<string, object>) newItem ).Add( property.Key, func( property.Value ) );
         }

         ( (IDictionary<string, object>) newItem ).Add( "PartitionKey", PartitionKey );
         ( (IDictionary<string, object>) newItem ).Add( "RowKey", RowKey );

         return newItem;
      }

      public T ConvertTo<T>() where T : new()
      {
         Type objectType = typeof( T );

         if ( _properties == null )
         {
            return new T();
         }

         var newItem = new T();

         SetRowKeyAndPartitionKeyOnObject( newItem );

         objectType.GetProperties().Where( p => p.ShouldSerialize() )
                                   .Where( p => _properties.ContainsKey( p.Name ) )
                                   .ToList()
                                   .ForEach( p => SetPropertyOnObject( p, newItem ) );

         return newItem;
      }

      private void SetPropertyOnObject<T>( PropertyInfo typeProperty, T newItem ) where T : new()
      {
         if ( TableConstants.ReservedPropertyNames.Contains( typeProperty.Name ) )
         {
            return;
         }

         IObjectToTypeConverter objectConverter = ObjectConverterFactory.GetConverterFor( typeProperty.PropertyType );
         typeProperty.SetValue( newItem, objectConverter.ConvertToValue( _properties[typeProperty.Name], typeProperty ), null );
      }

      private void SetRowKeyAndPartitionKeyOnObject<T>( T newItem ) where T : new()
      {
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
      }
   }
}