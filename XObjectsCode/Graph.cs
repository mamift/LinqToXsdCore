using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.CodeGen;

public partial class Graph
{
    /// <summary>
    /// Given a folder path, build a <see cref="Graph"/> from all XSD files in the folder and subfolder.
    /// This graph tracks which XSD files import/include each other in the folder and can be used to determine dependencies between XSD files.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="searchOption"></param>
    /// <param name="skipSchemasWithNoImportsOrIncludes"></param>
    /// <returns></returns>
    public static Graph BuildFromFolder(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly, bool skipSchemasWithNoImportsOrIncludes = false)
    {
        DirectoryInfo dir = new DirectoryInfo(directory);
        FileInfo[] files = dir.GetFiles("*.xsd", searchOption);
        var graph = new Graph();
        
        foreach (FileInfo file in files)
        {
            using FileStream fileStream = file.OpenRead();
            XDocument xDocument = XDocument.Load(fileStream);
            if (!xDocument.IsAnXmlSchema()) continue;

            List<XElement> includes = xDocument.GetIncludeElements().ToList();
            List<XElement> imports = xDocument.GetImportElements().ToList();

            var schemaEl = new Schema() {
                Name = file.Name,
            };

            if (includes.Any())
            {
                schemaEl.Includes = new Schema.IncludesLocalType() {
                    Schema = includes.Select(i => new Schema() { Name = i.Attribute("schemaLocation")?.Value }).ToList()
                };
            }

            if (imports.Any())
            {
                schemaEl.Imports = new Schema.ImportsLocalType() {
                    Schema = imports.Select(i => new Schema() { Name = i.Attribute("schemaLocation")?.Value }).ToList()
                };
            }

            if (skipSchemasWithNoImportsOrIncludes && schemaEl.Includes is null && schemaEl.Imports is null)
                continue;
            
            graph.Schema.Add(schemaEl);
        }

        return graph;
    }

    public List<Schema> GetSchemasThatImportsOrIncludeOthers()
    {
        return this.Schema.Where(s => (s.Imports?.Schema != null && s.Imports.Schema.Any()) ||
                                      (s.Includes?.Schema != null && s.Includes.Schema.Any())).ToList();
    }

    public List<Schema> GetSchemasThatDoNotImportAndIncludeOthers()
    {
        return this.Schema.Where(s => (s.Imports?.Schema == null || s.Imports?.Schema?.Count == 0) &&
                                      (s.Includes?.Schema == null || s.Includes?.Schema?.Count == 0)).ToList();
    }

    public List<Schema> GetSchemasThatAreIncludedByOthers()
    {
        return (from s in Schema
            where s.Includes?.Schema is not null && s.Includes.Schema.Any()
            from i in s.Includes.Schema
            select i).Distinct().ToList();
    }

    public List<Schema> GetSchemasThatAreImportedByOthers()
    {
        IEnumerable<string> named = (from s in Schema
            where s.Imports?.Schema is not null && s.Imports.Schema.Any()
            from i in s.Imports.Schema
            select i.Name).Distinct();

        return SchemaField.Where(sc => named.Contains(sc.Name)).ToList();
    }

    public List<Schema> FindEntryPointSchemas()
    {
        var entryPointNames = FindEntryPointSchemaNames();

        return this.SchemaField
            .Where(sc => entryPointNames.Any(e => e.Equals(sc.Name, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
    }

    /// <summary>
    /// Finds the minimum set of graph schema entry points that should be added to an <see cref="System.Xml.Schema.XmlSchemaSet"/>.
    /// One schema representative is selected from each source strongly connected component in the include/import graph.
    /// </summary>
    /// <returns>Schema file names suitable as entry points.</returns>
    public List<string> FindEntryPointSchemaNames()
    {
        if (Schema.Count == 0)
            return new List<string>();

        var schemaMap = Schema
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .GroupBy(s => Path.GetFileName(s.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        if (schemaMap.Count == 0)
            return new List<string>();

        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (Schema schema in schemaMap.Values)
        {
            string current = Path.GetFileName(schema.Name);
            var neighbors = new List<string>();

            foreach (string dependency in EnumerateDependencies(schema))
            {
                if (schemaMap.ContainsKey(dependency))
                    neighbors.Add(dependency);
            }

            graph[current] = neighbors
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (graph.Count <= 1)
            return graph.Keys.ToList();

        var index = 0;
        var stack = new Stack<string>();
        var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lowLink = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var onStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sccs = new List<List<string>>();

        void StrongConnect(string node)
        {
            indices[node] = index;
            lowLink[node] = index;
            index++;
            stack.Push(node);
            onStack.Add(node);

            foreach (string neighbor in graph[node])
            {
                if (!indices.ContainsKey(neighbor))
                {
                    StrongConnect(neighbor);
                    lowLink[node] = Math.Min(lowLink[node], lowLink[neighbor]);
                }
                else if (onStack.Contains(neighbor))
                {
                    lowLink[node] = Math.Min(lowLink[node], indices[neighbor]);
                }
            }

            if (lowLink[node] != indices[node])
                return;

            var scc = new List<string>();
            string item;
            do
            {
                item = stack.Pop();
                onStack.Remove(item);
                scc.Add(item);
            } while (!string.Equals(item, node, StringComparison.OrdinalIgnoreCase));

            sccs.Add(scc);
        }

        foreach (string node in graph.Keys)
        {
            if (!indices.ContainsKey(node))
                StrongConnect(node);
        }

        var nodeToScc = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < sccs.Count; i++)
        {
            foreach (string node in sccs[i])
                nodeToScc[node] = i;
        }

        var sccInDegree = new int[sccs.Count];
        foreach (KeyValuePair<string, List<string>> kvp in graph)
        {
            int fromScc = nodeToScc[kvp.Key];
            foreach (string target in kvp.Value)
            {
                int toScc = nodeToScc[target];
                if (fromScc != toScc)
                    sccInDegree[toScc]++;
            }
        }

        var entryPoints = new List<string>();
        for (int i = 0; i < sccs.Count; i++)
        {
            if (sccInDegree[i] == 0) 
                entryPoints.Add(sccs[i][0]);
        }

        if (entryPoints.Count == 0 && sccs.Count > 0)
            entryPoints.Add(sccs[0][0]);

        return entryPoints;
    }

    private static IEnumerable<string> EnumerateDependencies(Schema schema)
    {
        if (schema.Includes?.Schema != null)
        {
            foreach (Schema include in schema.Includes.Schema)
            {
                string normalized = Path.GetFileName(include.Name);
                if (!string.IsNullOrWhiteSpace(normalized))
                    yield return normalized;
            }
        }

        if (schema.Imports?.Schema == null)
            yield break;

        foreach (Schema import in schema.Imports.Schema)
        {
            string normalized = Path.GetFileName(import.Name);
            if (!string.IsNullOrWhiteSpace(normalized))
                yield return normalized;
        }
    }
}