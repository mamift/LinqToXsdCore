using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Rss;

namespace Xml.Schema.Linq.Tests
{
    public class ConfigurationTests: BaseTester
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
        public void TestSplitByElementIsCommentedOut()
        {
            var w3cXmlFiles = AllTestFiles.AllFiles.Where(f => f.StartsWith("W3C")).ToList();
            var rssSchema = XDocument.Parse(AllTestFiles.GetFile(@"W3CXML\xmlUse1.xsd").TextContents);
            var loaded = ConfigurationProvider.LoadForSchema(rssSchema);

            var codeGenEl = loaded.Descendants(XName.Get(nameof(CodeGeneration), loaded.Root.Name.NamespaceName)).ToList();

            Assert.IsNotEmpty(codeGenEl);

            var comment = codeGenEl.DescendantNodes().First(c => c is XComment);

            Assert.IsNotNull(comment);

            var commentString = comment.ToString();

            Assert.True(commentString.Contains("<SplitCodeFiles"));
        }

        [Test]
        public void TestThatConfigHasDefaultNamespaceMappingForXsdWithNoTargetNamespace()
        {
            var rssSchema = XDocument.Parse(AllTestFiles.GetFile(@"Rss\rss-2_0.xsd").TextContents);
            var loaded = Configuration.LoadForSchema(rssSchema);

            var namespaceEl = loaded.Namespaces.Namespace.First();

            Assert.IsTrue(namespaceEl.Clr == "Default");
            Assert.IsTrue(namespaceEl.Schema.OriginalString == "");
        }
    }
}