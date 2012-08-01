using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TechSmith.CloudServices.DataModel.Core
{
   /// <summary>
   /// Marks a property as the partition key. The property must have string type.
   /// </summary>
   [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
   public class PartitionKeyAttribute : DontSerializeAttribute
   {
   }
}
