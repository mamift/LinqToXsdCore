using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq.Tests
{
    public class WssXsdCodeGenerationTests
    {
        private SyntaxTree Tree { get; set; }

        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string wssXsdFilePath = @"Schemas\SharePoint2010\wss.xsd";
            var code = Utilities.GenerateSourceText(wssXsdFilePath);
            Tree = CSharpSyntaxTree.ParseText(code);

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

            var x = (from cds in typesThatInheritFromXTypedElement
             where cds.Members.OfType<PropertyDeclarationSyntax>().Any(pds => pds.Modifiers.Any(m => m.ToFullString().Contains("virtual")))
             select cds).ToList();

            var members = (from cds in typesThatInheritFromXTypedElement
                            from member in cds.Members
                            select member).ToList();

            Assert.IsTrue(members.Count == 5080);

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
    }
}