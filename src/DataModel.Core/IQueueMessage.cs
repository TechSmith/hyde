namespace TechSmith.CloudServices.DataModel.Core
{
   public interface IQueueMessage
   {
      string Message { get; }
      bool IsPoison( IPoisonLimitProvider poisonLimitProvider );
   }
}