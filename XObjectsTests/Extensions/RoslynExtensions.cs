#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class RoslynExtensions
{
    public static SourceText ToSourceText(this FileInfo csFile)
    {
        StreamReader text = csFile.OpenText();

        return SourceText.From(text, (int)text.BaseStream.Length);
    }

    public static CSharpSyntaxTree ToSyntaxTree(this FileInfo csFile)
    {
        var source = csFile.ToSourceText();

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default);

        return (CSharpSyntaxTree)tree;
    }

    public static void WriteToFile(this NamespaceDeclarationSyntax ns, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

        using var streamWriter = new StreamWriter(filePath);
        ns.WriteTo(streamWriter);
    }

    public static NamespaceDeclarationSyntax SortClassesByName(this NamespaceDeclarationSyntax ns)
    {
        // Sort only direct class members of the namespace by name (Ordinal),
        // keep non-class members in their original positions, and preserve formatting.
        var members = ns.Members;

        var sortedClasses = members
            .OfType<ClassDeclarationSyntax>()
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

    public static ClassDeclarationSyntax SortMembersByIdentifier(this ClassDeclarationSyntax cs)
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