using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Table
{
   public class TableItem
   {
      public string PartitionKey
      {
         get;
         private set;
      }
      public string RowKey
      {
         get;
         private set;
      }

      public Dictionary<string, Tuple<object, Type>> Properties
      {
         get;
         private set;
      }

      public TableItem( Dictionary<string, Tuple<object, Type>> properties )
      {
         Tuple<object, Type> value;
         properties.TryGetValue( TableConstants.PartitionKey, out value );
         properties.Remove( TableConstants.PartitionKey );
         PartitionKey = value == null ? null : value.Item1 as string;

         properties.TryGetValue( TableConstants.RowKey, out value );
         properties.Remove( TableConstants.RowKey );
         RowKey = value == null ? null : value.Item1 as string;

         Properties = properties;
      }

      public void VerifyRequiredKeys()
      {
         if ( string.IsNullOrEmpty( PartitionKey ) )
         {
            throw new ArgumentException( "Required Property PartitionKey missing" );
         }
         if ( string.IsNullOrEmpty( RowKey ) )
         {
            throw new ArgumentException( "Required Property RowKey missing" );
         }
      }

      public void SetKeys( string partitionKey, string rowKey )
      {
         if ( string.IsNullOrEmpty( PartitionKey ) )
         {
            PartitionKey = partitionKey;
         }
         else if ( PartitionKey != partitionKey )
         {
            throw new ArgumentException( string.Format( "Entity defines PartitionKey: {0} but it conflicts with partitionKey argument: {1}", PartitionKey, partitionKey ) );
         }

         if ( string.IsNullOrEmpty( RowKey ) )
         {
            RowKey = rowKey;
         }
         else if ( RowKey != rowKey )
         {
            throw new ArgumentException( string.Format( "Entity defines RowKey: {0} but it conflicts with rowKey argument: {1}", RowKey, rowKey ) );
         }
      }

      public static TableItem CreateAndThrowOnReservedProperties( dynamic item )
      {
         Dictionary<string, Tuple<object, Type>> properties = GetProperties( item );
         foreach ( string propertyName in TableConstants.ReservedPropertyNames )
         {
            if ( properties.ContainsKey( propertyName ) )
            {
               throw new InvalidEntityException( string.Format( "Reserved property name {0}", propertyName ) );
            }
         }

         TrySetValue<PartitionKeyAttribute>( item, properties );
         TrySetValue<RowKeyAttribute>( item, properties );

         return new TableItem( properties );
      }

      public static TableItem CreateAndIgnoreReservedProperties( dynamic item )
      {
         var properties = GetProperties( item );

         TrySetValue<PartitionKeyAttribute>( item, properties );
         TrySetValue<RowKeyAttribute>( item, properties );

         properties.Remove( TableConstants.Timestamp );
         properties.Remove( TableConstants.ETag );

         return new TableItem( properties );
      }

      private static Dictionary<string, Tuple<object, Type>> GetProperties( dynamic item )
      {
         Dictionary<string, Tuple<object, Type>> properties;

         if ( item is IDynamicMetaObjectProvider )
         {
            properties = GetPropertiesFromDynamicMetaObject( item );
         }
         else
         {
            properties = GetPropertiesFromType( item );
         }
         return properties;
      }

      private static void TrySetValue<T>( dynamic item, Dictionary<string, Tuple<object, Type>> properties ) where T : Attribute
      {
         string keyName;
         if ( typeof( T ) == typeof( PartitionKeyAttribute ) )
         {
            keyName = "PartitionKey";
         }
         else if ( typeof( T ) == typeof( RowKeyAttribute ) )
         {
            keyName = "RowKey";
         }
         else
         {
            throw new ArgumentException( string.Format( "Type: {0} is not a valid key type.", typeof( T ) ) );
         }

         try
         {
            string keyValue = ( (object) item ).ReadPropertyDecoratedWith<T, string>();
            if (properties.ContainsKey( keyName ))
            {
               throw new InvalidEntityException( string.Format( "Entity of type {0} has {1} defined as an attribute and property", item.GetType(), keyName ) );
            }
            properties[keyName] = new Tuple<object, Type>( keyValue, typeof( string ) );
         }
         catch ( ArgumentException )
         {
         }
      }

      private static Dictionary<string, Tuple<object, Type>> GetPropertiesFromDynamicMetaObject( IDynamicMetaObjectProvider item )
      {
         var properties = new Dictionary<string, Tuple<object, Type>>();
         IEnumerable<string> memberNames = ImpromptuInterface.Impromptu.GetMemberNames( item );
         foreach ( var memberName in memberNames )
         {
            dynamic result = ImpromptuInterface.Impromptu.InvokeGet( item, memberName );
            properties[memberName] = new Tuple<object, Type>( (object) result, result.GetType() );
         }
         return properties;
      }

      private static Dictionary<string, Tuple<object, Type>> GetPropertiesFromType<T>( T item )
      {
         var properties = new Dictionary<string, Tuple<object, Type>>();
         foreach ( var property in item.GetType().GetProperties().Where( p => p.ShouldSerialize() ) )
         {
            properties[property.Name] = new Tuple<object, Type>( property.GetValue( item, null ), property.PropertyType );
         }
         return properties;
      }
   }
}