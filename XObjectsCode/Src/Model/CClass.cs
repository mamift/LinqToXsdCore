#nullable enable

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

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
public class CSimpleType(ClrSimpleTypeInfo info) : CClass
{
    public override bool IsSimpleType => true;

    public string Name => info.clrtypeName;
    
    public IEnumerable<string> Comments => info.Annotations?.Select(a => a.Text) ?? [];
}