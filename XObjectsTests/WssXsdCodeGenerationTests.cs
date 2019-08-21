using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq.Tests
{
    public class WssXsdCodeGenerationTests
    {
        private SyntaxTree Tree { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string wssXsdFilePath = @"Schemas\SharePoint2010\wss.xsd";
            var code = Utilities.GenerateSourceText(wssXsdFilePath);
            Tree = CSharpSyntaxTree.ParseText(code);
        }

        [Test]
        public void TypesGeneratedTest()
        {
            var namespaceNode = Tree.GetNamespaceRoot();

            Assert.IsNotNull(namespaceNode);

            var allTypesGenerated = namespaceNode.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            Assert.IsNotEmpty(allTypesGenerated);
            Assert.IsTrue(allTypesGenerated.Count == 399);

            var sealedTypes = allTypesGenerated.Where(t => t.Modifiers.Any(c => c.IsKind(SyntaxKind.SealedKeyword)))
                                               .ToList();
            Assert.IsNotEmpty(sealedTypes);
            Assert.IsTrue(sealedTypes.Count == 89);

            var partialTypes = allTypesGenerated.Where(t => t.Modifiers.Any(c => c.IsKind(SyntaxKind.PartialKeyword)) && 
                                                            t.Modifiers.All(c => !c.IsKind(SyntaxKind.SealedKeyword)))
                                                .ToList();
            Assert.IsNotEmpty(partialTypes);
            Assert.IsTrue(partialTypes.Count == 309);

            var others = allTypesGenerated.Except(sealedTypes).Except(partialTypes).ToList();
            Assert.IsNotEmpty(others);
            Assert.IsTrue(others.Count == 1);
            Assert.IsTrue((others.First().Identifier.Value as string) == Constants.LinqToXsdTypeManager);
        }
    }
}