using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq.Tests
{
    public class CodeGenerationTests
    {
        [Test]
        public void NamespaceCodeGenerationConventionTest()
        {
            const string simpleDocXsdFilepath = @"Schemas\Toy schemas\Simple doc.xsd";
            var simpleDocXsd = XmlReader.Create(simpleDocXsdFilepath).ToXmlSchema();

            var sourceText = Utilities.GenerateSourceText(simpleDocXsdFilepath);

            var tree = CSharpSyntaxTree.ParseText(sourceText);
            var namespaceNode = tree.GetNamespaceRoot();

            Assert.IsNotNull(namespaceNode);

            var xmlQualifiedNames = simpleDocXsd.Namespaces.ToArray();
            var nsName = Regex.Replace(xmlQualifiedNames.Last().Namespace, @"\W", "_");
            var cSharpNsName = Regex.Replace(namespaceNode.Name.ToString(), @"\W", "_");

            Assert.IsTrue(cSharpNsName == nsName);
        }
    }
}