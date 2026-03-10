#nullable enable

namespace Xml.Schema.Linq.Codegen.Model;

/// <summary>
/// Represents a class depicting an xsd element in generated code
/// </summary>
public class CElement
{
    /// <summary>Fully qualified C# generated class name </summary>
    public required  string Name { get; init; }
    /// <summary>Element namespace (from xsd)</summary>
    public required string XsdNs { get; init; }
    /// <summary>Element name (from xsd)</summary>
    public required string XsdName { get; init; }
}