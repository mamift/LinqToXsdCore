#nullable enable

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq.CodeGen.Model;

/// <summary>
/// Represents a class in generated code, specialized into various kind of xsd source: simple type, element.
/// </summary>
public abstract class CClass
{
    /// <summary>Parent C# namespace</summary>
    public CNamespace? Namespace { get; set; }    

    // This isn't great OOP design but Scriban doesn't have first-class support for OOP (no `is` operator)
    public virtual bool IsSimpleType => false;
}

/// <summary>
/// Represents a class depicting an xsd element in generated code
/// </summary>
public class CElement : CClass
{
    // TODO: Temporary, remove after full migration
    public required CodeTypeDeclaration Dom { get; init; }

    /// <summary>Fully qualified C# generated class name</summary>
    public required string Name { get; init; }

    /// <summary>Tag namespace (from xsd)</summary>
    public required string XsdNs { get; init; }

    /// <summary>Tag name (from xsd)</summary>
    public required string XsdName { get; init; }
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