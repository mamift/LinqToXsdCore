using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    public class CodeGenerationTests
    {
        public static SourceText GenerateSourceText(string filePath)
        {
            var possibleSettingsFile = $"{filePath}.config";
            KeyValuePair<string, TextWriter> code = File.Exists(possibleSettingsFile)
                ? XObjectsCoreGenerator.Generate(filePath, possibleSettingsFile)
                : XObjectsCoreGenerator.Generate(filePath, (string) null);

            return SourceText.From(code.Value.ToString());
        }

        [Test]
        public void NamespaceCodeGenerationConventionTest()
        {
            var simpleDocXsdFilepath = @"Schemas\Toy schemas\Simple doc.xsd";
            var simpleDocXsd = XmlReader.Create(simpleDocXsdFilepath).ToXmlSchema();

            var sourceText = GenerateSourceText(simpleDocXsdFilepath);

            var tree = CSharpSyntaxTree.ParseText(sourceText);

            var root = tree.GetRoot();
            var namespaceNode = root.DescendantNodes()
                                    .First(dn => dn is NamespaceDeclarationSyntax) as NamespaceDeclarationSyntax;

            Assert.IsNotNull(namespaceNode);

            var xmlQualifiedNames = simpleDocXsd.Namespaces.ToArray();
            var nsName = Regex.Replace(xmlQualifiedNames.Last().Namespace, @"\W", "_");
            var cSharpNsName = Regex.Replace(namespaceNode.Name.ToString(), @"\W", "_");

            Assert.IsTrue(cSharpNsName == nsName);
        }

        [Test]
        public void WssXsdCodeGenerationTests()
        {
            var wssXsdFilePath = @"Schemas\SharePoint2010\wss.xsd";
            var wssXsd = XmlReader.Create(wssXsdFilePath).ToXmlSchema();

            var sourceText = GenerateSourceText(wssXsdFilePath);

            File.WriteAllText("wss.xsd.cs", sourceText.ToString());

            var tree = CSharpSyntaxTree.ParseText(sourceText);

            var root = tree.GetRoot();
            var namespaceNode = root.DescendantNodes()
                                    .First(dn => dn is NamespaceDeclarationSyntax) as NamespaceDeclarationSyntax;
        }
    }
}