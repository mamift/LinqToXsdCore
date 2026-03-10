#nullable enable

namespace Xml.Schema.Linq.Codegen.Model;

/// <summary>
/// Represents a class depicting an xsd type in generated code
/// </summary>
public class CType
{
    /// <summary>Fully qualified C# generated class name </summary>
    public required string Name { get; init; }
    /// <summary>Type namespace (from xsd)</summary>
    public required string XsdNs { get; init; }
    /// <summary>Type name (from xsd)</summary>
    public required string XsdName { get; init; }
}