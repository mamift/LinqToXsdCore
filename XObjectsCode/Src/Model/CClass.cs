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
    // Initially null until added to a namespace during processing.
    // Remains null for nested types.
    public CNamespace? Namespace { get; set; }

    public abstract string Name { get; }

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
public class CElement(ClrContentTypeInfo info) : CClass, IHasTypes
{
    public string XName => info.schemaName;
    public string XNamespace => info.schemaNs;
    public SchemaOrigin Origin => info.typeOrigin;
    public override string Name => info.clrtypeName;
    public string Fqn => QualifiedName(Namespace!.Name, Name);
    public bool IsAbstract => info.IsAbstract;
    public bool IsSealed => info.IsSealed;
    public bool IsSubstitutionHead => info.IsSubstitutionHead;
    public bool IsDerived => info.IsDerived;
    public string BaseType => QualifiedName(info.baseTypeClrNs, info.baseTypeClrName, global: true) ?? "XTypedElement";
    public bool HasSaveMethods => info.typeOrigin == SchemaOrigin.Element && !info.IsDerived;
    public bool HasLoadMethods => info.typeOrigin == SchemaOrigin.Element;
    public IEnumerable<string> Comments => info.Annotations?.Select(a => a.Text) ?? [];
    public IEnumerable<string> RegexComments 
        => info.Annotations?
            .Where(a => a.Section == "summaryRegEx")
            .Select(a => a.Text) 
            ?? [];

    public IEnumerable<CAttribute> Content => info.Content.SelectMany(FlattenContents).Where(x => x.ShouldGenerate);
    public bool HasGroups => info.Content.Any(x => x.ContentType == ContentType.Grouping);
    public ContentGroup? Group => info.Content
        .Where(x => x.ContentType == ContentType.Grouping)
        .Select(x => new ContentGroup(x))
        .FirstOrDefault();

    public IEnumerable<CAttribute> LocalElements => info.Content.SelectMany(FlattenContents).Where(x => x.IsLocalElement);

    public List<CClass> Types { get; } = [];

    public void Add(CClass type) => Types.Add(type);

    private static IEnumerable<CAttribute> FlattenContents(ContentInfo info)
    {
        if (info.ContentType == ContentType.Property) 
            return [new CAttribute((ClrPropertyInfo)info)];
        if (info.ContentType == ContentType.Grouping) 
            return info.Children.SelectMany(FlattenContents);
        // TODO: wildcard
        return [];
    }

    public class ContentGroup(ContentInfo info)
    {
        public string? Type => info switch {
            GroupingInfo { ContentModelType: ContentModelType.Sequence } => "Sequence",
            GroupingInfo { ContentModelType: ContentModelType.Choice } => "Choice",
            ClrPropertyInfo { IsSubstitutionHead: true } => "Substituted",
            ClrPropertyInfo => "Named",
            _ => null,
        };

        public string? XNameField => info is ClrPropertyInfo p ? p.PropertyName + "XName" : null;

        public IEnumerable<XmlSchemaElement> SubstitutionMembers => info is ClrPropertyInfo p ? p.SubstitutionMembers : [];

        public IEnumerable<ContentGroup> Children => info is GroupingInfo g 
            ? g.Children.Select(x => new ContentGroup(x)) 
            : [];
    }
}

public class CElementWrapper(ClrWrapperTypeInfo info, CClass wrapped) : CClass
{
    public string XName => info.schemaName;
    public string XNamespace => info.schemaNs;
    public override string Name => info.clrtypeName;
    public string Fqn => QualifiedName(Namespace!.Name, Name);
    public string WrappedName => wrapped.Name;
    public bool IsDerived => info.IsDerived;    
    public string BaseType => QualifiedName(info.baseTypeClrNs, info.baseTypeClrName, global: true) ?? "XTypedElement";
    public bool HasSaveMethods => info.typeOrigin == SchemaOrigin.Element && !info.IsDerived;
    public IEnumerable<string> Comments => info.Annotations?.Select(a => a.Text) ?? [];
    public IEnumerable<string> RegexComments => []; // TODO where from ?
    public IEnumerable<CAttribute> Content => wrapped is CElement e ? e.Content : [];
    public bool IsSubstitutionHead => info.IsSubstitutionHead;
    public bool IsSubstitutionMember => info.IsSubstitutionMember();
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
    public override string Name { get; } = info.clrtypeName + (info is EnumSimpleTypeInfo ? "Validator" : "");

    public string FullyQualifiedName { get; } = info.FullyQualifiedName(nameMappings, settings);

    public XmlSchemaDatatypeVariety Variety => info.Variety;
    public XmlTypeCode XmlTypeCode => info.TypeCode;

    public CompiledFacets Restrictions => info.RestrictionFacets;
    
    public IEnumerable<string> Comments 
        => Namespace is null 
        ? []    // no comments on nested simple types
        : info.Annotations?.Select(a => a.Text) ?? [];

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

public class CSimpleTypeWrapper(ClrWrapperTypeInfo info, ClrPropertyInfo propertyInfo) : CClass
{
    public override bool IsSimpleType => true;

    public string XName => info.schemaName;
    public string XNamespace => info.schemaNs;
    public override string Name => info.clrtypeName;
    public string Fqn => QualifiedName(Namespace!.Name, Name);
    public bool HasSaveMethods => info.typeOrigin == SchemaOrigin.Element && !info.IsDerived;
    public bool IsDerived => info.IsDerived;    
    public string BaseType => QualifiedName(info.baseTypeClrNs, info.baseTypeClrName, global: true) ?? "XTypedElement";
    public string WrappedName => info.InnerType.IsSchemaList 
        ? $"IList<{propertyInfo.ClrTypeName}>" 
        : propertyInfo.ClrTypeName;
    public bool NeedsEnumParse => info.InnerType.IsEnum && !info.InnerType.IsEquivalentTo(info.clrtypeName);
    public string EnumType => info.InnerType.ClrFullTypeName;
    public CAttribute TypedValueProperty => new CAttribute(propertyInfo);
    public bool HasWildcard => info.HasElementWildCard; // Unused?    
    // public XmlTypeCode XmlTypeCode => info.InnerType.TypeCode;
    public IEnumerable<string> Comments => info.Annotations?.Select(a => a.Text) ?? [];
}