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
                var schemaByNameFromGraphRoot = graph.Schema.Single(s => s.Name.EqualsIgnoreCase(include.Name));
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
    public List<Schema> GetDependenciesRecursively(List<Schema> skipList = null)
    {
        Graph graph = (Graph)this.Untyped.Parent;

        var dependencies = GetDependencies();

        var returnList = new List<Schema>();
        foreach (Schema dependency in dependencies)
        {
            if (returnList.Contains(dependency))
            {
                continue;
            }

            returnList.Add(dependency);

            var countOfSkips = 0;
            foreach (Schema recursiveDependency in dependency.GetDependenciesRecursively(returnList))
            {
                if (returnList.Contains(recursiveDependency))
                {
                    countOfSkips++;
                    continue;
                }

                returnList.Add(recursiveDependency);
            }
        }

        return returnList;
    }
}