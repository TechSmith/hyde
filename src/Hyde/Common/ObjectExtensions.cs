using System;
using System.Linq;
using System.Reflection;

namespace TechSmith.Hyde.Common
{
   internal static class ObjectExtensions
   {
      /// <summary>
      /// Return the property decorated with attribute T, or null if none exists.
      /// </summary>
      /// <typeparam name="T">the attribute that decorates the desired property</typeparam>
      /// <param name="obj">an object</param>
      /// <returns>the property decorated with T, or null</returns>
      /// <throws>ArgumentException if obj has multiple public properties decorated with T1, or if the property is not of type T2</throws>
      internal static PropertyInfo FindPropertyDecoratedWith<T>( this object obj ) where T : Attribute
      {
         var attrType = typeof( T );
         var objType = obj.GetType();
         var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
         var props = objType.GetProperties( bindingFlags )
                            .Where( p => p.GetCustomAttributes( attrType, inherit: true ).Length > 0 )
                            .ToArray();

         if ( props.Length == 0 )
         {
            return null;
         }
         if ( props.Length > 1 )
         {
            var propNames = string.Join( ", ", props.Select( p => p.Name ) );
            var errorMsg = string.Format(
               "Ambiguous {0}: Type {1} has multiple public properties decorated with {0} ({2})",
               attrType.FullName, objType.FullName, propNames );
            throw new ArgumentException( errorMsg );
         }
         return props[0];
      }

      internal static bool HasPropertyDecoratedWith<T>( this object obj ) where T : Attribute
      {
         return obj.FindPropertyDecoratedWith<T>() != null;
      }

      /// <summary>
      /// Read the string property decorated with attribute T.
      /// </summary>
      /// <typeparam name="T1">the attribute decorating the desired propery</typeparam>
      /// <typeparam name="T2">the expected type of the desired property</typeparam>
      /// <param name="obj">an object</param>
      /// <returns>the value of the decorated property</returns>
      /// <throws>ArgumentException if obj does not have exactly one public property of type T2 decorated with T1</throws>
      internal static T2 ReadPropertyDecoratedWith<T1,T2>( this object obj ) where T1 : Attribute
      {
         var attrType = typeof( T1 );
         var propType = typeof( T2 );
         var objType = obj.GetType();
         var prop = obj.FindPropertyDecoratedWith<T1>();
         if ( prop == null )
         {
            throw new ArgumentException( string.Format( "Type {0} has no public property decorated with {1}", objType.FullName, attrType.FullName ) );
         }
         if ( prop.PropertyType != propType )
         {
            var msg = string.Format( "Property {0}.{1} was not of type {2}", objType.FullName, prop.Name, propType.FullName );
            throw new ArgumentException( msg );
         }
         return (T2)prop.GetValue( obj, null );
      }

      internal static void WritePropertyDecoratedWith<T1,T2>( this object obj, T2 value ) where T1 : Attribute
      {
         var attrType = typeof( T1 );
         var propType = typeof( T2 );
         var objType = obj.GetType();
         var prop = obj.FindPropertyDecoratedWith<T1>();
         if ( prop == null )
         {
            throw new ArgumentException( string.Format( "Type {0} has no public property decorated with {1}", objType.FullName, attrType.FullName ) );
         }
         if ( prop.PropertyType != propType )
         {
            var msg = string.Format( "Property {0}.{1} was not of type {2}", objType.FullName, prop.Name, propType.FullName );
            throw new ArgumentException( msg );
         }
         prop.SetValue( obj, value, null );
      }
   }
}
