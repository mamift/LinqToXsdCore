using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Schemas.SharePoint;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    public class WssXsdCodeGenerationTests
    {
        public SyntaxTree Tree { get; private set; }

        public List<ClassDeclarationSyntax> GeneratedTypes { get; private set; }

        public XmlSchemaSet XmlSchemaSet { get; private set; }

        [SetUp]
        public void GenerateCode()
        {
            const string wssXsdFilePath = @"Schemas\SharePoint2010\wss.xsd";
            var wssXsdFileInfo = new FileInfo(wssXsdFilePath);
            Tree = Utilities.GenerateSyntaxTree(wssXsdFileInfo);
            XmlSchemaSet = Utilities.CompileXmlSchemaSet(wssXsdFileInfo);
            GeneratedTypes = Tree
                             .GetNamespaceRoot()
                             .DescendantNodes()
                             .OfType<ClassDeclarationSyntax>().ToList();
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
            var typesThatInheritFromXTypedElement = GeneratedTypes
                                                    .Where(cd =>
                                                        cd.BaseList?.ToFullString().Contains(nameof(XTypedElement)) ??
                                                        false).ToList();

            Assert.IsNotEmpty(typesThatInheritFromXTypedElement);
            Assert.IsTrue(typesThatInheritFromXTypedElement.Count == 279);

            var typesWithGeneratedProperties = (from cds in typesThatInheritFromXTypedElement
                                                where cds.Members.OfType<PropertyDeclarationSyntax>().Any(pds =>
                                                    pds.Modifiers.Any(m => m.ToFullString().Contains("virtual")))
                                                select cds).ToList();

            Assert.IsNotEmpty(typesWithGeneratedProperties);
            Assert.IsTrue(typesWithGeneratedProperties.Count == 275);

            var typesWithoutGeneratedProperties =
                typesThatInheritFromXTypedElement.Except(typesWithGeneratedProperties).ToList();

            Assert.IsNotEmpty(typesWithoutGeneratedProperties);
            Assert.IsTrue(typesWithoutGeneratedProperties.Count == 4);

            var members = typesThatInheritFromXTypedElement.SelectMany(cds => cds.Members).ToList();

            Assert.IsTrue(members.Count == 5084);

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

            var codeGenOutput = typesWithEnumsDefined.ToFullString();
            var compiledXsd = XmlSchemaSet.ExtractGlobalItemsToSingleFileSchema().ToXmlString();
            
            TestContext.CurrentContext.DumpDebugOutputToFile(debugStrings: new[] { codeGenOutput, compiledXsd });

            Assert.IsNotEmpty(typesWithEnumsDefined);
            Assert.IsTrue(typesWithEnumsDefined.Count() == 3);
        }

        [Test]
        public void GenerateInlineDeclaredEnumsTest()
        {
            var inlineDeclaredEnums = GeneratedTypes.SelectMany(t => 
                t.DescendantNodes().OfType<EnumDeclarationSyntax>().Distinct()).ToList();

            TestContext.CurrentContext.DumpDebugOutputToFile(debugStrings: new [] { Tree.ToString() });

            Assert.IsNotEmpty(inlineDeclaredEnums);
            Assert.IsTrue(inlineDeclaredEnums.Count == 4);
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
