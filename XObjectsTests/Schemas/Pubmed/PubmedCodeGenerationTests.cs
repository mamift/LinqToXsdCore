using System.IO;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests.Schemas.Pubmed
{
    public class PubmedCodeGenerationTests
    {
        [Test]
        public void TestEfetchPubmedSchemaCodeGenerates()
        {
            const string efetchPubmedXsd = @"Schemas\Pubmed\efetch-pubmed.xsd";
            var efetchPubmedXsdFile = new FileInfo(efetchPubmedXsd);

            Assert.DoesNotThrow(() => {
                var syntaxTree = Utilities.GenerateSyntaxTree(efetchPubmedXsdFile);
            });
        }
    }
}