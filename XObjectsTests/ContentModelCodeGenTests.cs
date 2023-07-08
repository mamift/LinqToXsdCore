using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class ContentModelCodeGenTests
    {
        public MockFileSystem TestFiles { get; set; }
        private SyntaxTree Tree { get; set; }
        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string XsdFilePath = @"ContentModelTest\ContentModelTest.xsd";
            TestFiles = Utilities.GetAssemblyFileSystem(typeof(LinqToXsd.Schemas.Test.ContentModelTypes.BaseType).Assembly);
            Tree = Utilities.GenerateSyntaxTree(XsdFilePath, TestFiles);

            var diags = Utilities.GetSyntaxAndCompilationDiagnostics(Tree);
            Assert.AreEqual(0, diags.Length);

            var nodes = Tree.GetNamespaceRoot().DescendantNodes();
            GeneratedTypes = nodes.OfType<ClassDeclarationSyntax>().ToList();
        }

        [Test]
        public void T1_ContentModelShouldInheritBaseContentModelForTypeInheritedByRestriction()
        {
            var type   = GeneratedTypes.Single(type => type.Identifier.Text == "RestrictionType");
            var method = type.Members.OfType<MethodDeclarationSyntax>().SingleOrDefault(meth => meth.Identifier.Text == "GetContentModel");
            Assert.IsNull(method);

            type   = GeneratedTypes.Single(type => type.Identifier.Text == "EmptyExtensionType");
            method = type.Members.OfType<MethodDeclarationSyntax>().SingleOrDefault(meth => meth.Identifier.Text == "GetContentModel");
            Assert.IsNotNull(method);
        }
        [Test]
        public void T2_ContentModelShouldBeGeneratedForComplexGrouping()
        {
            var type   = GeneratedTypes.Single(type => type.Identifier.Text == "SequenceWithChoiceType");
            var field  = type.Members.OfType<FieldDeclarationSyntax>().SingleOrDefault(field => field.Declaration.Variables.SingleOrDefault()?.Identifier.Text == "contentModel");
            var method = type.Members.OfType<MethodDeclarationSyntax>().SingleOrDefault(meth => meth.Identifier.Text == "GetContentModel");
            var ctor   = type.Members.OfType<ConstructorDeclarationSyntax>().SingleOrDefault(ctor => ctor.Modifiers.SingleOrDefault().IsKind(SyntaxKind.StaticKeyword));
            Assert.IsNotNull(field);
            Assert.IsNotNull(method);
            Assert.IsNotNull(ctor);

            var assignment = ctor.Body.Statements.OfType<ExpressionStatementSyntax>().Select(s => s.Expression).OfType<AssignmentExpressionSyntax>().SingleOrDefault();
            Assert.IsNotNull(assignment);
            Assert.AreEqual("contentModel", (assignment.Left as IdentifierNameSyntax)?.Identifier.Text);
            Assert.AreEqual("SequenceContentModelEntity", ((assignment.Right as ObjectCreationExpressionSyntax)?.Type as IdentifierNameSyntax)?.Identifier.Text);
        }
    }
}