using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using Xml.Schema.Linq.Tests.Extensions;

namespace Xml.Schema.Linq.Tests;

internal class RoslynCodeSplittingTests
{
    [Test]
    public void PrototypeSplitByNamespaceAndClass()
    {
        DirectoryInfo schemaLibFolder = new DirectoryInfo(Environment.CurrentDirectory).AscendToFolder("GeneratedSchemaLibraries");
        DirectoryInfo projectFolder = schemaLibFolder.DescendToFolder("Microsoft Project 2007");

        var csFiles = projectFolder.GetFiles("mspdi_pj12.xsd.cs");

        Assert.IsNotEmpty(csFiles);

        var filePath = csFiles.First().FullName;

        using var stream = File.OpenRead(filePath);
        var sourceText = SourceText.From(stream, Encoding.UTF8);
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        // Gather shared usings/namespace context
        var usings = root.Usings.ToFullString();

        foreach (var member in root.Members) {
            if (member is BaseNamespaceDeclarationSyntax ns) {
                foreach (var typeMember in ns.Members.OfType<BaseTypeDeclarationSyntax>()) {
                    string outputFile = Path.Combine(projectFolder.FullName, $"{typeMember.Identifier.Text}.cs");
                    var content = $"{usings}\nnamespace {ns.Name};\n\n{typeMember.ToFullString()}";
                    File.WriteAllText(outputFile, content);
                }
            }
            else if (member is BaseTypeDeclarationSyntax type) {
                string outputFile = Path.Combine(projectFolder.FullName, $"{type.Identifier.Text}.cs");
                var content = $"{usings}\n{type.ToFullString()}";
                File.WriteAllText(outputFile, content);
            }
        }
    }
}