using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Xml.Linq;

using LinqToXsd.Schemas.Test.EnumsTypes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class EnumsCodeGenTest: BaseTester
    {
        public MockFileSystem TestFiles { get; set; }
        const string EnumTypesNamespace = "LinqToXsd.Schemas.Test.EnumsTypes";

        private SyntaxTree Tree { get; set; }
        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }
        private List<EnumDeclarationSyntax> GeneratedEnums { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string xsdFilePath = @"EnumsTest\EnumsTest.xsd";
            TestFiles = Utilities.GetAssemblyFileSystem(typeof(global::LinqToXsd.Schemas.Test.EnumsTypes.LanguageCodeEnum).Assembly);

            this.Tree = Utilities.GenerateSyntaxTree(xsdFilePath, TestFiles);

            var diags = Utilities.GetSyntaxAndCompilationDiagnostics(Tree);
            //Assert.AreEqual(0, diags.Length);
            if (diags.Length > 0)
            {
                Assert.Warn("Diagnostics for this test class's Tree should be 0");
            }

            var nodes = this.Tree.GetNamespaceRoot().DescendantNodes();
            this.GeneratedTypes = nodes.OfType<ClassDeclarationSyntax>().ToList();
            this.GeneratedEnums = nodes.OfType<EnumDeclarationSyntax>().ToList();
        }

        [Test]
        public void T1_UseGlobalEnumForGlobalEnumElement()
        {
            var type = GeneratedTypes.Single(type => type.Identifier.Text == nameof(GlobalEnumElementType));
            var prop = type.Members.OfType<PropertyDeclarationSyntax>()
                .SingleOrDefault(prop => prop.Identifier.Text == "Language");
            var getter =
                prop.AccessorList.Accessors.Single(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var ret = getter.DescendantNodes().OfType<ReturnStatementSyntax>().Single();
            Assert.IsTrue(ret.ToString().Contains($"return (({EnumTypesNamespace}.{nameof(LanguageCodeEnum)}"));
        }

        [Test]
        public void T2_UseGlobalEnumForGlobalEnumAttribute()
        {
            var type = GeneratedTypes.Single(type => type.Identifier.Text == nameof(GlobalEnumAttributeType));
            var prop = type.Members.OfType<PropertyDeclarationSyntax>()
                .SingleOrDefault(prop => prop.Identifier.Text == "language");
            var getter =
                prop.AccessorList.Accessors.Single(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var ret = getter.DescendantNodes().OfType<ReturnStatementSyntax>().Last();
            Assert.IsTrue(ret.ToString().StartsWith($"return (({EnumTypesNamespace}.{nameof(LanguageCodeEnum)}"));
        }

        [Test]
        public void T3_UseNestedEnumForNestedEnumElement()
        {
            var type = GeneratedTypes.Single(type => type.Identifier.Text == nameof(NestedEnumElementType));
            Assert.AreEqual(2, type.Members.OfType<EnumDeclarationSyntax>().Count());
            var prop = type.Members.OfType<PropertyDeclarationSyntax>()
                .SingleOrDefault(prop => prop.Identifier.Text == "Language");
            var getter =
                prop.AccessorList.Accessors.Single(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var ret = getter.DescendantNodes().OfType<ReturnStatementSyntax>().Last();
            Assert.IsTrue(ret.ToString()
                .StartsWith(
                    $"return (({EnumTypesNamespace}.{nameof(NestedEnumElementType)}.{nameof(NestedEnumElementType.LanguageEnum)}"));
        }

        [Test]
        public void T4_UseNestedEnumForNestedEnumAttribute()
        {
            var type = GeneratedTypes.Single(type => type.Identifier.Text == nameof(NestedEnumAttributeType));
            Assert.AreEqual(2, type.Members.OfType<EnumDeclarationSyntax>().Count());
            var prop = type.Members.OfType<PropertyDeclarationSyntax>()
                .SingleOrDefault(prop => prop.Identifier.Text == "language");
            var getter =
                prop.AccessorList.Accessors.Single(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var ret = getter.DescendantNodes().OfType<ReturnStatementSyntax>().Last();
            Assert.IsTrue(ret.ToString()
                .StartsWith(
                    $"return (({EnumTypesNamespace}.{nameof(NestedEnumAttributeType)}.{nameof(NestedEnumAttributeType.LanguageEnum)}"));
        }

        [Test]
        public void T5_DoNotRedefineDerivedNestedEnumAttribute()
        {
            var type = GeneratedTypes.Single(type => type.Identifier.Text == nameof(NestedDerivedEnumAttributeType));
            Assert.AreEqual(2, type.Members.OfType<EnumDeclarationSyntax>().Count());
        }

        [Test]
        public void T6_InvalidCharEnum()
        {
            var element1 = new GlobalEnumElementType
            {
                Language = LanguageCodeEnum.fr,
                Invalid  = InvalidCharEnum.en_fr,
                Empty    = string.Empty,
            };
            Assert.AreEqual(LanguageCodeEnum.fr, element1.Language);
            Assert.AreEqual(InvalidCharEnum.en_fr, element1.Invalid);
            Assert.AreEqual(string.Empty, element1.Empty);
            Assert.AreEqual(0.0m, element1.Version);
            Assert.AreEqual("fr",    ((XElement)element1.Untyped.FirstNode).Value);
            Assert.AreEqual("en-fr", ((XElement)element1.Untyped.FirstNode.NextNode).Value);

            var element2 = new GlobalEnumAttributeType
            {
                language = LanguageCodeEnum.fr,
                invalid  = InvalidCharEnum.en_fr
            };
            Assert.AreEqual(LanguageCodeEnum.fr, element2.language);
            Assert.AreEqual(InvalidCharEnum.en_fr, element2.invalid);
            Assert.AreEqual("fr",    element2.Untyped.FirstAttribute.Value);
            Assert.AreEqual("en-fr", element2.Untyped.LastAttribute.Value);

            var element3 = new NestedEnumElementType
            {
                Language = NestedEnumElementType.LanguageEnum.fr,
                Invalid  = NestedEnumElementType.InvalidEnum.en_fr
            };
            Assert.AreEqual(NestedEnumElementType.LanguageEnum.fr, element3.Language);
            Assert.AreEqual(NestedEnumElementType.InvalidEnum.en_fr, element3.Invalid);
            Assert.AreEqual("fr",    ((XElement)element3.Untyped.FirstNode).Value);
            Assert.AreEqual("en-fr", ((XElement)element3.Untyped.LastNode).Value);

            var element4 = new NestedEnumAttributeType
            {
                language = NestedEnumAttributeType.LanguageEnum.fr,
                invalid  = NestedEnumAttributeType.InvalidEnum.en_fr
            };
            Assert.AreEqual(NestedEnumAttributeType.LanguageEnum.fr, element4.language);
            Assert.AreEqual(NestedEnumAttributeType.InvalidEnum.en_fr, element4.invalid);
            Assert.AreEqual("fr",    element4.Untyped.FirstAttribute.Value);
            Assert.AreEqual("en-fr", element4.Untyped.LastAttribute.Value);

            var element5 = new NestedDerivedEnumAttributeType
            {
                language           = NestedEnumAttributeType.LanguageEnum.fr,
                invalid            = NestedEnumAttributeType.InvalidEnum.en_fr,
                additionalLanguage = NestedDerivedEnumAttributeType.AdditionalLanguageEnum.rm,
                additionalInvalid  = NestedDerivedEnumAttributeType.AdditionalInvalidEnum.it_rm,

            };
            Assert.AreEqual(NestedEnumAttributeType.LanguageEnum.fr, element5.language);
            Assert.AreEqual(NestedEnumAttributeType.InvalidEnum.en_fr, element5.invalid);
            Assert.AreEqual("fr",    element5.Untyped.FirstAttribute.Value);
            Assert.AreEqual("en-fr", element5.Untyped.FirstAttribute.NextAttribute.Value);
            Assert.AreEqual("rm",    element5.Untyped.FirstAttribute.NextAttribute.NextAttribute.Value);
            Assert.AreEqual("it-rm", element5.Untyped.FirstAttribute.NextAttribute.NextAttribute.NextAttribute.Value);
        }

        [Test]
        public void T7_ListOfEnumsSimpleType_GeneratesCorrectValidatorAndProperties()
        {
            const string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://example.com/test"" xmlns=""http://example.com/test"" elementFormDefault=""qualified"">
    <xs:simpleType name=""fontStylesType"">
        <xs:restriction>
            <xs:simpleType>
                <xs:list>
                    <xs:simpleType>
                        <xs:restriction base=""xs:string"">
                            <xs:enumeration value=""bold""/>
                            <xs:enumeration value=""italics""/>
                        </xs:restriction>
                    </xs:simpleType>
                </xs:list>
            </xs:simpleType>
            <xs:minLength value=""1""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:element name=""Root"">
        <xs:complexType>
            <xs:attribute name=""STYLE"" type=""fontStylesType""/>
        </xs:complexType>
    </xs:element>
</xs:schema>";

            var mfs = new MockFileSystem(new Dictionary<string, MockFileData> {
                { @"C:\schema.xsd", new MockFileData(xsd) }
            });

            var tree = Utilities.GenerateSyntaxTree(@"C:\schema.xsd", mfs);
            var diags = Utilities.GetSyntaxAndCompilationDiagnostics(tree);
            Assert.AreEqual(0, diags.Length);

            var nodes = tree.GetNamespaceRoot().DescendantNodes();
            var classes = nodes.OfType<ClassDeclarationSyntax>().ToList();
            var validatorClass = classes.FirstOrDefault(c => c.Identifier.Text == "fontStylesType");
            Assert.IsNotNull(validatorClass);
            Assert.IsNull(classes.FirstOrDefault(c => c.Identifier.Text == "fontStylesTypeValidator"));

            var rootClass = classes.FirstOrDefault(c => c.Identifier.Text == "Root");
            Assert.IsNotNull(rootClass);
            var styleProp = rootClass.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == "STYLE");
            Assert.IsNotNull(styleProp);
            Assert.AreEqual("IList<string>", styleProp.Type.ToString());
        }
    }
}