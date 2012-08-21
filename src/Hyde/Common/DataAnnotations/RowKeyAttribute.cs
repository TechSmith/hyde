using System;

namespace TechSmith.Hyde.Common.DataAnnotations
{
   /// <summary>
   /// Marks a property as a table storage row key. Property must have string type.
   /// </summary>
   [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
   public class RowKeyAttribute : DontSerializeAttribute
   {
   }
}
