using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class ContentModelTests
    {
        private SyntaxTree Tree { get; set; }

        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string xsdFilePath = @"ContentModelTest\ContentModelTest.xsd";
            var xsdFileInfo = new FileInfo(xsdFilePath);
            Tree = Utilities.GenerateSyntaxTree(xsdFileInfo);

            GeneratedTypes = Tree
                             .GetNamespaceRoot()
                             .DescendantNodes()
                             .OfType<ClassDeclarationSyntax>().ToList();
        }

        [Test]
        public void GetContentModelShouldNotBeGeneratedForTypeInheritedByRestriction()
        {
            var type   = GeneratedTypes.Single(type => type.Identifier.Text == "RestrictionType");
            var method = type.Members.OfType<MethodDeclarationSyntax>().SingleOrDefault(meth => meth.Identifier.Text == "GetContentModel");
            Assert.IsNull(method);

            type   = GeneratedTypes.Single(type => type.Identifier.Text == "EmptyExtensionType");
            method = type.Members.OfType<MethodDeclarationSyntax>().SingleOrDefault(meth => meth.Identifier.Text == "GetContentModel");
            Assert.IsNotNull(method);
        }
    }
}