#nullable enable

using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xml.Schema.Linq.CodeGen.Model;

/// <summary>
/// Represents a namespace in generated code, holding types.
/// </summary>
public class CNamespace(string name)
{
    // TODO: this is temporary to perform migration step-by-step. Must be removed.
    public required CodeNamespace Dom { get; init; }
    
    public string Name => name;

    public IEnumerable<CodeTypeDeclaration> Types => Dom.Types
        .Cast<CodeTypeDeclaration>()
        .Where(x => x.Name is not ("XRootNamespace" or "XRoot" or "LinqToXsdTypeManager"));

    public CodeTypeDeclaration RootType => Dom.Types[0];

    public IEnumerable<CodeTypeDeclaration> Elements => Types.Where(x => !x.TypeAttributes.HasFlag(TypeAttributes.Sealed));
}