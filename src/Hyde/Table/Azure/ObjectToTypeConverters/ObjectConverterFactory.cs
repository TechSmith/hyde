using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TechSmith.Hyde.Table.Azure.ObjectToTypeConverters
{
   internal class ObjectConverterFactory
   {
      private static readonly ConcurrentDictionary<Type, IObjectToTypeConverter> _objectToTypeConverters = GetObjectToTypeConverters();

      public static IObjectToTypeConverter GetConverterFor( Type type )
      {
         if ( type.IsEnum )
         {
            return new EnumConverter();
         }

         if ( !_objectToTypeConverters.ContainsKey( type ) )
         {
            throw new NotSupportedException( string.Format( "The type {0} is not supported", type.Name ) );
         }

         return _objectToTypeConverters[type];
      }

      private static ConcurrentDictionary<Type, IObjectToTypeConverter> GetObjectToTypeConverters()
      {
         var basicDictionaryPairs = Assembly.GetAssembly( typeof( ObjectConverterFactory ) )
                                       .GetTypes()
                                       .Where( t => typeof( IObjectToTypeConverter ).IsAssignableFrom( t ) && !t.IsInterface && !t.IsAbstract )
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