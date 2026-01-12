using System.IO;
using System.Linq;
using System.Xml.Schema;
using MoreLinq;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq.Tests.Extensions
{
    [TestFixture]
    public class XmlSchemaExtensionsTests: BaseTester
    {
        private string xsdFilePathPubmed = @"Pubmed\efetch-pubmed.xsd";

        [Test]
        public void TestGetClosestNamedParent1()
        {
            using var streamReader = GetFileStreamReader(xsdFilePathPubmed);
            var xsd = XmlSchema.Read(streamReader, (sender, args) => { });

            var urlElement = xsd.Items.Cast<XmlSchemaObject>()
                .OfType<XmlSchemaElement>()
                .First(e => e.Name == "URL");

            XmlSchemaAttribute langAttr = null;
            if (urlElement.SchemaType is XmlSchemaComplexType ctx) {
                if (ctx.ContentModel is XmlSchemaSimpleContent xssc) {
                    if (xssc.Content is XmlSchemaSimpleContentExtension xssce) {
                        var attrs = xssce.Attributes.Cast<XmlSchemaAttribute>().ToList();
                        if (attrs.Any()) {
                            langAttr = attrs.First(a => a.Name == "lang");
                        }
                    }
                }
            }
            Assert.IsNotNull(langAttr);

            var namedParent = langAttr.GetClosestNamedParent();

            Assert.IsNotNull(namedParent);
            var parentElement = namedParent as XmlSchemaElement;
            Assert.IsTrue(parentElement != null);
            Assert.IsTrue(parentElement.Name == "URL");
        }

        [Test]
        public void TestGetClosestNamedParent2()
        {
            using var streamReader = GetFileStreamReader(xsdFilePathPubmed);
            var xsd = XmlSchema.Read(streamReader, (sender, args) => { });

            var urlElement = xsd.Items.Cast<XmlSchemaObject>()
                .OfType<XmlSchemaElement>()
                .First(e => e.Name == "URL");

            
            var namedParent = urlElement.GetClosestNamedParent();
            Assert.IsNull(namedParent);
            Assert.IsTrue(urlElement.Parent is XmlSchema);
        }

        [Test]
        public void TestGetClosestNamedParent3()
        {
            using var streamReader = GetFileStreamReader(xsdFilePathPubmed);
            var xsd = XmlSchema.Read(streamReader, (sender, args) => { });

            var articleType = xsd.Items.Cast<XmlSchemaObject>()
                .OfType<XmlSchemaComplexType>()
                .First(e => e.Name == "ArticleType");

            var pubModelAttr = articleType.Attributes.Cast<XmlSchemaAttribute>().First();
            var restrictions = (XmlSchemaSimpleTypeRestriction) pubModelAttr.SchemaType.Content;
            var facets = restrictions.Facets.Cast<XmlSchemaEnumerationFacet>().ToList();

            var aRandomFacet = facets.RandomSubset(1).First();

            var namedParentOfRandomFacet = aRandomFacet.GetClosestNamedParent();

            Assert.IsNotNull(namedParentOfRandomFacet);
            Assert.IsTrue(namedParentOfRandomFacet is XmlSchemaAttribute);
            Assert.IsTrue(((XmlSchemaAttribute) namedParentOfRandomFacet).Name == "PubModel");
        }

        [Test]
        public void TestRetrieveAllAnonymousSimpleTypes_OPML()
        {
            var opmlFile = AllTestFiles.AllFiles.SingleOrDefault(f => f.EndsWith("Opml\\opml2.xsd"));
            Assert.NotNull(opmlFile);
            var opmlXsd = AllTestFiles.FileInfo.New(opmlFile).ReadAsXmlSchemaInstance(MockXmlFileResolver);

            var anonTypes = opmlXsd.RetrieveAllAnonymousSimpleTypes();
            
            Assert.NotNull(anonTypes);
            Assert.IsEmpty(anonTypes);
        }

        [Test]
        public void TestRetrieveAllAnonymousSimpleTypes_StuDateAndTime()
        {
            var file = AllTestFiles.AllFiles.SingleOrDefault(f => f.EndsWith("StuDateAndTime.xsd"));
            Assert.NotNull(file);
            var xsd = AllTestFiles.FileInfo.New(file).ReadAsXmlSchemaInstance(MockXmlFileResolver);

            var anonTypes = xsd.RetrieveAllAnonymousSimpleTypes();
            
            Assert.NotNull(anonTypes);
            Assert.IsEmpty(anonTypes);
        }

        [Test]
        public void TestRetrieveAllAnonymousSimpleUnionTypes_AkomaNtoso()
        {
            var opmlFile = AllTestFiles.AllFiles.SingleOrDefault(f => f.EndsWith("\\AkomaNtoso\\akomantoso30.xsd"));
            Assert.NotNull(opmlFile);
            var opmlXsd = AllTestFiles.FileInfo.New(opmlFile).ReadAsXmlSchemaInstance(MockXmlFileResolver);

            var anonUnionTypes = opmlXsd.RetrieveAllAnonymousSimpleUnionTypes();
            
            Assert.NotNull(anonUnionTypes);
            Assert.IsNotEmpty(anonUnionTypes);
        }

        [Test]
        public void TestRetrieveAllSimpleTypes_AkomaNtoso()
        {
            var opmlFile = AllTestFiles.AllFiles.SingleOrDefault(f => f.EndsWith("\\AkomaNtoso\\akomantoso30.xsd"));
            Assert.NotNull(opmlFile);
            var opmlXsd = AllTestFiles.FileInfo.New(opmlFile).ReadAsXmlSchemaInstance(MockXmlFileResolver);

            var anonUnionTypes = opmlXsd.RetrieveAllSimpleTypes();
            
            Assert.NotNull(anonUnionTypes);
            Assert.IsNotEmpty(anonUnionTypes);
        }
    }
}