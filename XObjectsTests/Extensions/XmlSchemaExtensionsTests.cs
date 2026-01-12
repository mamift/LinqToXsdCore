using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using MoreLinq;
using NUnit.Framework;
using OneOf;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq.Tests.Extensions
{
    [TestFixture]
    public class XmlSchemaExtensionsTests: BaseTester
    {
        private const string PubMedEFetchXsd = @"Pubmed\efetch-pubmed.xsd";

        [Test, TestCase(PubMedEFetchXsd)]
        public void TestGetClosestNamedParent1(string endsWithFilePattern)
        {
            var xsd = GetTestFileAsXmlSchema(endsWithFilePattern);

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

        [Test, TestCase(PubMedEFetchXsd)]
        public void TestGetClosestNamedParent2(string endsWithFilePattern)
        {
            var xsd = GetTestFileAsXmlSchema(endsWithFilePattern);

            var urlElement = xsd.Items.Cast<XmlSchemaObject>()
                .OfType<XmlSchemaElement>()
                .First(e => e.Name == "URL");

            var namedParent = urlElement.GetClosestNamedParent();
            Assert.IsNull(namedParent);
            Assert.IsTrue(urlElement.Parent is XmlSchema);
        }

        [Test, TestCase(PubMedEFetchXsd)]
        public void TestGetClosestNamedParent3(string endsWithFilePattern)
        {
            var xsd = GetTestFileAsXmlSchema(endsWithFilePattern);

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

        [Test, TestCase("StuDateAndTime.xsd")]
        [TestCase("Elements.ElementsTests.xsd")]
        [TestCase("Elements.ElementsWithComplexTypes.xsd")]
        [TestCase("Elements.ElementsWithTypes.xsd")]
        [TestCase("Elements.ElementsWithTypesSimpleDerived.xsd")]
        public void RetrieveChildElementsTest(string endsWithFilePattern)
        {
            var xsd = GetTestFileAsXmlSchemaSet(endsWithFilePattern);

            var allElements = xsd.RetrieveAllElements();

            Assert.NotNull(allElements);
            Assert.IsNotEmpty(allElements);
        }

        [Test, TestCase("StuDateAndTime.xsd")]
        [TestCase("Elements.ElementsTests.xsd")]
        [TestCase("Elements.ElementsWithComplexTypes.xsd")]
        [TestCase("Elements.ElementsWithTypes.xsd")]
        [TestCase("Elements.ElementsWithTypesSimpleDerived.xsd")]
        public void RetrieveAllComplexTypeElementsTest(string endsWithFilePattern)
        {
            var xsd = GetTestFileAsXmlSchemaSet(endsWithFilePattern);

            var allElements = xsd.RetrieveAllComplexTypeElements();

            Assert.NotNull(allElements);
            Assert.IsNotEmpty(allElements);
        }

        [Test, TestCase("StuDateAndTime.xsd"), TestCase("\\AkomaNtoso\\akomantoso30.xsd"), TestCase("Opml\\opml2.xsd")]
        public void TestRetrieveAllAnonymousSimpleTypes(string endsWithFilePattern)
        {
            var xsd = GetTestFileAsXmlSchemaSet(endsWithFilePattern);

            var anonTypes = xsd.RetrieveAllAnonymousSimpleTypes();

            Assert.NotNull(anonTypes);
            Assert.IsNotEmpty(anonTypes);
        }

        [Test, TestCase("\\AkomaNtoso\\akomantoso30.xsd"), TestCase("StuDateAndTime.xsd")]
        public void TestRetrieveAllAnonymousSimpleUnionTypes(string endsWithFilePattern)
        {
            var xsd = GetTestFileAsXmlSchemaSet(endsWithFilePattern);

            var anonUnionTypes = xsd.RetrieveAllAnonymousSimpleUnionTypes();

            Assert.NotNull(anonUnionTypes);
            Assert.IsNotEmpty(anonUnionTypes);
        }

        [Test, TestCase("\\AkomaNtoso\\akomantoso30.xsd")]
        public void TestRetrieveAllSimpleTypes(string endsWithFilePattern)
        {
            var xsd = GetTestFileAsXmlSchemaSet(endsWithFilePattern);

            Dictionary<OneOf<XmlSchemaElement, XmlSchemaAttribute, XmlQualifiedName>, XmlSchemaSimpleType> simpleTypes = xsd.RetrieveAllSimpleTypes();

            Assert.NotNull(simpleTypes);
            Assert.IsNotEmpty(simpleTypes);

            var elementTypes = simpleTypes.Where(e => e.Key.IsT0).ToList();
            var attrTypes = simpleTypes.Where(e => e.Key.IsT1).ToList();

            if (!(attrTypes.Count > elementTypes.Count)) {
                Assert.Warn("That's weird, a schema with more elements whose type is simple than attributes?!");
            }
        }
    }
}
