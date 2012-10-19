using System;

namespace TechSmith.Hyde.Common.DataAnnotations
{

   [AttributeUsage( AttributeTargets.Property | AttributeTargets.Class )]
   public class DontSerializeAttribute : Attribute
   {
   }
}
