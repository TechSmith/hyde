using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TechSmith.CloudServices.DataModel.Core.Tests
{
   [TestClass]
   public class GenericEntityTests
   {
      [TestMethod]
      public void SimpleItemConvertsToGenericEntityCorrectly()
      {
         var itemToSave = new SimpleDataItem
                          {
                             FirstType = "foo",
                             SecondType = "bar"
                          };

         var genericItemToTest = GenericEntity.HydrateGenericEntityFromItem( itemToSave, "pk", "rk" );


         var wereCool = true;


         wereCool &= itemToSave.FirstType == genericItemToTest.GetProperties()["FirstType"].Value;
         wereCool &= itemToSave.SecondType == genericItemToTest.GetProperties()["SecondType"].Value;

         wereCool &= genericItemToTest.GetProperties().Count == 2;

         Assert.IsTrue( wereCool );
      }

      [TestMethod]
      public void SimpleItemWithDontSerializeAttributeConvertsToGenericEntityCorrectly()
      {
         var itemToSave = new SimpleItemWithDontSerializeAttribute
                          {
                             SerializedString = "foo",
                             NotSerializedString = "bar"
                          };

         var genericItemToTest = GenericEntity.HydrateGenericEntityFromItem( itemToSave, "pk", "rk" );


         var wereCool = true;


         wereCool &= itemToSave.SerializedString == (string) genericItemToTest.GetProperties()["SerializedString"].Value;

         try
         {
            wereCool &= null == genericItemToTest.GetProperties()["NotSerializedString"].Value;
         }
         catch ( KeyNotFoundException )
         {
            wereCool &= true;
         }

         wereCool &= genericItemToTest.GetProperties().Count == 1;

         Assert.IsTrue( wereCool );
      }










   }
}
