using System.IO;
using System.Linq;
using System.Xml.Schema;
using MoreLinq;
using NUnit.Framework;
using XObjects;

namespace Xml.Schema.Linq.Tests.Extensions
{
    [TestFixture]
    public class XmlSchemaExtensionsTests
    {
        private string xsdFilePathPubmed = @"Pubmed\efetch-pubmed.xsd";

        [Test]
        public void TestGetClosestNamedParent1()
        {
            Assert.IsTrue(File.Exists(xsdFilePathPubmed));

            var xsd = XmlSchema.Read(File.OpenText(xsdFilePathPubmed), (sender, args) => { });

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
            Assert.IsTrue(File.Exists(xsdFilePathPubmed));

            var xsd = XmlSchema.Read(File.OpenText(xsdFilePathPubmed), (sender, args) => { });

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
            Assert.IsTrue(File.Exists(xsdFilePathPubmed));

            var xsd = XmlSchema.Read(File.OpenText(xsdFilePathPubmed), (sender, args) => { });

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
    }
}