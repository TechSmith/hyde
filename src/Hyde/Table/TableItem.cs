using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Table
{
   public class TableItem
   {
      public enum ReservedPropertyBehavior
      {
         Throw,
         Ignore,
      }

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

      private TableItem( Dictionary<string, Tuple<object, Type>> properties, bool throwOnReservedProperties )
      {
         Properties = new Dictionary<string, Tuple<object, Type>>();
         foreach ( string propertyName in properties.Keys )
         {
            if ( !TableConstants.ReservedPropertyNames.Contains( propertyName ) )
            {
               Properties[propertyName] = properties[propertyName];
            }
            else if ( throwOnReservedProperties )
            {
               throw new InvalidEntityException( string.Format( "Reserved property name {0}", propertyName ) );
            }
            else if( propertyName == TableConstants.PartitionKey )
            {
               var partitionKeyProperty = properties[propertyName];
               if( partitionKeyProperty.Item2 != typeof( string ) )
               {
                  throw new InvalidEntityException( string.Format( "PartitionKey property must be a string but was a {0}", partitionKeyProperty.Item2 ) );
               }
               PartitionKey = (string) partitionKeyProperty.Item1;
            }
            else if( propertyName == TableConstants.RowKey )
            {
               var rowKeyProperty = properties[propertyName];
               if ( rowKeyProperty.Item2 != typeof( string ) )
               {
                  throw new InvalidEntityException( string.Format( "RowKey property must be a string but was a {0}", rowKeyProperty.Item2 ) );
               }
               RowKey = (string) rowKeyProperty.Item1;
            }
         }
      }

      public static TableItem Create( dynamic entity, ReservedPropertyBehavior reservedPropertyBehavior = ReservedPropertyBehavior.Throw )
      {
         bool throwOnReservedPropertyName = reservedPropertyBehavior == ReservedPropertyBehavior.Throw;
         TableItem item =  entity is IDynamicMetaObjectProvider ?
            CreateFromDynamicMetaObject( entity, throwOnReservedPropertyName ) :
            CreateFromType( entity, throwOnReservedPropertyName );

         if ( item.PartitionKey == null )
         {
            throw new ArgumentException( "Missing PartitionKey property" );
         }

         if ( item.RowKey == null )
         {
            throw new ArgumentException( "Missing RowKey property" );
         }

         return item;
      }

      public static TableItem Create( dynamic entity, string partitionKey, string rowKey, ReservedPropertyBehavior reservedPropertyBehavior = ReservedPropertyBehavior.Throw )
      {
         bool throwOnReservedPropertyName = reservedPropertyBehavior == ReservedPropertyBehavior.Throw;
         TableItem item = entity is IDynamicMetaObjectProvider ?
            CreateFromDynamicMetaObject( entity, throwOnReservedPropertyName ) :
            CreateFromType( entity, throwOnReservedPropertyName );

         if ( item.PartitionKey != null && item.PartitionKey != partitionKey )
         {
            throw new ArgumentException( string.Format( "Entity defines PartitionKey: {0} but it conflicts with partitionKey argument: {1}", item.PartitionKey, partitionKey ) );
         }
         item.PartitionKey = partitionKey;

         if ( item.RowKey != null && item.RowKey != rowKey )
         {
            throw new ArgumentException( string.Format( "Entity defines RowKey: {0} but it conflicts with rowKey argument: {1}", item.RowKey, rowKey ) );
         }
         item.RowKey = rowKey;

         return item;
      }

      private static TableItem CreateFromDynamicMetaObject( IDynamicMetaObjectProvider entity, bool throwOnReservedPropertyName )
      {
         var properties = new Dictionary<string, Tuple<object, Type>>();
         IEnumerable<string> memberNames = ImpromptuInterface.Impromptu.GetMemberNames( entity );
         foreach ( var memberName in memberNames )
         {
            dynamic result = ImpromptuInterface.Impromptu.InvokeGet( entity, memberName );
            properties[memberName] = new Tuple<object, Type>( (object) result, result.GetType() );
         }

         return new TableItem( properties, throwOnReservedPropertyName );
      }

      private static TableItem CreateFromType( object entity, bool throwOnReservedPropertyName )
      {
         if ( IsAnonymousType( entity ) )
         {
            return CreateFromAnonymousType( entity, throwOnReservedPropertyName );
         }
         else
         {
            return CreateFromStaticType( entity, throwOnReservedPropertyName );
         }
      }

      private static TableItem CreateFromAnonymousType( object entity, bool throwOnReservedPropertyName )
      {
         var properties = new Dictionary<string, Tuple<object, Type>>();

         var allInstancePropertiesFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
         foreach ( var property in entity.GetType().GetProperties( allInstancePropertiesFlag ) )
         {
            properties[property.Name] = new Tuple<object, Type>( property.GetValue( entity, null ), property.PropertyType );
         }

         return new TableItem( properties, throwOnReservedPropertyName );
      }

      private static TableItem CreateFromStaticType( object entity, bool throwOnReservedPropertyName )
      {
         var properties = new Dictionary<string, Tuple<object, Type>>();
         foreach ( var property in entity.GetType().GetProperties().Where( p => p.ShouldSerialize() ) )
         {
            properties[property.Name] = new Tuple<object, Type>( property.GetValue( entity, null ), property.PropertyType );
         }

         var item = new TableItem( properties, throwOnReservedPropertyName );

         if ( entity.HasPropertyDecoratedWith<PartitionKeyAttribute>() )
         {
            item.PartitionKey = entity.ReadPropertyDecoratedWith<PartitionKeyAttribute, string>();
         }

         if ( entity.HasPropertyDecoratedWith<RowKeyAttribute>() )
         {
            item.RowKey = entity.ReadPropertyDecoratedWith<RowKeyAttribute, string>();
         }
         return item;
      }

      private static bool IsAnonymousType( object entity )
      {
          Type type = entity.GetType();
          return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                  && type.Name.Contains("AnonymousType")
                  && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                  && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
      }
   }
}