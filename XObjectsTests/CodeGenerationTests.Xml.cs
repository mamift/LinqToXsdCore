using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using LinqToXsd.Schemas.Test.EnumsTypes;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    public partial class CodeGenerationTests
    {
        public const string XmlXsdFilePath = @"SchemaLibs\Xml\xml.xsd";
        public const string XmlLangTestXsdFilePath = @"SchemaLibs\Xml\xml_langTest.xsd";

        [Test]
        public void XmlLangTestCodeGeneration()
        {
            var documentationType = typeof(global::XmlLangTest.documentation);
            var assembly = documentationType.Assembly;

            var xsdResourceNames = assembly.GetManifestResourceNames().Where(f => Path.GetExtension(f).EndsWith("xsd"));
            var configResourceNames = assembly.GetManifestResourceNames().Where(f => Path.GetExtension(f).EndsWith("config"));

            foreach (var resourceName in xsdResourceNames) {
                var sr = assembly.GetManifestResourceStream(resourceName);

                var simpleDocXsd = XmlReader.Create(sr).ToXmlSchemaSet();
                var sourceText = Utilities.GenerateSourceText(simpleDocXsd, new LinqToXsdSettings(nameMangler2: false));

                var tree = CSharpSyntaxTree.ParseText(sourceText);
                var namespaceNode = tree.GetNamespaceRoot();

                Assert.IsNotNull(namespaceNode);
            }
        }
    }
}