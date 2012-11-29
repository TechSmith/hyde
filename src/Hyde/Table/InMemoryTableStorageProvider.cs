using TechSmith.Hyde.Table.Memory;

namespace TechSmith.Hyde.Table
{
   public class InMemoryTableStorageProvider : TableStorageProvider
   {
      public InMemoryTableStorageProvider()
         : base( new MemoryTableContext() )
      {
      }

      public static void ResetAllTables()
      {
         MemoryTableContext.ResetAllTables();
      }
   }
}