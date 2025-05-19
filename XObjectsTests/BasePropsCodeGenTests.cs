using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class BasePropsCodeGenTests: BaseTester
    {
        public MockFileSystem TestFiles { get; set; }
        private SyntaxTree Tree { get; set; }
        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string XsdFilePath = @"BasePropsTest\BasePropsTest.xsd";
            TestFiles = Utilities.GetAssemblyFileSystem(typeof(LinqToXsd.Schemas.Test.BasePropsTypes.Wrapper).Assembly);
            Tree = Utilities.GenerateSyntaxTree(XsdFilePath, TestFiles);

            var diags = Utilities.GetSyntaxAndCompilationDiagnostics(Tree);
            //Assert.AreEqual(0, diags.Length);
            if (diags.Length > 0) {
                Assert.Warn("Diagnostics for this test class's Tree should be 0");
            }

            var nodes = Tree.GetNamespaceRoot().DescendantNodes();
            GeneratedTypes = nodes.OfType<ClassDeclarationSyntax>().ToList();
        }

        [Test]
        public void T1_WrapperShouldDeclarePropertyOfBaseOfContentType()
        {
            var type   = GeneratedTypes.Single(type => type.Identifier.Text == "Wrapper");
            var prop = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "BaseContext");
            Assert.IsNotNull(prop);
        }
    }
}