using System;
using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Test
{
	/// <summary>
	/// Class with decorated partition, row key and timestamp properties, for testing purposes.
	/// </summary>
	internal class DecoratedItemWithTimestamp
	{
		[PartitionKey]
		public string Id
		{
			get;
			set;
		}

		[RowKey]
		public string Name
		{
			get;
			set;
		}

		[Timestamp]
		public DateTimeOffset Timestamp
		{
			get;
			set;
		}

		public int Age
		{
			get;
			set;
		}
	}
}
