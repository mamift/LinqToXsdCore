#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Xml.Schema.Linq.Extensions;
using NameAndMemberTuple = (string name, Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax member);
using AttributeNameAndCountList = System.Collections.Generic.List<(string attributeName, int count)>;
// using AttributeNameAndCountList = System.Collections.Generic.List<(string attributeName, int count)>;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class RoslynExtensions
{
    public static List<(string name, string)> ExtractAllClassMembersWithAttributeCounts(this NamespaceDeclarationSyntax root)
    {
        var list = root.ExtractAllMembersWithAttributes();

        return list.ConvertAll(nm => {
            var attListAndCount = (from a in nm.member.AttributeLists
                from attrSyntax in a.Attributes
                let name = (attrSyntax.Name as IdentifierNameSyntax)
                let count = a.Attributes.Count(m => m.Name.Equals(name))
                orderby name.Identifier.ValueText
                select (attributeName: name.Identifier.ValueText, count: count)).ToList();

            // return (nm.name, attListAndCount);
            return (nm.name, $"{attListAndCount.ToDelimitedString(tuple => $"{tuple.attributeName}:{tuple.count}")}");
        });
    }

    public static List<(string name, string)> ExtractAllClassFieldsWithAttributeCounts(this NamespaceDeclarationSyntax root)
    {
        var list = root.ExtractAllMembersWithAttributes();

        return list.Where(m => m.member is FieldDeclarationSyntax).Select(nm => {
            var attListAndCount = (from a in nm.member.AttributeLists
                from attrSyntax in a.Attributes
                let name = (attrSyntax.Name as IdentifierNameSyntax)
                let count = a.Attributes.Count(m => m.Name.Equals(name))
                orderby name.Identifier.ValueText
                select (attributeName: name.Identifier.ValueText, count: count)).ToList();

            // return (nm.name, attListAndCount);
            return (nm.name, $"{attListAndCount.ToDelimitedString(tuple => $"{tuple.attributeName}:{tuple.count}")}");
        }).ToList();
    }

    public static List<NameAndMemberTuple> ExtractAllMembersWithAttributes(this NamespaceDeclarationSyntax root)
    {
        var query = from m in root.Members
            let @class = m as ClassDeclarationSyntax
            let classMembersWithAttributes = @class.ExtractAllMembersWithAttributes()
            from classMembers in classMembersWithAttributes
            orderby classMembers.name
            select classMembers;

        return query.ToList();
    }

    public static List<NameAndMemberTuple> ExtractAllMembersWithAttributes(this ClassDeclarationSyntax cs)
    {
        var list = new List<NameAndMemberTuple>();
        foreach (MemberDeclarationSyntax member in cs.Members) {
            if (member.AttributeLists.Any()) {
                string id = null!;
                if (member is FieldDeclarationSyntax f) {
                    var ids = f.Declaration.Variables.Select(v => v.Identifier);
                    id = ids.First().ValueText + $"{cs.Identifier.ValueText}_Field";
                } else if (member is PropertyDeclarationSyntax p) {
                    id = p.Identifier.ValueText + $"{cs.Identifier.ValueText}_Property";
                }
                else {
                    throw new NotImplementedException();
                }

                list.Add((id, member));
            }
        }

        return list;
    }

    public static List<NameAndMemberTuple> ExtractAllFieldsWithAttributes(this ClassDeclarationSyntax cs)
    {
        var list = new List<NameAndMemberTuple>();
        foreach (MemberDeclarationSyntax member in cs.Members) {
            if (member.AttributeLists.Any()) {
                if (member is not FieldDeclarationSyntax f) continue;
                var ids = f.Declaration.Variables.Select(v => v.Identifier);
                var id = ids.First().ValueText + $"_{cs.Identifier.ValueText}_Field";
                list.Add((id, member));
            }
        }

        return list;
    }

    public static List<NameAndMemberTuple> ExtractAllPropertiesWithAttributes(this ClassDeclarationSyntax cs)
    {
        var list = new List<NameAndMemberTuple>();
        foreach (MemberDeclarationSyntax member in cs.Members) {
            if (member.AttributeLists.Any()) {
                if (member is not PropertyDeclarationSyntax p) continue;
                var id = p.Identifier.ValueText + $"_{cs.Identifier.ValueText}_Property";
                list.Add((id, member));
            }
        }

        return list;
    }

    /// <summary>
    /// Performs the following steps to clean a namespace for comparison. 1) sort all types and their members by name. 2) remove all doc comments
    /// 3) normalise whitespace.
    /// </summary>
    /// <param name="ns"></param>
    /// <returns></returns>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static NamespaceDeclarationSyntax CleanForComparison(this NamespaceDeclarationSyntax ns)
    {
        ns = ns.SortTypesByName();
        ns = ns.StripOfDocComments();
        ns = ns.NormalizeWhitespace();

        // var cu = ns.SyntaxTree.GetCompilationUnitRoot();
        // var container = cu.GetText().Container;
        // var ws = Workspace.GetWorkspaceRegistration(container);

        return ns;
    }

    /// <summary>
    /// Remove doc comments from each type defined in the namespace.
    /// </summary>
    /// <param name="ns"></param>
    /// <returns></returns>
    public static NamespaceDeclarationSyntax StripOfDocComments(this NamespaceDeclarationSyntax ns)
    {
        // strip all /// triple slash doc comments from types (classes, structs etc) and return the updated namespace
        var members = ns.Members;
        if (members.Count == 0)
            return ns;

        var newMembers = new SyntaxList<MemberDeclarationSyntax>();
        foreach (var member in members)
        {
            MemberDeclarationSyntax updated = member;

            switch (member)
            {
                case TypeDeclarationSyntax typeDecl:
                    updated = StripLeadingTripleSlashDocComments(typeDecl);
                    updated = typeDecl.StripOfDocComments();
                    break;

                case DelegateDeclarationSyntax delegateDecl:
                    updated = StripLeadingTripleSlashDocComments(delegateDecl);
                    break;

                default:
                    // Leave non-type members (e.g., namespaces) untouched
                    break;
            }

            newMembers = newMembers.Add(updated);
        }

        return ns.WithMembers(newMembers);
    }

    /// <summary>
    /// Remove doc comments from each member defined in the class.
    /// </summary>
    /// <param name="ns"></param>
    /// <returns></returns>
    public static TypeDeclarationSyntax StripOfDocComments(this TypeDeclarationSyntax ns)
    {
        // strip all /// triple slash doc comments from members (fields, properties, methods, constructors) and return the updated type syntax
        var members = ns.Members;
        if (members.Count == 0)
            return ns;

        var newMembers = new SyntaxList<MemberDeclarationSyntax>();
        foreach (var member in members)
        {
            // Remove leading triple-slash docs on the member itself
            var updated = StripLeadingTripleSlashDocComments(member);

            // If the member is a nested type, also strip docs from its inner members (recursive)
            if (updated is TypeDeclarationSyntax nestedType)
            {
                updated = nestedType.StripOfDocComments();
            }

            newMembers = newMembers.Add(updated);
        }

        return ns.WithMembers(newMembers);
    }

    private static SyntaxTriviaList StripTripleSlashDocComments(SyntaxTriviaList trivia)
    {
        if (trivia.Count == 0)
            return trivia;

        var list = new List<SyntaxTrivia>(trivia.Count);

        for (int i = 0; i < trivia.Count; i++)
        {
            var t = trivia[i];

            if (t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                // Remove indentation immediately preceding the doc comment if it's the start of a line
                if (list.Count > 0 &&
                    list[^1].IsKind(SyntaxKind.WhitespaceTrivia) &&
                    (list.Count == 1 || list[^2].IsKind(SyntaxKind.EndOfLineTrivia)))
                {
                    list.RemoveAt(list.Count - 1);
                }

                // Also skip the newline that follows the doc comment line (if present)
                if (i + 1 < trivia.Count && trivia[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
                    i++;

                continue; // Drop the doc comment trivia
            }

            list.Add(t);
        }

        return SyntaxFactory.TriviaList(list);
    }

    private static T StripLeadingTripleSlashDocComments<T>(T node) where T : SyntaxNode
    {
        return node.WithLeadingTrivia(StripTripleSlashDocComments(node.GetLeadingTrivia()));
    }

    public static SourceText ToSourceText(this FileInfo csFile)
    {
        using StreamReader text = csFile.OpenText();

        return SourceText.From(text, (int)text.BaseStream.Length);
    }

    public static CSharpSyntaxTree ToSyntaxTree(this FileInfo csFile)
    {
        SourceText source = csFile.ToSourceText();

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default);

        return (CSharpSyntaxTree)tree;
    }

    public static void WriteToFile(this NamespaceDeclarationSyntax ns, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

        using var streamWriter = new StreamWriter(filePath);
        ns.WriteTo(streamWriter);
        streamWriter.Flush();
    }

    public static NamespaceDeclarationSyntax SortTypesByName(this NamespaceDeclarationSyntax ns)
    {
        // Sort only direct class members of the namespace by name (Ordinal),
        // keep non-class members in their original positions, and preserve formatting.
        var members = ns.Members;

        var sortedClasses = members
            .OfType<TypeDeclarationSyntax>()
            .OrderBy(c => c.Identifier.ValueText, StringComparer.Ordinal)
            .ToList();

        if (sortedClasses.Count <= 1)
            return ns;

        int classIndex = 0;
        var newMembers = new SyntaxList<MemberDeclarationSyntax>();

        foreach (var member in members)
        {
            if (member is ClassDeclarationSyntax originalSlot)
            {
                var nextClass = sortedClasses[classIndex++]
                    .WithLeadingTrivia(originalSlot.GetLeadingTrivia())
                    .WithTrailingTrivia(originalSlot.GetTrailingTrivia());

                nextClass = nextClass.SortMembersByIdentifier();

                newMembers = newMembers.Add(nextClass);
            }
            else
            {
                newMembers = newMembers.Add(member);
            }
        }

        return ns.WithMembers(newMembers);
    }

    public static TypeDeclarationSyntax SortMembersByIdentifier(this TypeDeclarationSyntax cs)
    {
        // Sort identifiable members (those with a name) by their identifier (Ordinal).
        // Non-identifiable members (e.g., indexers, operators) stay in place.
        // Formatting is preserved by transferring original slot trivia.
        var members = cs.Members;

        // Collect identifiable members with their names
        var identifiable = members
            .Select(m => (member: m, hasName: TryGetMemberIdentifier(m, out var name), name))
            .Where(x => x.hasName)
            .Select(x => (x.member, name: x.name))
            .ToList();

        if (identifiable.Count <= 1)
            return cs;

        // Sort by identifier
        var sortedByName = identifiable
            .OrderBy(x => x.name, StringComparer.Ordinal)
            .Select(x => x.member)
            .ToList();

        int idx = 0;
        var newMembers = new SyntaxList<MemberDeclarationSyntax>();

        foreach (var member in members)
        {
            if (TryGetMemberIdentifier(member, out _))
            {
                var placed = sortedByName[idx++]
                    .WithLeadingTrivia(member.GetLeadingTrivia())
                    .WithTrailingTrivia(member.GetTrailingTrivia());
                newMembers = newMembers.Add(placed);
            }
            else
            {
                newMembers = newMembers.Add(member);
            }
        }

        return cs.WithMembers(newMembers);
    }

    private static bool TryGetMemberIdentifier(MemberDeclarationSyntax member, out string name)
    {
        switch (member)
        {
            // Nested types
            case ClassDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case StructDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case InterfaceDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case EnumDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case DelegateDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case RecordDeclarationSyntax n: name = n.Identifier.ValueText; return true;

            // Members with direct identifiers
            case MethodDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case PropertyDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case EventDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case ConstructorDeclarationSyntax n: name = n.Identifier.ValueText; return true;
            case DestructorDeclarationSyntax n: name = n.Identifier.ValueText; return true;

            // Members whose names come from variables (use first variable)
            case FieldDeclarationSyntax n:
                name = n.Declaration?.Variables.FirstOrDefault()?.Identifier.ValueText ?? string.Empty;
                return !string.IsNullOrEmpty(name);
            case EventFieldDeclarationSyntax n:
                name = n.Declaration?.Variables.FirstOrDefault()?.Identifier.ValueText ?? string.Empty;
                return !string.IsNullOrEmpty(name);
        }

        name = string.Empty;
        return false;
    }
}