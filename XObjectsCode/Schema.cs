#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.CodeGen;

public partial class Schema
{
    /// <summary>
    /// If this Schema links or imports others (has dependencies), this will return a flat list of those linked Schema objects.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Schema> GetDependencies()
    {
        if (this.Untyped.Parent is null) throw new InvalidOperationException("This method only works when Schema is already part of a Graph.");
        
        // since this Schema represents a <Schema> XML element under the <Graph> XML element,
        // we can conveniently navigate to the parent by casting to the right type!
        Graph graph = (Graph)this.Untyped.Parent;

        if (Includes?.Schema != null && Includes.Schema.Any())
        {
            foreach (Schema include in Includes.Schema)
            {
                var includeName = System.IO.Path.GetFileName(include.Name);
                var schemaByNameFromGraphRoot = graph.Schema.FirstOrDefault(s => s.Name.EqualsIgnoreCase(includeName));
                if (schemaByNameFromGraphRoot != null)
                    yield return schemaByNameFromGraphRoot;
            }
        }

        if (Imports?.Schema != null && Imports.Schema.Any())
        {
            foreach (Schema import in Imports.Schema)
            {
                var schemaByNameFromGraphRoot = graph.Schema.Single(s => s.Name.EqualsIgnoreCase(import.Name));
                yield return schemaByNameFromGraphRoot;
            }
        }
    }

    /// <summary>
    /// Returns the complete list of dependencies for the current Schema, the direct and indirect dependencies. 
    /// </summary>
    /// <param name="skipList"></param>
    /// <returns></returns>
    public List<Schema> GetDependenciesRecursively(List<Schema>? skipList = null)
    {
        skipList ??= new List<Schema>();

        var dependencies = GetDependencies();

        foreach (Schema dependency in dependencies)
        {
            if (skipList.Contains(dependency))
            {
                continue;
            }

            skipList.Add(dependency);
            dependency.GetDependenciesRecursively(skipList);
        }

        return skipList;
    }

    private List<string>? _importedByList = null;
    internal List<string> ImportedByList
    {
        get
        {
            if (_importedByList == null)
            {
                if (string.IsNullOrWhiteSpace(ImportedBy))
                    _importedByList = new List<string>();
                else
                    _importedByList = ImportedBy.Split([';'], StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return _importedByList;
        }

        set
        {
            _importedByList = value;
            if (value != null && value.Any())
            {
                ImportedBy = string.Join(";", value);
            }
            else
            {
                ImportedBy = null;
            }
        }
    }
    
    private List<string>? _includedBy = null;
    internal List<string> IncludedByList
    {
        get
        {
            return _includedBy ??= this.IncludedBy?.Split([';'], StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>();
        }

        set
        {
            _includedBy = value;
            if (value.Any())
            {
                IncludedBy = string.Join(";", value);
            }
        }
    }
}