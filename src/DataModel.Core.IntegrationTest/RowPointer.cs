using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechSmith.CloudServices.DataModel.Core;

namespace TechSmith.CloudServices.DataModel.CoreIntegrationTests
{
   /// <summary>
   /// A record that points at another record, for testing purposes.
   /// </summary>
   class RowPointer
   {
      [PartitionKey]
      [RowKey]
      public string Id
      {
         get;
         set;
      }

      /// <summary>
      /// Partition key of the pointed-to row.
      /// </summary>
      public string PartitionKey
      {
         get;
         set;
      }

      /// <summary>
      /// Row key of the pointed-to row.
      /// </summary>
      public string RowKey
      {
         get;
         set;
      }
   }
}
