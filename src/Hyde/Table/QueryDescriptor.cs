using System;

namespace TechSmith.Hyde.Table
{
   public struct KeyBound
   {
      public string Value;
      public bool IsInclusive;
   }

   public struct KeyRange
   {
      public KeyBound? Lower;
      public KeyBound? Upper;

      public bool IsSingleValue()
      {
         return Lower.HasValue && Upper.HasValue &&
                Lower.Value.Value == Upper.Value.Value &&
                Lower.Value.IsInclusive && Upper.Value.IsInclusive;
      }

      public string SingleValue()
      {
         if ( !IsSingleValue() )
            throw new InvalidOperationException( "range does not have identical, inclusive upper and lower bounds");
         return Lower.Value.Value;
      }
   }

   public struct QueryDescriptor
   {
      public KeyRange RowKeyRange;

      public KeyRange PartitionKeyRange;

      public int? TopCount;
   }
}
