using System.Linq;
using System.Reflection;
using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Common
{
   internal static class PropertyInfoExtensions
   {
      public static bool ShouldSerialize( this PropertyInfo property )
      {
         var shouldSerialize = !property.GetCustomAttributes( typeof( DontSerializeAttribute ), inherit: true ).Any();

         shouldSerialize &= (property.CanWrite);
         shouldSerialize &= (property.CanRead);
         shouldSerialize &= !property.PropertyType.GetCustomAttributes( typeof( DontSerializeAttribute ), inherit: true ).Any();

         return shouldSerialize;
      }
   }
}
