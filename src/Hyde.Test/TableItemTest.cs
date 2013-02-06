using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechSmith.Hyde.Common;
using TechSmith.Hyde.Common.DataAnnotations;
using TechSmith.Hyde.Table;

namespace TechSmith.Hyde.Test
{
   public class ClassWithPartitionKeyAttributeDecorationAndCorrespondingProperty
   {
      [PartitionKey]
      public string key
      {
         get;
         set;
      }

      public string PartitionKey
      {
         get;
         set;
      }

      public ClassWithPartitionKeyAttributeDecorationAndCorrespondingProperty( string key, string partitionKey )
      {
         this.key = key;
         PartitionKey = partitionKey;
      }
   }

   public class ClassWithRowKeyAttributeDecorationAndCorrespondingProperty
   {
      [RowKey]
      public string row
      {
         get;
         set;
      }

      public string RowKey
      {
         get;
         set;
      }

      public ClassWithRowKeyAttributeDecorationAndCorrespondingProperty( string row, string rowKey )
      {
         this.row = row;
         RowKey = rowKey;
      }
   }
   
   public class ClassWithDecoratedPartitionKeyPropertyOnly
   {
      [PartitionKey]
      public string PartitionKey
      {
         get;
         set;
      }
   }

   public class ClassWithUndecoratedPartitionKeyPropertyOnly
   {
      public string PartitionKey
      {
         get;
         set;
      }
   }

   public class ClassWithDecoratedRowKeyPropertyOnly
   {
      [RowKey]
      public string RowKey
      {
         get;
         set;
      }
   }

   public class ClassWithUndecoratedRowKeyPropertyOnly
   {
      public string RowKey
      {
         get;
         set;
      }
   }

   [TestClass]
   public class TableItemTest
   {
      [TestMethod]
      [ExpectedException( typeof( InvalidEntityException ) )]
      public void CreateAndThrowOnReservedProperties_ItemHasPartitionKeyAttributeDecorationAndProperty_ShouldThrowInvalidEntityException()
      {
         var item = new ClassWithPartitionKeyAttributeDecorationAndCorrespondingProperty( "abc", "123" );
         TableItem.CreateAndThrowOnReservedProperties( item );
      }

      [TestMethod]
      public void CreateAndIgnoreReservedProperties_ItemHasPartitionKeyAttributeDecorationAndProperty_ShouldUseValueOfDecoratedProperty()
      {
         var item = new ClassWithPartitionKeyAttributeDecorationAndCorrespondingProperty( "abc", "123" );
         var tableItem = TableItem.CreateAndIgnoreReservedProperties( item );
         Assert.AreEqual( "abc", tableItem.PartitionKey, "PartitionKey was not equal to the value of the decorated PartitionKey attribute property" );
      }

      [TestMethod]
      [ExpectedException( typeof( InvalidEntityException ) )]
      public void CreateAndThrowOnReservedProperties_ItemHasRowKeyAttributeDecorationAndProperty_ShouldThrowInvalidEntityException()
      {
         var item = new ClassWithRowKeyAttributeDecorationAndCorrespondingProperty( "cba", "321" );
         TableItem.CreateAndThrowOnReservedProperties( item );
      }

      [TestMethod]
      public void CreateAndIgnoreReservedProperties_ItemHasRowKeyAttributeDecorationAndProperty_ShouldUseValueOfDecoratedProperty()
      {
         var item = new ClassWithRowKeyAttributeDecorationAndCorrespondingProperty( "cba", "321" );
         var tableItem = TableItem.CreateAndIgnoreReservedProperties( item );
         Assert.AreEqual( "cba", tableItem.RowKey, "RowKey was not equal to the value of the decorated RowKey attribute property" );
      }

      [TestMethod]
      [ExpectedException( typeof( InvalidEntityException ) )]
      public void CreateAndThrowOnReservedProperties_ItemHasUndecoratedPartitionKeyPropertyOnly_ShouldThrowInvalidEntityException()
      {
         var item = new ClassWithUndecoratedPartitionKeyPropertyOnly()
         {
            PartitionKey = "abc"
         };
         TableItem.CreateAndThrowOnReservedProperties( item );
      }

      [TestMethod]
      public void CreateAndIgnoreReservedProperties_ItemHasUndecoratedPartitionKeyPropertyOnly_ShouldUseValueOfProperty()
      {
         var item = new ClassWithUndecoratedPartitionKeyPropertyOnly()
         {
            PartitionKey = "abc"
         };
         var tableItem = TableItem.CreateAndIgnoreReservedProperties( item );
         Assert.AreEqual( "abc", tableItem.PartitionKey );
      }

      [TestMethod]
      [ExpectedException( typeof( InvalidEntityException ) )]
      public void CreateAndThrowOnReservedProperties_ItemHasUndecoratedRowKeyPropertyOnly_ShouldThrowInvalidEntityException()
      {
         var item = new ClassWithUndecoratedRowKeyPropertyOnly()
         {
            RowKey = "cba"
         };
         TableItem.CreateAndThrowOnReservedProperties( item );
      }

      [TestMethod]
      public void CreateAndIgnoreReservedProperties_ItemHasUndecoratedRowKeyPropertyOnly_ShouldUseValueOfProperty()
      {
         var item = new ClassWithUndecoratedRowKeyPropertyOnly()
         {
            RowKey = "cba"
         };
         var tableItem = TableItem.CreateAndIgnoreReservedProperties( item );
         Assert.AreEqual( "cba", tableItem.RowKey );
      }

      [TestMethod]
      public void CreateAndThrowOnReservedProperties_ItemHasDecoratedPartitionKeyPropertyOnly_ShouldUseValueOfProperty()
      {
         var item = new ClassWithDecoratedPartitionKeyPropertyOnly()
         {
            PartitionKey = "abc"
         };
         var tableItem = TableItem.CreateAndThrowOnReservedProperties( item );
         Assert.AreEqual( "abc", tableItem.PartitionKey );
      }

      [TestMethod]
      public void CreateAndThrowOnReservedProperties_ItemHasDecoratedRowKeyPropertyOnly_ShouldUseValueOfProperty()
      {
         var item = new ClassWithDecoratedRowKeyPropertyOnly()
         {
            RowKey = "cba"
         };
         var tableItem = TableItem.CreateAndThrowOnReservedProperties( item );
         Assert.AreEqual( "cba", tableItem.RowKey );
      }
   }
}
