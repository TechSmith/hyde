using System;

namespace TechSmith.CloudServices.DataModel.Core
{

   [AttributeUsage( AttributeTargets.Property )]
   public class DontSerializeAttribute : Attribute
   {
   }
}
