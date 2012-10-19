using TechSmith.Hyde.Common.DataAnnotations;

namespace TechSmith.Hyde.Test
{
   public class SimpleDataItem
   {
      public object FirstType
      {
         get;
         set;
      }
      public object SecondType
      {
         get;
         set;
      }
   }

   public class SimpleItemWithDontSerializeAttribute
   {

      public string SerializedString
      {
         get;
         set;
      }

      [DontSerialize]
      public string NotSerializedString
      {
         get;
         set;
      }
   }

   public class TypeWithPropertyWithInternalGetter
   {
      public string FirstType { get; set; }
      public int PropertyWithInternalGetter { internal get; set; }
   }

   [DontSerialize]
   public class SimpleTypeWithDontSerializeAttribute
   {
      public string StringWithoutDontSerializeAttribute
      {
         get;
         set;
      }
   }

   public class SimpleClassContainingTypeWithDontSerializeAttribute
   {
      public SimpleTypeWithDontSerializeAttribute ThingWithDontSerializeAttribute
      {
         get;
         set;
      }

      public string StringWithoutDontSerializeAttribute
      {
         get;
         set;
      }
   }
}
