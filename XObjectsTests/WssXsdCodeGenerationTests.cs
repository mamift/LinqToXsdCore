using System.Collections.Generic;
using System.IO;
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

        [SetUp]
        public void GenerateCode()
        {
            const string wssXsdFilePath = @"SharePoint2010\wss.xsd";
            var wssXsdFileInfo = new FileInfo(wssXsdFilePath);
            Tree = Utilities.GenerateSyntaxTree(wssXsdFileInfo);

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
            Assert.IsTrue(GeneratedTypes.Count == 399);

            var sealedTypes = GeneratedTypes.Where(t => t.Modifiers.Any(c => c.IsKind(SyntaxKind.SealedKeyword)))
                                               .ToList();
            Assert.IsNotEmpty(sealedTypes);
            Assert.IsTrue(sealedTypes.Count == 89);

            var partialTypes = GeneratedTypes.Where(t => t.Modifiers.Any(c => c.IsKind(SyntaxKind.PartialKeyword)) &&
                                                            t.Modifiers.All(c => !c.IsKind(SyntaxKind.SealedKeyword)))
                                                .ToList();
            Assert.IsNotEmpty(partialTypes);
            Assert.IsTrue(partialTypes.Count == 309);

            var others = GeneratedTypes.Except(sealedTypes).Except(partialTypes).ToList();
            Assert.IsNotEmpty(others);
            Assert.IsTrue(others.Count == 1);
            Assert.IsTrue(others.First().Identifier.Value as string == Constants.LinqToXsdTypeManager);
        }

        [Test]
        public void TypesThatInheritFromXTypedElementTest()
        {
            Assert.IsNotEmpty(TypesThatInheritFromXTypedElement);
            Assert.IsTrue(TypesThatInheritFromXTypedElement.Count == 279);

            var typesWithGeneratedProperties = (from cds in TypesThatInheritFromXTypedElement
                                                where
                                                    (from pds in cds.Members.OfType<PropertyDeclarationSyntax>()
                                                     where pds.Modifiers.Any(m => m.ToFullString().Contains("virtual"))
                                                     select pds).Any()
                                                select cds).ToList();
            Assert.IsNotEmpty(typesWithGeneratedProperties);
            Assert.IsTrue(typesWithGeneratedProperties.Count == 275);

            var typesWithoutGeneratedProperties =
                TypesThatInheritFromXTypedElement.Except(typesWithGeneratedProperties).ToList();
            Assert.IsNotEmpty(typesWithoutGeneratedProperties);
            Assert.IsTrue(typesWithoutGeneratedProperties.Count == 4);
        }

        [Test]
        public void TypesThatInheritFromXTypedElementTestAndTheirMembers()
        {
            var members = TypesThatInheritFromXTypedElement.SelectMany(cds => cds.Members).ToList();
            var actual = members.Count;
            const int expected = 5084;
            if (actual != expected) Assert.Warn(Utilities.WarningMessage(expected, actual));

            var propertyMembers = members.OfType<PropertyDeclarationSyntax>().ToList();
            Assert.IsTrue(propertyMembers.Count == 2939);

            var virtualPropertyMembers = propertyMembers
                .Where(p => p.Modifiers.Any(m => m.ToFullString().Contains("virtual")))
                .ToList();
            Assert.IsNotEmpty(virtualPropertyMembers);
            Assert.IsTrue(virtualPropertyMembers.Count == 1923);

            var typesWhoseMembersAreVirtual =
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

            var xRootClassMembers = xRootClass.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();

            Assert.IsNotEmpty(xRootClassMembers);

            var last = xRootClassMembers.LastOrDefault(m => m.Identifier.ValueText == "Root");

            Assert.IsNotNull(last);
        }

        [Test]
        public void ClassesWithInlineEnumsGenerationTest()
        {
            var typesWithEnumsDefined = GeneratedTypes.Where(t => t.Members.OfType<EnumDeclarationSyntax>().Any()).ToList();

            Assert.IsNotEmpty(typesWithEnumsDefined);
            var expected = 3;
            var actual = typesWithEnumsDefined.Count;
            var isExpected = actual == expected;
            
            if (!isExpected) Assert.Warn(Utilities.WarningMessage(expected, actual));
        }

        [Test]
        public void GenerateInlineDeclaredEnumsTest()
        {
            var inlineDeclaredEnums = GeneratedTypes.SelectMany(t => t.Members.OfType<EnumDeclarationSyntax>()).ToList();

            Assert.IsNotEmpty(inlineDeclaredEnums);
            const int expected = 4;
            var actual = inlineDeclaredEnums.Count;
            var isExpected = actual == expected;
            if (!isExpected) Assert.Warn(Utilities.WarningMessage(expected, actual));
            else Assert.Pass();
        }

        [Test]
        public void DuplicateInlineDeclaredEnumsAllowedTest()
        {
            // multiple inline defined enum types can be declared with the same name so long as they exist in
            // different complex types or elements.
            var inlineDeclaredEnumsWithSameName = GeneratedTypes.SelectMany(t => t.Members.OfType<EnumDeclarationSyntax>())
                .Where(c => c.Identifier.ValueText == nameof(parametersType.ParameterLocalType.DesignerTypeEnum)).ToList();

            Assert.IsNotEmpty(inlineDeclaredEnumsWithSameName);
            Assert.IsTrue(inlineDeclaredEnumsWithSameName.Count == 2);
        }

        [Test]
        public void DuplicateInlineDeclaredEnumsActuallyHaveDifferingMembersTest()
        {
            // multiple inline defined enum types can be declared with the same name so long as they exist in
            // different complex types or elements.
            var inlineDeclaredEnumsWithSameName = GeneratedTypes.SelectMany(t => t.Members.OfType<EnumDeclarationSyntax>())
                .Where(c => c.Identifier.ValueText == nameof(parametersType.ParameterLocalType.DesignerTypeEnum)).ToList();

            var membersGroupedByParentId = inlineDeclaredEnumsWithSameName.SelectMany(e => e.Members)
                .GroupBy(k => ((EnumDeclarationSyntax) k.Parent).Identifier)
                .ToList();

            Assert.IsTrue(membersGroupedByParentId.Count() == 2);
            Assert.IsTrue(membersGroupedByParentId.First().Count() == 33);
            Assert.IsTrue(membersGroupedByParentId.Last().Count() == 20);
        }
    }
}