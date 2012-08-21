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

}