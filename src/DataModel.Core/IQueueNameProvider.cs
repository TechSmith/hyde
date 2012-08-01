using System.Collections.Generic;

namespace TechSmith.CloudServices.DataModel.Core
{
   public interface IQueueNameProvider
   {
      IEnumerable<string> Names { get; }
   }
}