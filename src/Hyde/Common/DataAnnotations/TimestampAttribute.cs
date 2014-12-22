using System;

namespace TechSmith.Hyde.Common.DataAnnotations
{
	/// <summary>
	/// Marks a property as the time stamp. The property must have DateTime type.
	/// </summary>
	[AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
	public class TimestampAttribute : DontSerializeAttribute
	{
	}
}
