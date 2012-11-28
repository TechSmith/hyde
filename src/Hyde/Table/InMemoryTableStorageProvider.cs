using TechSmith.Hyde.Table.Memory;

namespace TechSmith.Hyde.Table
{
   public class InMemoryTableStorageProvider : TableStorageProvider
   {
      public static void ResetAllTables()
      {
         MemoryTableContext.ResetAllTables();
      }

      protected override ITableContext GetContext()
      {
         return new MemoryTableContext();
      }
   }
}