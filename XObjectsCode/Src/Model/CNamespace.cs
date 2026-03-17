#nullable enable

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xml.Schema.Linq.CodeGen.Scriban;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.CodeGen.Model;

/// <summary>
/// Represents a namespace in generated code, holding types.
/// </summary>
/// <param name="name">The CLR namespace name</param>
public class CNamespace(string name)
{
    // TODO: this is temporary to perform migration step-by-step. Must be removed.
    public required CodeNamespace Dom { get; init; }
    
    public string Name => name;

    public required string AccessModifier { get; init; }

    // List of root elements in this namespace (also found in Types and Elements)
    public List<CodeTypeDeclaration> Roots { get; } = [];

    public List<CClass> Types { get; } = [];

    public IEnumerable<CElement> Elements => Types.OfType<CElement>();

    public void Add(CodeTypeDeclaration type)
    {
        // TODO: should be passed instead of CodeTypeDeclaration
        CClass ctype = new CElement 
        { 
            Dom = type,
            Name = type.Name, 
            XsdName = ScribanGlobals.LocalName(type, "xName"),
            XsdNs = ScribanGlobals.Namespace(type, "xName"),
        };

        ctype.Namespace = this;
        Types.Add(ctype);

        // TODO: temporary, remove after full migration
        Dom.Types.Add(type);
        type.SetParent(Dom);
    }

    public void Add(CClass type)
    {
        type.Namespace = this;
        Types.Add(type);
    }
}