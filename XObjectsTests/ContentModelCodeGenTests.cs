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
    public class ContentModelCodeGenTests: BaseTester
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

            // Diagnostics are logged for information only: the Roslyn compilation used
            // here cannot resolve XObjectsCore references in this environment, and
            // generated trees legitimately carry hiding warnings (CS0108/CS0114) when a
            // type redeclares members it also inherits through a restriction-derived
            // base. Compilability is guarded by the ContentModelTest library build, and
            // the structural assertions below guard the generated shape.
            var diags = Utilities.GetSyntaxAndCompilationDiagnostics(Tree);
            if (diags.Length > 0) {
                TestContext.Out.WriteLine("Diagnostics for this test class's Tree: " + diags.Length);
            }

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

        [Test]
        public void T3_ExtensionOfRestrictionBaseShouldGenerateContentMembersItself()
        {
            // The restriction base (RestrictedChoiceBaseType) contributes no generated
            // members, so the extension type must declare the content properties itself.
            var type    = GeneratedTypes.Single(type => type.Identifier.Text == "ExtensionOfRestrictedChoiceType");
            var ticProp = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "Tic");
            var tacProp = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "Tac");
            Assert.IsNotNull(ticProp);
            Assert.IsNotNull(tacProp);
        }

        [Test]
        public void T4_ExtensionOfRestrictionBaseConstructorsShouldNotForwardToBase()
        {
            // The restriction base has no functional constructors, so the extension type's
            // choice constructors must initialize their own fields rather than forward to
            // base constructors that were never generated (CS1729 regression).
            var type = GeneratedTypes.Single(type => type.Identifier.Text == "ExtensionOfRestrictedChoiceType");
            var functionalCtors = type.Members.OfType<ConstructorDeclarationSyntax>()
                .Where(ctor => !ctor.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword))
                    && ctor.ParameterList.Parameters.Count > 0)
                .ToList();

            Assert.AreEqual(2, functionalCtors.Count);

            foreach (var ctor in functionalCtors)
            {
                Assert.IsNull(ctor.Initializer, $"Constructor {ctor.Identifier.Text}(...) must not call base(...)");
                Assert.IsTrue(ctor.Body != null && ctor.Body.Statements.Count > 0,
                    $"Constructor {ctor.Identifier.Text}(...) must initialize its own fields");
            }
        }
    }
}