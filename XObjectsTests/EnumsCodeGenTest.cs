using System.Collections.Generic;
using System.IO;
using System.Linq;

using LinqToXsd.Schemas.Test.EnumsTypes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class EnumsCodeGenTest
    {
        const string EnumTypesNamespace = "LinqToXsd.Schemas.Test.EnumsTypes";

        private SyntaxTree                   Tree           { get; set; }
        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }
        private List<EnumDeclarationSyntax>  GeneratedEnums { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string XsdFilePath = @"EnumsTest\EnumsTest.xsd";

            this.Tree = Utilities.GenerateSyntaxTree(XsdFilePath);

            var diags = Utilities.GetSyntaxAndCompilationDiagnostics(this.Tree);
            Assert.AreEqual(0, diags.Length);

            var nodes = this.Tree.GetNamespaceRoot().DescendantNodes();
            this.GeneratedTypes = nodes.OfType<ClassDeclarationSyntax>().ToList();
            this.GeneratedEnums = nodes.OfType<EnumDeclarationSyntax>().ToList();
        }

        [Test]
        public void T1_UseGlobalEnumForGlobalEnumElement()
        {
            var type   = GeneratedTypes.Single(type => type.Identifier.Text == nameof(GlobalEnumElementType));
            var prop   = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "Language");
            var getter = prop.AccessorList.Accessors.Single(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var ret    = getter.DescendantNodes().OfType<ReturnStatementSyntax>().Single();
            Assert.IsTrue(ret.ToString().Contains($"return (({EnumTypesNamespace}.{nameof(LanguageCodeEnum)}"));
        }
        [Test]
        public void T2_UseGlobalEnumForGlobalEnumAttribute()
        {
            var type   = GeneratedTypes.Single(type => type.Identifier.Text == nameof(GlobalEnumAttributeType));
            var prop   = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "language");
            var getter = prop.AccessorList.Accessors.Single(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var ret    = getter.DescendantNodes().OfType<ReturnStatementSyntax>().Last();
            Assert.IsTrue(ret.ToString().StartsWith($"return (({EnumTypesNamespace}.{nameof(LanguageCodeEnum)}"));
        }
        [Test]
        public void T3_UseNestedEnumForNestedEnumElement()
        {
            var type   = GeneratedTypes.Single(type => type.Identifier.Text == nameof(NestedEnumElementType));
            Assert.AreEqual(1, type.Members.OfType<EnumDeclarationSyntax>().Count());
            var prop   = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "Language");
            var getter = prop.AccessorList.Accessors.Single(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var ret    = getter.DescendantNodes().OfType<ReturnStatementSyntax>().Last();
            Assert.IsTrue(ret.ToString().StartsWith($"return (({EnumTypesNamespace}.{nameof(NestedEnumElementType)}.{nameof(NestedEnumElementType.LanguageEnum)}"));
        }
        [Test]
        public void T4_UseNestedEnumForNestedEnumAttribute()
        {
            var type   = GeneratedTypes.Single(type => type.Identifier.Text == nameof(NestedEnumAttributeType));
            Assert.AreEqual(1, type.Members.OfType<EnumDeclarationSyntax>().Count());
            var prop   = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "language");
            var getter = prop.AccessorList.Accessors.Single(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var ret    = getter.DescendantNodes().OfType<ReturnStatementSyntax>().Last();
            Assert.IsTrue(ret.ToString().StartsWith($"return (({EnumTypesNamespace}.{nameof(NestedEnumAttributeType)}.{nameof(NestedEnumAttributeType.LanguageEnum)}"));
        }
        [Test]
        public void T5_DoNotRedefineDerivedNestedEnumAttribute()
        {
            var type = GeneratedTypes.Single(type => type.Identifier.Text == nameof(NestedDerivedEnumAttributeType));
            Assert.AreEqual(1, type.Members.OfType<EnumDeclarationSyntax>().Count());
        }
    }
}