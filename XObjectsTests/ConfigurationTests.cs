using System.Linq;
using System.Xml;
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
    }
}