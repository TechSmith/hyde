using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table;
using TechSmith.Hyde.Table;
using TechSmith.Hyde.Table.Azure;

namespace TechSmith.Hyde.Test
{
   [TestClass]
   public class GenericTableEntityTests
   {
      [TestMethod]
      public void SimpleItemConvertsToGenericTableEntityCorrectly()
      {
         var itemToSave = new SimpleDataItem
                          {
                             FirstType = "foo",
                             SecondType = 0
                          };

         TableItem tableItem = TableItem.CreateAndThrowOnReservedProperties( itemToSave );
         tableItem.SetKeys( "pk", "rk" );

         var genericItemToTest = GenericTableEntity.HydrateFrom( tableItem );


         var wereCool = true;


         wereCool &= itemToSave.FirstType == genericItemToTest.WriteEntity( null )["FirstType"].StringValue;
         wereCool &= itemToSave.SecondType == genericItemToTest.WriteEntity( null )["SecondType"].Int32Value;
         wereCool &= null                  == genericItemToTest.WriteEntity( null )["UriTypeProperty"].StringValue;;

         wereCool &= genericItemToTest.WriteEntity( null ).Count == 3;

         Assert.IsTrue( wereCool );
      }

      [TestMethod]
      public void Hydrate_ItemDecoratedWithRowAndPartitionKeyAttributes_ReturnedGenericTableEntityHasCorrectProperties()
      {
         var itemToSave = new DecoratedItem
                          {
                             Id = "id",
                             Name = "name",
                             Age = 42,
                          };
         TableItem tableItem = TableItem.CreateAndThrowOnReservedProperties( itemToSave );
         tableItem.SetKeys( itemToSave.Id, itemToSave.Name );
         var genericEntity = GenericTableEntity.HydrateFrom( tableItem );

         Assert.AreEqual( "id", genericEntity.PartitionKey, "incorrect partition key" );
         Assert.AreEqual( "name", genericEntity.RowKey, "incorrect row key" );
         Assert.IsFalse( genericEntity.WriteEntity( null ).ContainsKey( "Id" ), "partition key property should not be serialized as separate property" );
         Assert.IsFalse( genericEntity.WriteEntity( null ).ContainsKey( "Name" ), "row key property should not be serialized as separate property" );
      }

      [TestMethod]
      public void CreateInstanceFromProperties_TargetTypeDecoratedWithRowAndPartitionKeyAttributes_RowAndPartitionKeySetCorrectly()
      {
         var genericEntity = new GenericTableEntity
                             {
                                PartitionKey = "foo", RowKey = "bar"
                             };
         var entityProperties = new Dictionary<string, EntityProperty>();
         entityProperties["Age"] = new EntityProperty( 42 );
         genericEntity.ReadEntity( entityProperties, null );

         var item = genericEntity.ConvertTo<DecoratedItem>();
         Assert.AreEqual( "foo", item.Id, "Incorrect partition key" );
         Assert.AreEqual( "bar", item.Name, "Incorrect row key" );
      }

      [TestMethod]
      public void SimpleItemWithDontSerializeAttributeConvertsToGenericTableEntityCorrectly()
      {
         var itemToSave = new SimpleItemWithDontSerializeAttribute
                          {
                             SerializedString = "foo",
                             NotSerializedString = "bar"
                          };
         TableItem tableItem = TableItem.CreateAndThrowOnReservedProperties( itemToSave );
         tableItem.SetKeys( "pk", "rk" );

         var genericItemToTest = GenericTableEntity.HydrateFrom( tableItem );


         var wereCool = true;


         wereCool &= itemToSave.SerializedString == genericItemToTest.WriteEntity( null )["SerializedString"].StringValue;

         try
         {
            wereCool &= null == genericItemToTest.WriteEntity( null )["NotSerializedString"].StringValue;
         }
         catch ( KeyNotFoundException )
         {
            wereCool &= true;
         }

         wereCool &= genericItemToTest.WriteEntity( null ).Count == 1;

         Assert.IsTrue( wereCool );
      }

      [TestMethod]
      public void DynamicItemConvertsToGenericTableEntityCorrectly()
      {
         dynamic itemToSave = new ExpandoObject();
         itemToSave.FirstType = "foo";
         itemToSave.SecondType = "bar";

         TableItem tableItem = TableItem.CreateAndThrowOnReservedProperties( itemToSave );
         tableItem.SetKeys( "pk", "rk" );

         var genericItemToTest = GenericTableEntity.HydrateFrom( tableItem );

         var wereCool = true;

         wereCool &= itemToSave.FirstType == genericItemToTest.WriteEntity( null )["FirstType"].StringValue;
         wereCool &= itemToSave.SecondType == genericItemToTest.WriteEntity( null )["SecondType"].StringValue;

         wereCool &= genericItemToTest.WriteEntity( null ).Count == 2;

         Assert.IsTrue( wereCool );
      }
   }
}
