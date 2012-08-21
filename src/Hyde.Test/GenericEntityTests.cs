using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde;
using TechSmith.Hyde.Azure;

namespace TechSmith.Hyde.Test
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

         var genericItemToTest = GenericEntity.HydrateFrom( itemToSave, "pk", "rk" );


         var wereCool = true;


         wereCool &= itemToSave.FirstType == genericItemToTest.GetProperties()["FirstType"].Value;
         wereCool &= itemToSave.SecondType == genericItemToTest.GetProperties()["SecondType"].Value;

         wereCool &= genericItemToTest.GetProperties().Count == 2;

         Assert.IsTrue( wereCool );
      }

      [TestMethod]
      public void Hydrate_ItemDecoratedWithRowAndPartitionKeyAttributes_ReturnedGenericEntityHasCorrectProperties()
      {
         var itemToSave = new DecoratedItem
                          {
                             Id = "id",
                             Name = "name",
                             Age = 42,
                          };
         var genericEntity = GenericEntity.HydrateFrom( itemToSave );

         Assert.AreEqual( "id", genericEntity.PartitionKey, "incorrect partition key" );
         Assert.AreEqual( "name", genericEntity.RowKey, "incorrect row key" );
         Assert.IsFalse( genericEntity.GetProperties().ContainsKey( "Id" ), "partition key property should not be serialized as separate property" );
         Assert.IsFalse( genericEntity.GetProperties().ContainsKey( "Name" ), "row key property should not be serialized as separate property" );
      }

      [TestMethod]
      public void CreateInstanceFromProperties_TargetTypeDecoratedWithRowAndPartitionKeyAttributes_RowAndPartitionKeySetCorrectly()
      {
         var genericEntity = new GenericEntity();
         genericEntity.PartitionKey = "foo";
         genericEntity.RowKey = "bar";
         genericEntity["Age"] = new EntityPropertyInfo( 42, typeof( int ), false );

         var item = genericEntity.CreateInstanceFromProperties<DecoratedItem>();
         Assert.AreEqual<string>( "foo", item.Id, "Incorrect partition key" );
         Assert.AreEqual<string>( "bar", item.Name, "Incorrect row key" );
      }

      [TestMethod]
      public void SimpleItemWithDontSerializeAttributeConvertsToGenericEntityCorrectly()
      {
         var itemToSave = new SimpleItemWithDontSerializeAttribute
                          {
                             SerializedString = "foo",
                             NotSerializedString = "bar"
                          };

         var genericItemToTest = GenericEntity.HydrateFrom( itemToSave, "pk", "rk" );


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
