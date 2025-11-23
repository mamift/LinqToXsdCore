using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class RoslynSyntaxTreeExtensions
{
    public static List<AttributeSyntax> GetAllPropertyDescendantAttributes(this NamespaceDeclarationSyntax root)
    {
        var allPropertiesWithAttrs = root.GetAllPropertyDescendantsWithAttrs();

        var allPropsAttributes = allPropertiesWithAttrs
            .SelectMany(p => p.AttributeLists.SelectMany(al => al.Attributes)).ToList();

        return allPropsAttributes;
    }

    public static List<PropertyDeclarationSyntax> GetAllPropertyDescendantsWithAttrs(this NamespaceDeclarationSyntax root)
    {
        var allProperties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();

        var allPropertiesWithAttrs = (from p in allProperties
            where p.AttributeLists.Select(al => al.Attributes).Any()
            orderby p.Identifier.ValueText
            select p).ToList();

        return allPropertiesWithAttrs;
    }

    public static List<AttributeSyntax> GetAllFieldDescendantAttributes(this NamespaceDeclarationSyntax root)
    {
        var allFieldsWithAttrs = root.GetAllFieldDescendantsWithAttrs();

        var allFieldAttributes = allFieldsWithAttrs
            .SelectMany(f => f.AttributeLists.SelectMany(al => al.Attributes)).ToList();

        return allFieldAttributes;
    }

    public static List<FieldDeclarationSyntax> GetAllFieldDescendantsWithAttrs(this NamespaceDeclarationSyntax root)
    {
        var allFields = root.DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();

        var allFieldsWithAttrs = (from field in allFields
            where field.AttributeLists.Select(al => al.Attributes).Any()
            select field).ToList();

        return allFieldsWithAttrs;
    }
}