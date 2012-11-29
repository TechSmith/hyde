namespace TechSmith.Hyde.Table
{
   /// <summary>
   /// Options for executing table storage operations.
   /// </summary>
   public enum Execute
   {
      /// <summary>
      /// execute each operation individually
      /// </summary>
      Individually,

      /// <summary>
      /// execute sequential operations in batches when possible, for efficiency
      /// </summary>
      InBatches,

      /// <summary>
      /// execute all operations in a single atomic transaction, and fail if this isn't possible
      /// </summary>
      Atomically,
   }
}
