using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.WindowsAzure.Storage.Table;

namespace TechSmith.Hyde.Table.Azure.ObjectToTypeConverters
{
   internal interface IObjectToTypeConverter
   {
      object ConvertToValue( EntityProperty entityProperty, PropertyInfo propertyInfo );
      bool CanConvertType( Type type );
      IEnumerable<Type> GetSupportedTypes();
      EntityProperty ConvertToEntityProperty( object rawItem, Type rawItemType );
   }

   internal abstract class ReferenceTypeConverter<T> : IObjectToTypeConverter
   {
      private readonly Func<EntityProperty, T> _toValue;
      private readonly Func<object, EntityProperty> _toEntityProperty;

      protected ReferenceTypeConverter( Func<EntityProperty, T> toValue, Func<object, EntityProperty> toEntityProperty )
      {
         _toValue = toValue;
         _toEntityProperty = toEntityProperty;
      }

      public virtual object ConvertToValue( EntityProperty entityProperty, PropertyInfo propertyInfo )
      {
         Type typeToConvertTo = propertyInfo.PropertyType;

         EnsureTypeIsConvertable( typeToConvertTo );
         return _toValue( entityProperty );
      }

      protected void EnsureTypeIsConvertable( Type type )
      {
         if ( !CanConvertType( type ) )
         {
            throw new NotSupportedException( string.Format( "The type {0} is not supported by {1}", type.Name, GetType().Name ) );
         }
      }

      public virtual bool CanConvertType( Type type )
      {
         return typeof( T ) == type;
      }

      public virtual IEnumerable<Type> GetSupportedTypes()
      {
         return new List<Type> { typeof( T ) };
      }

      public virtual EntityProperty ConvertToEntityProperty( object rawItem, Type rawItemType )
      {
         EnsureTypeIsConvertable( rawItemType );
         return _toEntityProperty( rawItem );
      }
   }

   internal abstract class ValueTypeConverter<T> : IObjectToTypeConverter where T : struct
   {
      private readonly Func<EntityProperty, T?> _toValue;
      private readonly Func<object, EntityProperty> _toEntityProperty;

      protected ValueTypeConverter( Func<EntityProperty, T?> toValue, Func<object, EntityProperty> toEntityProperty )
      {
         _toValue = toValue;
         _toEntityProperty = toEntityProperty;
      }

      public virtual object ConvertToValue( EntityProperty entityProperty, PropertyInfo propertyInfo )
      {
         Type typeToConvertTo = propertyInfo.PropertyType;

         EnsureTypeIsConvertable( typeToConvertTo );
         if ( typeToConvertTo == typeof( T? ) )
         {
            return _toValue( entityProperty );
         }
         return _toValue( entityProperty ).Value;
      }

      protected void EnsureTypeIsConvertable( Type type )
      {
         if ( !CanConvertType( type ) )
         {
            throw new NotSupportedException( string.Format( "The type {0} is not supported by {1}", type.Name, GetType().Name ) );
         }
      }

      public virtual bool CanConvertType( Type type )
      {
         return typeof( T ) == type || typeof( T? ) == type;
      }

      public virtual IEnumerable<Type> GetSupportedTypes()
      {
         return new List<Type> { typeof( T ), typeof( T? ) };
      }

      public virtual EntityProperty ConvertToEntityProperty( object rawItem, Type rawItemType )
      {
         EnsureTypeIsConvertable( rawItemType );
         return _toEntityProperty( rawItem );
      }
   }

   internal class BoolTypeConverter : ValueTypeConverter<bool>
   {
      public BoolTypeConverter()
         : base( ep => ep.BooleanValue, o => new EntityProperty( (bool?) o ) )
      {
      }
   }

   internal class ByteArrayConverter : ReferenceTypeConverter<byte[]>
   {
      public ByteArrayConverter()
         : base( ep => ep.BinaryValue, o => new EntityProperty( (byte[]) o ) )
      {
      }
   }

   internal class DateTimeConverter : ValueTypeConverter<DateTime>
   {
      public DateTimeConverter()
         : base(
            ep => ep.DateTimeOffsetValue.HasValue ? ep.DateTimeOffsetValue.Value.UtcDateTime : (DateTime?) null,
            o =>
            {
               var date = (DateTime?) o;
               if ( date.HasValue )
               {
                  date = DateTime.SpecifyKind( date.Value, DateTimeKind.Utc );
               }
               return new EntityProperty( date );
            } )
      {
      }
   }

   internal class DoubleConverter : ValueTypeConverter<double>
   {
      public DoubleConverter()
         : base( ep => ep.DoubleValue, o => new EntityProperty( (double?) o ) )
      {
      }
   }

   internal class GuidConverter : ValueTypeConverter<Guid>
   {
      public GuidConverter()
         : base( ep => ep.GuidValue, o => new EntityProperty( (Guid?) o ) )
      {
      }
   }

   internal class IntegerConverter : ValueTypeConverter<int>
   {
      public IntegerConverter()
         : base( ep => ep.Int32Value, o => new EntityProperty( (int?) o ) )
      {
      }
   }

   internal class LongConverter : ValueTypeConverter<long>
   {
      public LongConverter()
         : base( ep => ep.Int64Value, o => new EntityProperty( (long?) o ) )
      {
      }
   }

   internal class StringConverter : ReferenceTypeConverter<string>
   {
      public StringConverter()
         : base( ep => ep.StringValue, o => new EntityProperty( (string) o ) )
      {
      }
   }

   internal class UriConverter : ReferenceTypeConverter<Uri>
   {
      public UriConverter()
         : base(
             ep => string.IsNullOrWhiteSpace( ep.StringValue ) ? null : new Uri( ep.StringValue),
             o => o == null ? new EntityProperty( (string) null ) : new EntityProperty( ( (Uri) o ).AbsoluteUri ) )
      {
      }
   }

   internal class EnumConverter : IntegerConverter
   {
      public override object ConvertToValue( EntityProperty entityProperty, PropertyInfo propertyInfo )
      {
         Type typeToConvertTo = propertyInfo.PropertyType;
         EnsureTypeIsConvertable( typeToConvertTo );

         if ( entityProperty.PropertyType != EdmType.Int32 )
         {
            throw new InvalidOperationException( string.Format( "Cannot convert {0} to an Enum for property {1}", entityProperty.PropertyType, propertyInfo.Name ) );
         }

         int intValue = (int) base.ConvertToValue( entityProperty, propertyInfo );

         if ( Enum.IsDefined( typeToConvertTo, intValue ) )
         {
            return Enum.ToObject( typeToConvertTo, intValue );
         }

         Array enumValues = Enum.GetValues( typeToConvertTo );
         if ( enumValues.Length > 0 )
         {
            return enumValues.GetValue( 0 );
         }

         return 0;
      }

      public override bool CanConvertType( Type type )
      {
         return type.IsEnum;
      }

      public override IEnumerable<Type> GetSupportedTypes()
      {
         return new List<Type> { typeof( Enum ) };
      }

      public override EntityProperty ConvertToEntityProperty( object rawItem, Type rawItemType )
      {
         EnsureTypeIsConvertable( rawItemType );

         return new EntityProperty( (int) rawItem );
      }
   }
}