using System;

namespace TechSmith.Hyde.Common.DataAnnotations
{

   [AttributeUsage( AttributeTargets.Property )]
   public class DontSerializeAttribute : Attribute
   {
   }
}
