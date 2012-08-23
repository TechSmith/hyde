using TechSmith.Hyde.Table.Memory;

namespace TechSmith.Hyde.Table
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