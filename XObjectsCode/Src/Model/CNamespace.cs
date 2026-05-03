#nullable enable

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace Xml.Schema.Linq.CodeGen.Model;

/// <summary>
/// Represents a namespace in generated code, holding types.
/// </summary>
/// <param name="name">The CLR namespace name</param>
public class CNamespace(string name) : IHasTypes
{
    // TODO: this is temporary to perform migration step-by-step. Must be removed.
    public required CodeNamespace Dom { get; init; }
    
    public string Name => name;

    public required string AccessModifier { get; init; }

    // List of root elements in this namespace (also found in Types and Elements)
    public List<CElement> Roots { get; } = [];

    public List<CClass> Types { get; } = [];

    public void Add(CClass type)
    {
        Types.Add(type);
        type.Namespace = this;
    }

    public IEnumerable<CElement> Elements => Types.OfType<CElement>();
}