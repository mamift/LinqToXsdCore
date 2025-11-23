using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq;
using ObjectsComparer;

namespace Xml.Schema.Linq.Tests.Extensions;

using PropOrField = OneOf.OneOf<PropertyDeclarationSyntax, TypeDeclarationSyntax>;
using SummaryOfClassMembers = IOrderedEnumerable<IGrouping<string, (string MemberName, List<string> DistinctAttrs)>>;

public static class RoslynClassReportExtensions
{
    public static List<Difference> CompareProperties(this NamespaceDeclarationSyntax ns1,
        NamespaceDeclarationSyntax ns2)
    {
        var classes1 = ns1.Members.OfType<TypeDeclarationSyntax>().ToList();
        var classes2 = ns2.Members.OfType<TypeDeclarationSyntax>().ToList();

        var classesList1 = classes1.Select(c => c.Identifier.ValueText).OrderBy(a => a).ToString();
        var classesList2 = classes2.Select(c => c.Identifier.ValueText).OrderBy(a => a).ToString();

        var compareClassLists = classesList1.CompareObjects(classesList2);
        if (compareClassLists.Any()) {
            return compareClassLists;
        }

        var propsPerClass1 = classes1.GetSummaryOfProperties().ToComparisonString();
        var propsPerClass2 = classes2.GetSummaryOfProperties().ToComparisonString();

        var compareNamespaces = propsPerClass1.CompareObjects(propsPerClass2);
        return compareNamespaces;
    }

    public static List<Difference> CompareFields(this NamespaceDeclarationSyntax ns1,
        NamespaceDeclarationSyntax ns2)
    {
        var classes1 = ns1.Members.OfType<TypeDeclarationSyntax>().ToList();
        var classes2 = ns2.Members.OfType<TypeDeclarationSyntax>().ToList();

        var classesList1 = classes1.Select(c => c.Identifier.ValueText).OrderBy(a => a).ToString();
        var classesList2 = classes2.Select(c => c.Identifier.ValueText).OrderBy(a => a).ToString();

        var compareClassLists = classesList1.CompareObjects(classesList2);
        if (compareClassLists.Any()) {
            return compareClassLists;
        }

        var propsPerClass1 = classes1.GetSummaryOfFields().ToComparisonString();
        var propsPerClass2 = classes2.GetSummaryOfFields().ToComparisonString();

        var compareNamespaces = propsPerClass1.CompareObjects(propsPerClass2);
        return compareNamespaces;
    }

    public static SummaryOfClassMembers GetSummaryOfProperties(this IEnumerable<TypeDeclarationSyntax> types)
    {
        IOrderedEnumerable<IGrouping<string, (string PropName, List<string> DistincAttrs)>> query =
            from c in types
            from p in c.Members.OfType<PropertyDeclarationSyntax>()
            let propSummary = (
                PropName: p.Identifier.ValueText,
                // DistinctAttrs: p.AttributeLists.SelectMany(a => a.Attributes.Select(attrSyn => attrSyn.Name).Distinct()).Distinct().ToList()
                DistincAttrs: (from atl in p.AttributeLists
                    from aast in atl.Attributes
                    let nameStr = aast.Name as IdentifierNameSyntax
                    select nameStr?.Identifier.ValueText).ToList()
            )
            where propSummary.DistincAttrs.Any()
            group propSummary by c.Identifier.ValueText
            into propGroup
            orderby propGroup.Key
            select propGroup;

        return query;
    }

    public static SummaryOfClassMembers GetSummaryOfFields(this IEnumerable<TypeDeclarationSyntax> types)
    {
        IOrderedEnumerable<IGrouping<string, (string PropName, List<string> DistincAttrs)>> query =
            from c in types
            from p in c.Members.OfType<FieldDeclarationSyntax>()
            let propSummary = (
                PropName: p.Declaration.Variables.Single().Identifier.ValueText,
                // DistinctAttrs: p.AttributeLists.SelectMany(a => a.Attributes.Select(attrSyn => attrSyn.Name).Distinct()).Distinct().ToList()
                DistincAttrs: (from atl in p.AttributeLists
                    from aast in atl.Attributes
                    let nameStr = aast.Name as IdentifierNameSyntax
                    select nameStr?.Identifier.ValueText).ToList()
            )
            where propSummary.DistincAttrs.Any()
            group propSummary by c.Identifier.ValueText
            into propGroup
            orderby propGroup.Key
            select propGroup;

        return query;
    }

    public static List<string> ToComparisonString(this SummaryOfClassMembers t)
    {
        var query = from g in t
            from c in g
            select $"Class = {g.Key}, Property = {c.MemberName}, Attrs = {c.DistinctAttrs.ToDelimitedString(",")}";

        return query.ToList();
    }
}