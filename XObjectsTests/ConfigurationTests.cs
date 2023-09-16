using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Rss;

namespace Xml.Schema.Linq.Tests
{
    public class ConfigurationTests
    {
        public MockFileSystem TestFiles { get; set; }

        [SetUp]
        public void Setup()
        {
            TestFiles = Utilities.GetAssemblyFileSystem(typeof(RssChannel).Assembly);
        }

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
            var rssSchema = XDocument.Parse(TestFiles.GetFile(@"Rss\rss-2_0.xsd").TextContents);
            var loaded = Configuration.LoadForSchema(rssSchema);

            var namespaceEl = loaded.Namespaces.Namespace.First();

            Assert.IsTrue(namespaceEl.Clr == "Default");
            Assert.IsTrue(namespaceEl.Schema.OriginalString == "");
        }
    }
}