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
        public const string XmlXsdFilePath = @"C:\Users\mmiftah\source\Tests\LinqToXsdCoreDiffs\XML\xml.xsd";
        public const string XmlLangTestXsdFilePath = @"C:\Users\mmiftah\source\Tests\LinqToXsdCoreDiffs\XML\xmlTest.xsd";

        [Test]
        public void XmlLangTestCodeGeneration()
        {
            var xmlXsdFile = new FileInfo(XmlXsdFilePath);
            var xmlTestXsdFile = new FileInfo(XmlLangTestXsdFilePath);
            
            var sourceText = Utilities.GenerateSourceText(XmlLangTestXsdFilePath);

            var tree = CSharpSyntaxTree.ParseText(sourceText);
            var namespaceNode = tree.GetNamespaceRoot();

            Assert.IsNotNull(namespaceNode);
        }
    }
}