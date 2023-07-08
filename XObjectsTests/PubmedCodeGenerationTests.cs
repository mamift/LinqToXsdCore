using System.IO;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class PubmedCodeGenerationTests
    {
        public MockFileSystem PubMedFiles { get; set; }

        [SetUp]
        public void Setup()
        {
            PubMedFiles = Utilities.GetAssemblyFileSystem(typeof(PubMed.Eutils.AbstractText).Assembly);
        }

        [Test]
        public void TestEfetchPubmedSchemaCodeGenerates()
        {
            const string efetchPubmedXsd = @"Pubmed\efetch-pubmed.xsd";
            var efetchPubmedXsdFile = new MockFileInfo(PubMedFiles, efetchPubmedXsd);

            Assert.DoesNotThrow(() => {
                var syntaxTree = Utilities.GenerateSyntaxTree(efetchPubmedXsdFile, PubMedFiles);

                Assert.NotNull(syntaxTree);
            });
        }
    }
}