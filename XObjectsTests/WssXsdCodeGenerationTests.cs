using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Schemas.SharePoint;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq.Tests
{
    public class WssXsdCodeGenerationTests
    {
        private List<ClassDeclarationSyntax> TypesThatInheritFromXTypedElement { get; set; }

        private SyntaxTree Tree { get; set; }

        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }

        private MockFileSystem WssFiles { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            WssFiles = new MockFileSystem(Utilities.GetAssemblyTextFilesDictionary(typeof(FieldDefinition).Assembly));

            const string wssXsdFilePath = @"SharePoint2010\wss.xsd";
            
            var wssXsdFileInfo = new MockFileInfo(WssFiles, wssXsdFilePath);

            MockFileData mockFile = WssFiles.GetFile(wssXsdFilePath);
            Tree = Utilities.GenerateSyntaxTree(wssXsdFileInfo, WssFiles);

            Diagnostic[] diagnotics = Tree.GetDiagnostics().ToArray();
            diagnotics.CheckForZeroDiagnosticsAndErrorsOrThrow(wssXsdFilePath);

            GeneratedTypes = Tree
                             .GetNamespaceRoot()
                             .DescendantNodes()
                             .OfType<ClassDeclarationSyntax>().ToList();
            
            TypesThatInheritFromXTypedElement = GeneratedTypes
                .Where(cd =>
                    cd.BaseList?.ToFullString().Contains(nameof(XTypedElement)) ??
                    false).ToList();
        }

        [Test]
        public void TypesGeneratedCountTest()
        {
            var namespaceNode = Tree.GetNamespaceRoot();

            Assert.IsNotNull(namespaceNode);
            Assert.IsNotEmpty(GeneratedTypes);
            Assert.IsTrue(GeneratedTypes.Count == 403);

            List<ClassDeclarationSyntax> sealedTypes = GeneratedTypes.Where(t => t.Modifiers.Any(c => c.IsKind(SyntaxKind.SealedKeyword)))
                                               .ToList();
            Assert.IsNotEmpty(sealedTypes);
            Assert.IsTrue(sealedTypes.Count == 93);

            List<ClassDeclarationSyntax> partialTypes = GeneratedTypes.Where(t => t.Modifiers.Any(c => c.IsKind(SyntaxKind.PartialKeyword)) &&
                                                                                  t.Modifiers.All(c => !c.IsKind(SyntaxKind.SealedKeyword)))
                                                .ToList();
            Assert.IsNotEmpty(partialTypes);
            Assert.IsTrue(partialTypes.Count == 309);

            List<ClassDeclarationSyntax> others = GeneratedTypes.Except(sealedTypes).Except(partialTypes).ToList();
            Assert.IsNotEmpty(others);
            Assert.IsTrue(others.Count == 1);
            Assert.IsTrue(others.First().Identifier.Value as string == Constants.LinqToXsdTypeManager);
        }

        [Test]
        public void TypesThatInheritFromXTypedElementTest()
        {
            Assert.IsNotEmpty(TypesThatInheritFromXTypedElement);
            Assert.IsTrue(TypesThatInheritFromXTypedElement.Count == 279);

            List<ClassDeclarationSyntax> typesWithGeneratedProperties = (from cds in TypesThatInheritFromXTypedElement
                                                where
                                                    (from pds in cds.Members.OfType<PropertyDeclarationSyntax>()
                                                     where pds.Modifiers.Any(m => m.ToFullString().Contains("virtual"))
                                                     select pds).Any()
                                                select cds).ToList();
            Assert.IsNotEmpty(typesWithGeneratedProperties);
            Assert.IsTrue(typesWithGeneratedProperties.Count == 275);

            List<ClassDeclarationSyntax> typesWithoutGeneratedProperties =
                TypesThatInheritFromXTypedElement.Except(typesWithGeneratedProperties).ToList();
            Assert.IsNotEmpty(typesWithoutGeneratedProperties);
            Assert.IsTrue(typesWithoutGeneratedProperties.Count == 4);
        }

        [Test]
        public void TypesThatInheritFromXTypedElementTestAndTheirMembers()
        {
            List<MemberDeclarationSyntax> members = TypesThatInheritFromXTypedElement.SelectMany(cds => cds.Members).ToList();
            int actual = members.Count;
            const int expected = 7188;
            if (actual != expected) Assert.Fail(Utilities.WarningMessage(expected, actual));

            List<PropertyDeclarationSyntax> propertyMembers = members.OfType<PropertyDeclarationSyntax>().ToList();
            Assert.IsTrue(propertyMembers.Count == 2939);

            List<PropertyDeclarationSyntax> virtualPropertyMembers = propertyMembers
                .Where(p => p.Modifiers.Any(m => m.ToFullString().Contains("virtual")))
                .ToList();
            Assert.IsNotEmpty(virtualPropertyMembers);
            Assert.IsTrue(virtualPropertyMembers.Count == 1923);

            List<ClassDeclarationSyntax?> typesWhoseMembersAreVirtual =
                virtualPropertyMembers.Select(vpm => vpm.Parent as ClassDeclarationSyntax).Distinct().ToList();
            Assert.IsNotEmpty(typesWhoseMembersAreVirtual);
            Assert.IsTrue(typesWhoseMembersAreVirtual.Count == 275);
        }

        [Test]
        public void GeneratedXRootClassTest()
        {
            var xRootClass = GeneratedTypes
                .First(cds => cds.Identifier.Value.ToString() == nameof(XRoot));

            Assert.IsNotNull(xRootClass);

            List<PropertyDeclarationSyntax> xRootClassMembers = xRootClass.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();

            Assert.IsNotEmpty(xRootClassMembers);

            var last = xRootClassMembers.LastOrDefault(m => m.Identifier.ValueText == "Root");

            Assert.IsNotNull(last);
        }

        [Test]
        public void ClassesWithInlineEnumsGenerationTest()
        {
            List<ClassDeclarationSyntax> typesWithEnumsDefined = GeneratedTypes.Where(t => t.Members.OfType<EnumDeclarationSyntax>().Any()).ToList();

            Assert.IsNotEmpty(typesWithEnumsDefined);
            int expected = 3;
            int actual = typesWithEnumsDefined.Count;
            bool isExpected = actual == expected;
            
            if (!isExpected) Assert.Fail(Utilities.WarningMessage(expected, actual));
        }

        [Test]
        public void GenerateInlineDeclaredEnumsTest()
        {
            List<EnumDeclarationSyntax> inlineDeclaredEnums = GeneratedTypes.SelectMany(t => t.Members.OfType<EnumDeclarationSyntax>()).ToList();

            Assert.IsNotEmpty(inlineDeclaredEnums);
            const int expected = 4;
            int actual = inlineDeclaredEnums.Count;
            bool isExpected = actual == expected;
            if (!isExpected) Assert.Fail(Utilities.WarningMessage(expected, actual));
            else Assert.Pass();
        }

        [Test]
        public void DuplicateInlineDeclaredEnumsAllowedTest()
        {
            // multiple inline defined enum types can be declared with the same name so long as they exist in
            // different complex types or elements.
            List<EnumDeclarationSyntax> inlineDeclaredEnumsWithSameName = GeneratedTypes.SelectMany(t => t.Members.OfType<EnumDeclarationSyntax>())
                .Where(c => c.Identifier.ValueText == nameof(parametersType.ParameterLocalType.DesignerTypeEnum)).ToList();

            Assert.IsNotEmpty(inlineDeclaredEnumsWithSameName);
            Assert.IsTrue(inlineDeclaredEnumsWithSameName.Count == 2);
        }

        [Test]
        public void DuplicateInlineDeclaredEnumsActuallyHaveDifferingMembersTest()
        {
            // multiple inline defined enum types can be declared with the same name so long as they exist in
            // different complex types or elements.
            List<EnumDeclarationSyntax> inlineDeclaredEnumsWithSameName = GeneratedTypes.SelectMany(t => t.Members.OfType<EnumDeclarationSyntax>())
                .Where(c => c.Identifier.ValueText == nameof(parametersType.ParameterLocalType.DesignerTypeEnum)).ToList();

            List<IGrouping<SyntaxToken, EnumMemberDeclarationSyntax>> membersGroupedByParentId = inlineDeclaredEnumsWithSameName.SelectMany(e => e.Members)
                .GroupBy(k => ((EnumDeclarationSyntax) k.Parent).Identifier)
                .ToList();

            Assert.IsTrue(membersGroupedByParentId.Count() == 2);
            Assert.IsTrue(membersGroupedByParentId.First().Count() == 33);
            Assert.IsTrue(membersGroupedByParentId.Last().Count() == 20);
        }
    }
}