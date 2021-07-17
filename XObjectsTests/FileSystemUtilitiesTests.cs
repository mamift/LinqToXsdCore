using System.Xml.Schema;
using LinqToXsd.Schemas;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class FileSystemUtilitiesTests
    {
        [Test]
        public void PreLoadXmlSchemasTest()
        {
            var xmlSpecXsd = @"XMLSpec\xmlspec.xsd";
            XmlSchemaSet xmlSpecSchemaSet = FileSystemUtilities.PreLoadXmlSchemas(xmlSpecXsd);

            Assert.IsNotNull(xmlSpecSchemaSet);
            Assert.IsTrue(xmlSpecSchemaSet.IsCompiled);
            Assert.IsTrue(xmlSpecSchemaSet.Count == 3);
        }

        [Test]
        public void TestPreloadedW3CXmlSchemaSetTest()
        {
            Assert.IsNotNull(Shared.W3CXmlSchemaSet.Value);
            Assert.IsTrue(Shared.W3CXmlSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.W3CXmlSchemaSet.Value.Count == 2);
        }

        [Test]
        public void TestPreloadedWssXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.WssXsdSchemaSet.Value);
            Assert.IsTrue(Shared.WssXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.WssXsdSchemaSet.Value.Count == 1);
        }

        [Test]
        public void TestPreloadedAspNetSiteMapXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.AspNetSiteMapXsdSchemaSet.Value);
            Assert.IsTrue(Shared.AspNetSiteMapXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.AspNetSiteMapXsdSchemaSet.Value.Count == 1);
        }

        [Test]
        public void TestPreloadedAtomXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.AtomXsdSchemaSet.Value);
            Assert.IsTrue(Shared.AtomXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.AtomXsdSchemaSet.Value.Count == 2);
        }

        [Test]
        public void TestPreloadedMsBuildXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.MsBuildXsdSchemaSet.Value);
            Assert.IsTrue(Shared.MsBuildXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.MsBuildXsdSchemaSet.Value.Count == 1);
        }

        [Test]
        public void TestPreloadedOpmlXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.OpmlXsdSchemaSet.Value);
            Assert.IsTrue(Shared.OpmlXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.OpmlXsdSchemaSet.Value.Count == 1);
        }

        [Test]
        public void TestPreloadedPubmedCollectionsXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.PubmedCollectionsXsdSchemaSet.Value);
            Assert.IsTrue(Shared.PubmedCollectionsXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.PubmedCollectionsXsdSchemaSet.Value.Count == 1);
        }

        [Test]
        public void TestPreloadedPubmedEfetchXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.PubmedEfetchXsdSchemaSet.Value);
            Assert.IsTrue(Shared.PubmedEfetchXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.PubmedEfetchXsdSchemaSet.Value.Count == 1);
        }

        [Test]
        public void TestPreloadedRss2XsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.Rss2XsdSchemaSet.Value);
            Assert.IsTrue(Shared.Rss2XsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.Rss2XsdSchemaSet.Value.Count == 1);
        }

        [Test]
        public void TestPreloadedNameMangledXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.ToySchemas.NameMangledXsdSchemaSet.Value);
            Assert.IsTrue(Shared.ToySchemas.NameMangledXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.ToySchemas.NameMangledXsdSchemaSet.Value.Count == 1);
        }

        [Test]
        public void TestPreloadedAbstractTypeTestXsdSchemaSetTest()
        {
            Assert.IsNotNull(Shared.ToySchemas.AbstractTypeTestXsdSchemaSet.Value);
            Assert.IsTrue(Shared.ToySchemas.AbstractTypeTestXsdSchemaSet.Value.IsCompiled);
            Assert.IsTrue(Shared.ToySchemas.AbstractTypeTestXsdSchemaSet.Value.Count == 1);
        }
    }
}