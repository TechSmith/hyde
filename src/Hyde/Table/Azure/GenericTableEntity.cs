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

      private static readonly HashSet<string> _invalidPropertyNames = new HashSet<string> { "PartitionKey", "RowKey", "Timestamp", "ETag" };

      private static readonly Dictionary<Type, Func<EntityProperty, object>> _typeToValueFunctions = new Dictionary<Type, Func<EntityProperty, object>>
        {
           { typeof( string ), p => p.StringValue },
           { typeof( int ), p => p.Int32Value },
           { typeof( int? ), p => IsNull( p ) ? (int?) null : p.Int32Value },
           { typeof( double ), p => p.DoubleValue },
           { typeof( double? ), p => IsNull( p ) ? (double?) null : p.DoubleValue },
           { typeof( byte[] ), p => p.BinaryValue },
           { typeof( Guid ), p => p.GuidValue },
           { typeof( Guid? ), p => IsNull(p)  ? (Guid?) null : p.GuidValue },
           { typeof( DateTime ), p => p.DateTimeOffsetValue.Value.UtcDateTime },
           { typeof( DateTime? ), p => IsNull( p )  ? (DateTime?) null : p.DateTimeOffsetValue.Value.UtcDateTime },
           { typeof( bool ), p => p.BooleanValue },
           { typeof( bool? ), p => IsNull( p ) ? (bool?) null : p.BooleanValue },
           { typeof( long ), p => p.Int64Value },
           { typeof( long? ), p => IsNull( p ) ? (long?) null : p.Int64Value },
           { typeof( Uri ), p => IsNull( p ) ? null : new Uri( p.StringValue ) },
        };

      private static readonly PropertyInfo _isNullProperty = typeof( EntityProperty ).GetProperty( "IsNull", BindingFlags.NonPublic | BindingFlags.Instance );
      private static bool IsNull( EntityProperty entityProperty )
      {
         // For some reason, IsNull is an internal property. Hopefully it will be made public in the future
         return (bool) _isNullProperty.GetValue( entityProperty, null );
      }

      private static readonly Dictionary<Type, Func<object, EntityProperty>> _typeToEntityPropertyFunctions = new Dictionary<Type, Func<object, EntityProperty>>
        {
           { typeof( string ), o => new EntityProperty( (string) o) },
           { typeof( int ), o => new EntityProperty( (int) o) },
           { typeof( int? ), o => o == null ? new EntityProperty( (string) null ) : new EntityProperty( (int) o) },
           { typeof( double ), o => new EntityProperty((double) o) },
           { typeof( double? ), o => o == null ? new EntityProperty((string) null ) : new EntityProperty((double) o) },
           { typeof( byte[] ), o => new EntityProperty((byte[]) o) },
           { typeof( Guid ), o => new EntityProperty((Guid) o)},
           { typeof( Guid? ), o => o== null ? new EntityProperty((string) null) : new EntityProperty((Guid) o)},
           { typeof( DateTime ), o => new EntityProperty( (DateTime) o)  },
           { typeof( DateTime? ), o => new EntityProperty( (DateTime?) o)  },
           { typeof( bool ), o => new EntityProperty((bool) o)},
           { typeof( bool? ), o => o== null ? new EntityProperty((string) null): new EntityProperty((bool) o) },
           { typeof( long ), o => new EntityProperty((long) o)},
           { typeof( long? ), o => o == null ? new EntityProperty((string) null):new EntityProperty((long) o ) },
           { typeof( Uri ), o => o == null ? new EntityProperty((string) null) : new EntityProperty( ((Uri)o).AbsoluteUri) },
        };

      private static readonly Dictionary<EdmType, Func<EntityProperty, object>> _edmTypeToConverterFunction = new Dictionary<EdmType, Func<EntityProperty, object>>
        {
           { EdmType.Binary, p => p.BinaryValue },
           { EdmType.Boolean, p => IsNull( p ) ? (bool?) null : p.BooleanValue },
           { EdmType.DateTime, p => p.DateTimeOffsetValue.HasValue ? p.DateTimeOffsetValue.Value.UtcDateTime : (DateTime?) null  },
           { EdmType.Double, p => IsNull( p ) ? (double?) null : p.DoubleValue },
           { EdmType.Guid, p => IsNull( p ) ? (Guid?) null : p.GuidValue },
           { EdmType.Int32, p => IsNull( p ) ? (int?) null : p.Int32Value },
           { EdmType.Int64, p => IsNull( p ) ? (long?) null : p.Int64Value },
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

      public static GenericTableEntity HydrateFrom( dynamic sourceItem, string partitionKey, string rowKey )
      {
         Dictionary<string, EntityProperty> properties = GetProperties( sourceItem );

         var genericEntity = new GenericTableEntity();
         if ( properties != null )
         {
            foreach ( KeyValuePair<string, EntityProperty> keyValuePair in properties )
            {
               if ( _invalidPropertyNames.Contains( keyValuePair.Key ) )
               {
                  throw new InvalidEntityException( string.Format( "Invalid property name {0}", keyValuePair.Key ) );
               }
               genericEntity._properties.Add( keyValuePair.Key, keyValuePair.Value );
            }
         }

         genericEntity.PartitionKey = partitionKey;
         genericEntity.RowKey = rowKey;

         return genericEntity;
      }

      private static Dictionary<string, EntityProperty> GetProperties( dynamic item )
      {
         if ( item is IDynamicMetaObjectProvider )
         {
            return GetPropertiesFromDynamicMetaObject( item );
         }
         return GetPropertiesFromType( item );
      }

      private static Dictionary<string, EntityProperty> GetPropertiesFromDynamicMetaObject( IDynamicMetaObjectProvider item )
      {
         var properties = new Dictionary<string, EntityProperty>();
         IEnumerable<string> memberNames = ImpromptuInterface.Impromptu.GetMemberNames( item );
         foreach ( var memberName in memberNames )
         {
            dynamic result = ImpromptuInterface.Impromptu.InvokeGet( item, memberName );
            Func<object, EntityProperty> objectToEntityPropertyConverter = _typeToEntityPropertyFunctions[result.GetType()];
            properties[memberName] = objectToEntityPropertyConverter( result );
         }
         return properties;
      }

      private static Dictionary<string, EntityProperty> GetPropertiesFromType<T>( T item )
      {
         var properties = new Dictionary<string, EntityProperty>();
         foreach ( var property in item.GetType().GetProperties().Where( p => p.ShouldSerialize() ) )
         {
            if ( _invalidPropertyNames.Contains( property.Name ) )
            {
               throw new InvalidEntityException( string.Format( "Invalid property name {0}", property.Name ) );
            }
            if ( !_typeToEntityPropertyFunctions.ContainsKey( property.PropertyType ) )
            {
               throw new NotSupportedException( "The type " + property.PropertyType + " is not supported." );
            }

            Func<object, EntityProperty> objectToEntityPropertyConverter = _typeToEntityPropertyFunctions[property.PropertyType];
            var valueOfProperty = property.GetValue( item, null );
            properties[property.Name] = objectToEntityPropertyConverter( valueOfProperty );
         }
         return properties;
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
         if ( _invalidPropertyNames.Contains( typeProperty.Name ) )
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