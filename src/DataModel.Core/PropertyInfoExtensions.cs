using System.Linq;
using System.Reflection;

namespace TechSmith.CloudServices.DataModel.Core
{
   internal static class PropertyInfoExtensions
   {
      public static bool ShouldSerialize( this PropertyInfo property )
      {
         var shouldSerialize = !property.GetCustomAttributes( typeof( DontSerializeAttribute ), inherit: true ).Any();

         shouldSerialize &= ( property.GetSetMethod() != null );
         shouldSerialize &= ( property.GetGetMethod() != null );

         return shouldSerialize;
      }
   }
}
