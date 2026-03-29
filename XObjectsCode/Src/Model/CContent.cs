using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public string Name => info.PropertyName;
    public string Type => info.ReturnTypeStr;
    public bool IsNew => info.IsNew;
    public bool IsEnum => info.IsEnum;
    public IEnumerable<string> Comments => info.Annotations.Select(x => x.Text);
    
    public bool IsOptional => info.IsOptional;
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