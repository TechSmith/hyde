using System;

namespace TechSmith.CloudServices.DataModel.Core
{
   internal class EntityPropertyInfo
   {
      public EntityPropertyInfo( object value, Type objectType, bool isNull )
      {
         Value = value;
         ObjectType = objectType;
         IsNull = isNull;
      }

      public object Value
      {
         get;
         set;
      }

      public Type ObjectType
      {
         get;
         set;
      }

      public bool IsNull
      {
         get;
         set;
      }
   }
}
