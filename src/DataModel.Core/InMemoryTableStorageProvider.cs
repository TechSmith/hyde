namespace TechSmith.CloudServices.DataModel.Core
{
   public class InMemoryTableStorageProvider : TableStorageProvider
   {
      public static void ResetAllTables()
      {
         MemoryTableContext.ClearTables();
      }

      protected override ITableContext GetContext()
      {
         return new MemoryTableContext();
      }
   }
}