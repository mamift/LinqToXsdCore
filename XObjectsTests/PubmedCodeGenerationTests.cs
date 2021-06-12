using System.IO;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class PubmedCodeGenerationTests
    {
        [Test]
        public void TestEfetchPubmedSchemaCodeGenerates()
        {
            const string efetchPubmedXsd = @"Pubmed\efetch-pubmed.xsd";
            var efetchPubmedXsdFile = new FileInfo(efetchPubmedXsd);

            Assert.DoesNotThrow(() => {
                var syntaxTree = Utilities.GenerateSyntaxTree(efetchPubmedXsdFile);
            });
        }
    }
}