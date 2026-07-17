using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Xml.Schema.Linq.Tests.Extensions;

namespace Xml.Schema.Linq.Tests;

public class NoSealedClassAndVirtualProperties: BaseTester
{
    public MockFileSystem TestFiles { get; set; } = null!;

    public NoSealedClassAndVirtualProperties()
    {
        TestFiles = Utilities.GetAssemblyFileSystem(typeof(LinqToXsd.Schemas.XInclude.fallbackType).Assembly);
    }

    [Test]
    public void XIncludeTest()
    {
        string xincludeXsd = "xinclude.xsd";
        var xIncludeXsdFile = TestFiles.AllFiles.Single(f => f.EndsWith(xincludeXsd));
        CSharpSyntaxTree xIncludeSourceTree = Utilities.GenerateSyntaxTree(xIncludeXsdFile, TestFiles);

        var generatedTypes = xIncludeSourceTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(t => t.Modifiers.Any(m => m.ValueText == "sealed")).ToList();

        Assert.True(generatedTypes.Any());

        foreach (var type in generatedTypes) {

            Assert.True(type.Modifiers.Any(m => m.ValueText == "sealed"));

            List<(string name, MemberDeclarationSyntax member)> props = type.GetAllPropertiesWithoutAttributes();
            foreach (var (name, member) in props) {
                var pds = member as PropertyDeclarationSyntax;
                Assert.NotNull(pds);

                var virtualModifiers = pds!.Modifiers.Where(m => m.IsKeyword() && m.ValueText == "virtual").ToList();
                Assert.True(virtualModifiers.Count == 0);
            }
        }
    }
}