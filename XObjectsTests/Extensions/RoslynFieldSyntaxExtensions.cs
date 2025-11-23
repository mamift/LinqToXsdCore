#nullable enable
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class RoslynFieldSyntaxExtensions
{
    public static string? PossibleIdentifier(this FieldDeclarationSyntax f)
    {
        var possibleId = f.Declaration.Variables.Select(v => v.Identifier).FirstOrDefault();

        return possibleId.ValueText;
    }
}