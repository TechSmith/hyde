namespace TechSmith.CloudServices.DataModel.Core
{
   public class GenericPoisonLimitProvider : IPoisonLimitProvider
   {
      public int Limit
      {
         get
         {
            return 3;
         }
      }
   }
}