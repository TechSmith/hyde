using System;
using TechSmith.Hyde.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TechSmith.Hyde.Test
{
   [TestClass]
   public class ObjectExtensionsTest
   {
      public class FooAttribute : Attribute
      {
      }

      public class BarAttribute : Attribute
      {
      }

      public class BazAttribute : Attribute
      {
      }

      public class NotUsedAttribute : Attribute
      {
      }

      public class TestClass
      {
         [Foo]
         public string Foo
         {
            get;
            set;
         }

         [Foo]
         [Bar]
         public virtual string FooBar
         {
            get;
            set;
         }

         [Baz]
         public virtual string Baz
         {
            get;
            set;
         }
      }

      public class TestSubclass : TestClass
      {
         public override string Baz
         {
            get;
            set;
         }
      }

      [TestMethod]
      public void FindProperty_PropertyWithAttributeNotFound_ReturnsNull()
      {
         Assert.IsNull( new TestClass().FindPropertyDecoratedWith<NotUsedAttribute>() );
      }

      [TestMethod]
      [ExpectedException( typeof( ArgumentException ) )]
      public void FindProperty_MultiplePropertiesWithAttributeFound_ThrowsArgumentException()
      {
         new TestClass().FindPropertyDecoratedWith<FooAttribute>();
      }

      [TestMethod]
      public void FindProperty_OnePropertyWithAttributeFound_ReturnsPropertyInfo()
      {
         Assert.IsNotNull( new TestClass().FindPropertyDecoratedWith<BarAttribute>() );
      }

      [TestMethod]
      public void FindProperty_PropertyWithAttributeIsOnBaseClass_ReturnsPropertyInfo()
      {
         Assert.IsNotNull( new TestSubclass().FindPropertyDecoratedWith<BarAttribute>() );
      }

      [TestMethod]
      public void FindProperty_OverriddenPropertyHasAttribute_ReturnsPropertyInfo()
      {
         Assert.IsNotNull( new TestSubclass().FindPropertyDecoratedWith<BazAttribute>() );
      }

      [TestMethod]
      public void HasProperty_PropertyWithAttributeNotFound_ReturnsFalse()
      {
         Assert.IsFalse( new TestClass().HasPropertyDecoratedWith<NotUsedAttribute>() );
      }

      [TestMethod]
      [ExpectedException( typeof( ArgumentException ) )]
      public void HasProperty_MultiplePropertiesWithAttributeFound_ThrowsArgumentException()
      {
         new TestClass().FindPropertyDecoratedWith<FooAttribute>();
      }

      [TestMethod]
      public void HasProperty_OnePropertyWithAttributeFound_ReturnsTrue()
      {
         Assert.IsTrue( new TestClass().HasPropertyDecoratedWith<BarAttribute>() );
      }

      [TestMethod]
      [ExpectedException( typeof( ArgumentException ) )]
      public void ReadProperty_PropertyWithAttributeNotFound_ThrowsArgumentException()
      {
         new TestClass().ReadPropertyDecoratedWith<FooAttribute,string>();
      }

      [TestMethod]
      [ExpectedException( typeof ( ArgumentException ) )]
      public void ReadProperty_PropertyHasUnexpectedType_ThrowsArgumentException()
      {
         new TestClass().ReadPropertyDecoratedWith<BarAttribute, int>();
      }

      [TestMethod]
      [ExpectedException( typeof ( ArgumentException ) )]
      public void ReadProperty_MultiplePropertiesWithAttributeFound_ThrowsArgumentException()
      {
         new TestClass().ReadPropertyDecoratedWith<FooAttribute, string>();
      }

      [TestMethod]
      public void ReadProperty_OnePropertyWithAttributeFound_ReturnsPropertyValue()
      {
         Assert.AreEqual( "s", new TestClass { FooBar = "s" }.ReadPropertyDecoratedWith<BarAttribute, string>() );
      }

      [TestMethod]
      [ExpectedException( typeof ( ArgumentException ) )]
      public void WriteProperty_PropertyWithAttributeNotFound_ThrowsArgumentException()
      {
         new TestClass().WritePropertyDecoratedWith<NotUsedAttribute, string>( "s" );
      }

      [TestMethod]
      [ExpectedException( typeof ( ArgumentException ) )]
      public void WriteProperty_MultiplePropertiesWithAttributeFound_ThrowsArgumentException()
      {
         new TestClass().WritePropertyDecoratedWith<FooAttribute, string>( "s" );
      }

      [TestMethod]
      public void WriteProperty_OnePropertyWithAttributeFound_SetsPropertyValue()
      {
         var obj = new TestClass();
         obj.WritePropertyDecoratedWith<BarAttribute, string>( "s" );
         Assert.AreEqual( "s", obj.FooBar );
      }
   }
}
