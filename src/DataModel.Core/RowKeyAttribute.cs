using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TechSmith.CloudServices.DataModel.Core
{
   /// <summary>
   /// Marks a property as a table storage row key. Property must have string type.
   /// </summary>
   [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
   public class RowKeyAttribute : DontSerializeAttribute
   {
   }
}
