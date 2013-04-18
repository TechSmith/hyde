namespace TechSmith.Hyde.Test
{
   public class TypeWithEnumProperty
   {
      public TheEnum EnumProperty
      {
         get;
         set;
      }

      public enum TheEnum
      {
         FirstItem = 0,
         SecondItem = 1
      }
   }
}