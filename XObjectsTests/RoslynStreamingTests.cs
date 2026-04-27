using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests;

internal class RoslynStreamingTests
{
    [Test]
    public void Prototype1()
    {
        var filePath = @"C:\Development\devdrive\GitHub\LinqToXsdCore\GeneratedSchemaLibraries\Microsoft Project 2007\mspdi_pj12.xsd.cs";
        using var stream = File.OpenRead(filePath);
        var sourceText = SourceText.From(stream, Encoding.UTF8);
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        // Gather shared usings/namespace context
        var usings = root.Usings.ToFullString();

        foreach (var member in root.Members) {
            if (member is BaseNamespaceDeclarationSyntax ns) {
                foreach (var typeMember in ns.Members.OfType<BaseTypeDeclarationSyntax>()) {
                    var outputFile = $"{typeMember.Identifier.Text}.cs";
                    var content = $"{usings}\nnamespace {ns.Name};\n\n{typeMember.ToFullString()}";
                    File.WriteAllText(outputFile, content);
                }
            }
            else if (member is BaseTypeDeclarationSyntax type) {
                var outputFile = $"{type.Identifier.Text}.cs";
                var content = $"{usings}\n{type.ToFullString()}";
                File.WriteAllText(outputFile, content);
            }
        }
    }
}