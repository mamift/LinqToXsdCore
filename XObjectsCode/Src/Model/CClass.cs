#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq.CodeGen.Model;

/// <summary>
/// Represents a class in generated code, specialized into various kind of xsd source: simple type, element.
/// </summary>
public abstract class CClass
{
    public CNamespace? Namespace { get; set; }    

    // This isn't great OOP design but Scriban doesn't have first-class support for OOP (no `is` operator).
    // Maybe it'd be cleaner to put the `is CSimpleType` behind a global function.
    public virtual bool IsSimpleType => false;

    [return: NotNullIfNotNull(nameof(name))]
    protected static string? QualifiedName(string? ns, string? name, bool global = false)
    {
        if (name == null) return null;
        var fqn = string.IsNullOrWhiteSpace(ns) ? name : ns + "." + name;
        return global ? "global::" + fqn : fqn;
    }
}

/// <summary>
/// Represents a class depicting an xsd element in generated code
/// </summary>
public class CElement(ClrContentTypeInfo info) : CClass
{
    public string XName => info.schemaName;
    public string XNamespace => info.schemaNs;
    public SchemaOrigin Origin => info.typeOrigin;
    public string Name => info.clrtypeName;
    public string Fqn => QualifiedName(Namespace!.Name, Name);
    public bool IsAbstract => info.IsAbstract;
    public bool IsSealed => info.IsSealed;
    public bool IsSubstitutionHead => info.IsSubstitutionHead;
    public bool IsDerived => info.IsDerived;
    public string BaseType => QualifiedName(info.baseTypeClrNs, info.baseTypeClrName, global: true) ?? "XTypedElement";    
    public bool HasSaveMethods => info.typeOrigin == SchemaOrigin.Element && !info.IsDerived;
    public bool HasLoadMethods => info.typeOrigin == SchemaOrigin.Element;
    public IEnumerable<string> Comments => info.Annotations?.Select(a => a.Text) ?? [];

    public IEnumerable<CAttribute> Content => info.Content.SelectMany(FlattenContents);

    private static IEnumerable<CAttribute> FlattenContents(ContentInfo info)
    {
        if (info.ContentType == ContentType.Property) 
            return info is ClrPropertyInfo { ShouldGenerate: true } p ? [new CAttribute(p)] : [];
        // TODO: flesh out processing of groupings, this is just to understand better the content model
        if (info.ContentType == ContentType.Grouping) return info.Children.SelectMany(FlattenContents);
        // TODO: wildcard
        return [];
    }
}

/// <summary>
/// Represents a class depicting an xsd simple type wrapper in generated code
/// </summary>
public class CSimpleType(
    ClrSimpleTypeInfo info,
    Dictionary<XmlSchemaObject, string> nameMappings,
    LinqToXsdSettings settings)
    : CClass
{
    public override bool IsSimpleType => true;

    public bool IsGlobal => info.IsGlobal;

    // For enums, the clrtypeName is used for `enum` declaration, 
    // so suffix "Validator" is added to the simple type class holding the `TypeDefinition`.
    public string Name { get; } = info.clrtypeName + (info is EnumSimpleTypeInfo ? "Validator" : "");

    public string FullyQualifiedName { get; } = info.FullyQualifiedName(nameMappings, settings);

    public XmlSchemaDatatypeVariety Variety => info.Variety;
    public XmlTypeCode XmlTypeCode => info.TypeCode;

    public CompiledFacets Restrictions => info.RestrictionFacets;
    
    public IEnumerable<string> Comments => info.Annotations?.Select(a => a.Text) ?? [];

    public bool IsEnum => info is EnumSimpleTypeInfo;
    public string EnumName => info.clrtypeName;
    public IEnumerable<string> EnumValues 
        => ((EnumSimpleTypeInfo)info).InnerType
            .GetEnumFacets()
            .Select(x => x.Member);

    public bool IsList => info is ListSimpleTypeInfo;
    public CSimpleType ListItemType => new CSimpleType(
        ((ListSimpleTypeInfo)info).ItemType,
        nameMappings,
        settings);

    public bool IsUnion => info is UnionSimpleTypeInfo;
    public IEnumerable<CSimpleType> UnionTypes 
        => ((UnionSimpleTypeInfo)info).MemberTypes
            .Select(x => new CSimpleType(x, nameMappings, settings));
}