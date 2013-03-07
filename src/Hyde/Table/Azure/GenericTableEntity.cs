using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Table.Azure
{
   internal class GenericTableEntity : ITableEntity
   {
      private IDictionary<string, EntityProperty> _properties;

      private static readonly Dictionary<Type, Func<EntityProperty, object>> _typeToValueFunctions = new Dictionary<Type, Func<EntityProperty, object>>
        {
           { typeof( string ), p => p.StringValue },
           { typeof( int ), p => p.Int32Value.Value },
           { typeof( int? ), p => p.Int32Value },
           { typeof( double ), p => p.DoubleValue.Value },
           { typeof( double? ), p => p.DoubleValue },
           { typeof( byte[] ), p => p.BinaryValue },
           { typeof( Guid ), p => p.GuidValue.Value },
           { typeof( Guid? ), p => p.GuidValue },
           { typeof( DateTime ), p => p.DateTimeOffsetValue.Value.UtcDateTime },
           { typeof( DateTime? ), p => p.DateTimeOffsetValue.HasValue ? p.DateTimeOffsetValue.Value.UtcDateTime : (DateTime?) null },
           { typeof( bool ), p => p.BooleanValue.Value },
           { typeof( bool? ), p => p.BooleanValue },
           { typeof( long ), p => p.Int64Value.Value },
           { typeof( long? ), p => p.Int64Value },
           { typeof( Uri ), p => string.IsNullOrWhiteSpace( p.StringValue ) ? null : new Uri( p.StringValue ) },
        };

      private static readonly Dictionary<Type, Func<object, EntityProperty>> _typeToEntityPropertyFunctions = new Dictionary<Type, Func<object, EntityProperty>>
        {
           { typeof( string ), o => new EntityProperty( (string) o) },
           { typeof( int ), o => new EntityProperty( (int) o) },
           { typeof( int? ), o => o == null ? new EntityProperty( (int?) null ) : new EntityProperty( (int) o) },
           { typeof( double ), o => new EntityProperty((double) o) },
           { typeof( double? ), o => o == null ? new EntityProperty((double?) null ) : new EntityProperty((double) o) },
           { typeof( byte[] ), o => new EntityProperty((byte[]) o) },
           { typeof( Guid ), o => new EntityProperty((Guid) o)},
           { typeof( Guid? ), o => o == null ? new EntityProperty((Guid?) null) : new EntityProperty((Guid) o)},
           { typeof( DateTime ), o => new EntityProperty( (DateTime) o)  },
           { typeof( DateTime? ), o => new EntityProperty( (DateTime?) o)  },
           { typeof( bool ), o => new EntityProperty((bool) o)},
           { typeof( bool? ), o => o== null ? new EntityProperty((bool?) null): new EntityProperty((bool) o) },
           { typeof( long ), o => new EntityProperty((long) o)},
           { typeof( long? ), o => o == null ? new EntityProperty((long?) null):new EntityProperty((long) o ) },
           { typeof( Uri ), o => o == null ? new EntityProperty((string) null) : new EntityProperty( ((Uri)o).AbsoluteUri) },
        };

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
            if ( !_typeToEntityPropertyFunctions.ContainsKey( propertyType ) )
            {
               throw new NotSupportedException( "The type " + propertyType + " is not supported." );
            }

            Func<object, EntityProperty> objectToEntityPropertyConverter = _typeToEntityPropertyFunctions[propertyType];
            entityProperties[property.Key] = objectToEntityPropertyConverter( property.Value.Item1 );
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

         Func<EntityProperty, object> funcToCall = _typeToValueFunctions[typeProperty.PropertyType];
         object propertyValue = funcToCall( _properties[typeProperty.Name] );

         typeProperty.SetValue( newItem, propertyValue, null );
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