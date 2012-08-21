using TechSmith.Hyde.Memory;

namespace TechSmith.Hyde
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