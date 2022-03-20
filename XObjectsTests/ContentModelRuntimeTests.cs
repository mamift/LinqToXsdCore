using System.Collections.Generic;
using System.IO;
using System.Linq;

using LinqToXsd.Schemas.ContentModelTest;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class ContentModelRuntimeTests
    {
        [Test]
        public void T1_SimpleChoice()
        {
            var obj = new SimpleChoiceType() { Tic = "tic", Tac = "tac" };
            Assert.IsNull(obj.Tic);
            Assert.AreEqual("tac", obj.Tac);
            Assert.IsNull(obj.Toc);
            obj.Tic = "tic";
            Assert.AreEqual("tic", obj.Tic);
            Assert.IsNull(obj.Tac);
            Assert.IsNull(obj.Toc);
            obj.Toc = "toc";
            Assert.IsNull(obj.Tic);
            Assert.IsNull(obj.Tac);
            Assert.AreEqual("toc", obj.Toc);
        }
        [Test]
        public void T2_SequenceWithChoice()
        {
            var obj = new SequenceWithChoiceType() { Tic = "tic", Tac = "tac", Foo = "foo", Bar = "bar" };
            Assert.IsNull(obj.Tic);
            Assert.AreEqual("tac", obj.Tac);
            Assert.AreEqual("foo", obj.Foo);
            Assert.AreEqual("bar", obj.Bar);
            obj.Tic = "tic";
            Assert.AreEqual("tic", obj.Tic);
            Assert.IsNull(obj.Tac);
            Assert.AreEqual("foo", obj.Foo);
            Assert.AreEqual("bar", obj.Bar);
        }
        [Test]
        public void T3_ChoiceWithSequence()
        {
            var obj = new ChoiceWithSequemceType() { Foo = "foo", Bar = "bar", Toc = "toc" };
            Assert.IsNull(obj.Foo);
            Assert.IsNull(obj.Bar);
            Assert.AreEqual("toc", obj.Toc);
            obj.Foo = "foo";
            Assert.AreEqual("foo", obj.Foo);
            Assert.IsNull(obj.Bar);
            Assert.IsNull(obj.Toc);
            obj.Bar = "bar";
            Assert.AreEqual("foo", obj.Foo);
            Assert.AreEqual("bar", obj.Bar);
            Assert.IsNull(obj.Toc);
            obj.Toc = "toc";
            Assert.IsNull(obj.Foo);
            Assert.IsNull(obj.Bar);
            Assert.AreEqual("toc", obj.Toc);
        }
        [Test]
        public void T4_DeepChoiceTree()
        {
            var obj = new DeepChoiceTreeType() { Foo = "foo", Bar = "bar", Toc = "toc", Foz = "foz", Baz = "baz", Tic = "tic" };
            Assert.IsNull(obj.Foo);
            Assert.IsNull(obj.Bar);
            Assert.IsNull(obj.Toc);
            Assert.IsNull(obj.Foz);
            Assert.IsNull(obj.Baz);
            Assert.AreEqual("tic", obj.Tic);
            obj.Toc = "toc";
            Assert.IsNull(obj.Foo);
            Assert.IsNull(obj.Bar);
            Assert.AreEqual("toc", obj.Toc);
            Assert.IsNull(obj.Foz);
            Assert.IsNull(obj.Baz);
            Assert.IsNull(obj.Tic);
            obj.Baz = "baz";
            obj.Foz = "foz";
            obj.Bar = "bar";
            obj.Foo = "foo";
            Assert.AreEqual("foo", obj.Foo);
            Assert.AreEqual("bar", obj.Bar);
            Assert.IsNull(obj.Toc);
            Assert.AreEqual("foz", obj.Foz);
            Assert.AreEqual("baz", obj.Baz);
            Assert.IsNull(obj.Tic);
            obj.Tic = "tic";
            Assert.IsNull(obj.Foo);
            Assert.IsNull(obj.Bar);
            Assert.IsNull(obj.Toc);
            Assert.IsNull(obj.Foz);
            Assert.IsNull(obj.Baz);
            Assert.AreEqual("tic", obj.Tic);
        }
        [Test]
        public void T5_DeepSequenceTree()
        {
            var obj = new DeepSequenceTreeType() { Foo = "foo", Bar = "bar", Toc = "toc", Foz = "foz", Baz = "baz", Tic = "tic" };
            Assert.IsNull(obj.Foo);
            Assert.IsNull(obj.Bar);
            Assert.IsNull(obj.Toc);
            Assert.IsNull(obj.Foz);
            Assert.AreEqual("baz", obj.Baz);
            Assert.AreEqual("tic", obj.Tic);

            obj.Foz = "foz";
            Assert.AreEqual("foz", obj.Foz);
            Assert.IsNull(obj.Baz);
            Assert.AreEqual("tic", obj.Tic);

            obj.Toc = "toc";
            Assert.AreEqual("toc", obj.Toc);
            Assert.IsNull(obj.Foz);
            Assert.IsNull(obj.Baz);
            Assert.AreEqual("tic", obj.Tic);

            obj.Foz = "foz";
            obj.Baz = "baz";
            obj.Foo = "foo";
            obj.Bar = "bar";
            Assert.IsNull(obj.Foo);
            Assert.AreEqual("bar", obj.Bar);
            Assert.IsNull(obj.Toc);
            Assert.IsNull(obj.Foz);
            Assert.IsNull(obj.Baz);
            Assert.AreEqual("tic", obj.Tic);
        }
    }
}