using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class ConfigurationTests
    {
        [Test]
        public void TestExampleNamespaceElementsArePresentInExampleConfig()
        {
            var eg = ConfigurationProvider.ProvideExampleConfigurationXml();
            var exampleNamespaceEl = eg.Descendants().Where(n => n.Name.LocalName == "Namespace");

            Assert.IsTrue(exampleNamespaceEl.Any());
            Assert.IsNotNull(eg);
        }

        [Test]
        public void TestHelpfulCommentsArePresent()
        {
            var eg = Configuration.GetExampleConfigurationInstance();

            var helpfulVersion = eg.AddHelpfulComments();

            var comments = helpfulVersion.DescendantNodes().Where(d => d.NodeType == XmlNodeType.Comment);

            Assert.IsTrue(comments.Any());
        }

        [Test]
        public void TestThatConfigHasDefaultNamespaceMappingForXsdWithNoTargetNamespace()
        {
            var rssSchema = XDocument.Load(@"Rss\rss-2_0.xsd");
            var loaded = Configuration.LoadForSchema(rssSchema);

            var namespaceEl = loaded.Namespaces.Namespace.First();

            Assert.IsTrue(namespaceEl.Clr == "Default");
            Assert.IsTrue(namespaceEl.Schema.OriginalString == "");
        }
    }
}