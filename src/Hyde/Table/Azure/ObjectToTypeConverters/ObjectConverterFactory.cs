using Hyde;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TechSmith.Hyde.Table.Azure.ObjectToTypeConverters
{
   public class ObjectConverterFactory
   {
      private static readonly ConcurrentDictionary<Type, IObjectToTypeConverter> _objectToTypeConverters = GetObjectToTypeConverters();

      public static IObjectToTypeConverter GetConverterFor( Type type )
      {
         if ( type.GetTypeInfo().IsEnum )
         {
            return new EnumConverter();
         }

         if ( !_objectToTypeConverters.ContainsKey( type ) )
         {
            throw new NotSupportedException( string.Format( "The type {0} is not supported", type.Name ) );
         }

         return _objectToTypeConverters[type];
      }

      public static void RegisterConverter( Type type, IObjectToTypeConverter converter )
      {
         if ( _objectToTypeConverters.ContainsKey( type ) )
         {
            throw new ConverterAlreadyRegisteredException( string.Format("Converter already registered for type {0}", type.Name ) );
         }

         _objectToTypeConverters[type] = converter;
      }

      private static ConcurrentDictionary<Type, IObjectToTypeConverter> GetObjectToTypeConverters()
      {
         TypeInfo objectToTypeConverterInterfaceTypeInfo = typeof( IObjectToTypeConverter ).GetTypeInfo();

         Type objectConverterFactoryType = typeof( ObjectConverterFactory );
         Assembly assembly = objectConverterFactoryType.GetTypeInfo().Assembly;
         var basicDictionaryPairs = assembly
                                       .GetTypes()
                                       .Where( t =>
                                       {
                                          TypeInfo typeInfo = t.GetTypeInfo();
                                          return objectToTypeConverterInterfaceTypeInfo.IsAssignableFrom( t ) && !typeInfo.IsInterface && !typeInfo.IsAbstract;
                                       } )
                                       .SelectMany( GetTypesSupportedByTypeConverter );

         return new ConcurrentDictionary<Type, IObjectToTypeConverter>( basicDictionaryPairs );
      }

      private static IEnumerable<KeyValuePair<Type, IObjectToTypeConverter>> GetTypesSupportedByTypeConverter( Type typeConverter )
      {
         var instance = Activator.CreateInstance( typeConverter ) as IObjectToTypeConverter;

         return instance.GetSupportedTypes().Select( type => new KeyValuePair<Type, IObjectToTypeConverter>( type, instance ) );
      }
   }
}