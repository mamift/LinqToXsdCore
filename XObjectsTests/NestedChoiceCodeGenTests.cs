using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class NestedChoiceCodeGenTests : BaseTester
    {
        public MockFileSystem TestFiles { get; set; }
        private SyntaxTree Tree { get; set; }
        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string xsdFilePath = @"NestedChoiceTest\NestedChoiceTest.xsd";
            TestFiles = Utilities.GetAssemblyFileSystem(typeof(LinqToXsd.Schemas.Test.NestedChoiceTypes.NestedChoiceType).Assembly);
            Tree = Utilities.GenerateSyntaxTree(xsdFilePath, TestFiles, new LinqToXsdSettings() {
                AlwaysPrefixGlobalInUsingDirectives = true
            });

            Diagnostic[] diags = Utilities.GetSyntaxAndCompilationDiagnostics(Tree);
            diags.CheckForZeroDiagnosticsAndErrorsOrThrow(xsdFilePath);

            GeneratedTypes = Tree.GetNamespaceRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        }

        [Test]
        public void T1_NestedChoiceShouldHaveNullableValueReturnType()
        {
            ClassDeclarationSyntax type   = GeneratedTypes.Single(type => type.Identifier.Text == "NestedChoiceType");
            PropertyDeclarationSyntax prop1 = type.Members.OfType<PropertyDeclarationSyntax>().Single(prop => prop.Identifier.Text == "Prop1");
            PropertyDeclarationSyntax prop2 = type.Members.OfType<PropertyDeclarationSyntax>().Single(prop => prop.Identifier.Text == "Prop2");
            PropertyDeclarationSyntax choice1Prop = type.Members.OfType<PropertyDeclarationSyntax>().Single(prop => prop.Identifier.Text == "Choice1Prop");
            PropertyDeclarationSyntax choice2Prop1 = type.Members.OfType<PropertyDeclarationSyntax>().Single(prop => prop.Identifier.Text == "Choice2Prop1");
            PropertyDeclarationSyntax choice2Prop2 = type.Members.OfType<PropertyDeclarationSyntax>().Single(prop => prop.Identifier.Text == "Choice2Prop2");
            Assert.IsTrue(prop1.Type is PredefinedTypeSyntax);
            Assert.IsTrue(prop2.Type is NullableTypeSyntax);
            Assert.IsTrue(choice1Prop.Type is NullableTypeSyntax);
            Assert.IsTrue(choice2Prop1.Type is NullableTypeSyntax);
            Assert.IsTrue(choice2Prop2.Type is NullableTypeSyntax);
        }
    }
}