using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Xml.Schema;
using Microsoft.VisualBasic;

namespace Xml.Schema.Linq.CodeGen.Model;

/// <summary>
/// Represents properties of an CElement: attributes or grouping of tag content
/// </summary>
public abstract class CContent(ContentInfo info)
{
    public ContentType ContentType => info.ContentType;
}

/// <summary>
/// Represents a property depicting an element attribute in generated code
/// </summary>
public class CAttribute(ClrPropertyInfo info) : CContent(info)
{
    public string XNamespace => info.PropertyNs;
    public string XName => info.SchemaName;
    public string XNameField { get; } = info.PropertyName + "XName";
    public string XTypeCode => info.TypeReference.TypeCodeString;
    public SchemaOrigin Origin => info.Origin;
    public string Name => info.PropertyName;
    public string Type => info.ReturnTypeStr;
    public string NullableType => info.NullableType; // One more type... this differs at least for lists where this is the element type
    public string ClrType => info.ClrTypeName;  // TODO: clarify naming and usage, seems to be the underlying type (e.g., without nullable)
    public string ClrFullTypeName => info.TypeReference.ClrFullTypeName; // Yet another type, seems to be used for enums
    public string SimpleTypeDefinition => info.GetSimpleTypeDefinition(disambiguateProperty: true);
    public string LocalSimpleTypeDefinition => info.GetSimpleTypeDefinition(disambiguateProperty: false);
    public bool IsLocalElement => info.IsLocalElement;
    public bool IsNew => info.IsNew;
    public bool IsOverride => info.IsOverride;
    public bool HasSet => info.HasSet;    
    public bool IsRef => info.IsRef;
    public bool IsValueType => info.TypeReference.IsValueType;
    public bool IsSimpleType => info.TypeReference.IsSimpleType;
    public bool IsEnum => info.IsEnum;
    public bool IsUnion => info.IsUnion;
    public bool IsList => info.IsList;
    public bool IsSchemaList => info.IsSchemaList;
    public bool HasValidation => Origin == SchemaOrigin.Attribute ? IsEnum : info.Validation;
    public bool IsNullable => info.IsNullable;      // Whether the C# type is nullable (both reference and value types)
    public bool IsNillable => info.IsNillable;      // Whether xs:nil is an acceptable value (on elements)
    public bool IsOptional => info.IsOptional;      // Whether element/attribute cardinality can be 0
    public bool CanBeAbsent => info.CanBeAbsent;    // Whether element/attribute is optional, or element is part of a choice
    public bool VerifyRequired => info.VerifyRequired;  // Whether property should throw when attempting to read a missing required element or attribute
    public IEnumerable<string> Comments => info.Annotations.Select(x => x.Text);
    
    public bool IsSubstitution => info.IsSubstitutionHead;
    public List<XmlSchemaElement> SubstitutionMembers => info.SubstitutionMembers;

    public string FixedValue => info.FixedValue;
    public string DefaultValue => info.DefaultValue;
    public string FixedOrDefaultValue => info.FixedValue ?? info.DefaultValue;
    public string FixedOrDefaultBaseType => info.FixedOrDefaultBaseType;
    public bool IsFixedOrDefaultList => info.IsFixedOrDefaultList;
    public string FixedOrDefaultField { get; } = info switch 
    {
        { FixedValue: not null } => info.PropertyName + "FixedValue",
        { DefaultValue: not null } => info.PropertyName + "DefaultValue",
        _ => null,
    };
}

/// <summary>
/// Represents a property depicting an element content in generated code
/// </summary>
public class CGrouping(ContentInfo info) : CContent(info)
{}