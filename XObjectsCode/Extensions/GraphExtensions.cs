#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.CodeGen;

public static class GraphExtensions
{
    /// <summary>
    /// Traverses a list of schemas and then compares against the given <paramref name="graph"/> that they are indeed entry point schemas.
    /// <para>Returns the flat list of schemas that are reachable from the entry point schemas.</para>
    /// </summary>
    /// <param name="schemas"></param>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static List<Schema> TraverseSchemas(this IEnumerable<Schema> schemas, Graph graph)
    {
        if (schemas == null) throw new ArgumentNullException(nameof(schemas));
        if (graph == null) throw new ArgumentNullException(nameof(graph));

        if (graph.Schema == null || !graph.Schema.Any()) return new List<Schema>();

        var entryPointSchemas = graph.FindEntryPointSchemas();
        if (!entryPointSchemas.Any()) return new List<Schema>();

        var result = new List<Schema>();
        var visitedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<Schema>();

        foreach (var schema in schemas)
        {
            if (schema == null || string.IsNullOrWhiteSpace(schema.Name))
                continue;

            string schemaFileName = Path.GetFileName(schema.Name);

            var matchedEntryPoint = entryPointSchemas.FirstOrDefault(ep =>
                ep == schema ||
                ep.Name.EqualsIgnoreCase(schema.Name) ||
                (!string.IsNullOrWhiteSpace(ep.Name) && Path.GetFileName(ep.Name).EqualsIgnoreCase(schemaFileName)));

            if (matchedEntryPoint != null)
            {
                string epFileName = Path.GetFileName(matchedEntryPoint.Name);
                if (visitedNames.Add(epFileName))
                {
                    result.Add(matchedEntryPoint);
                    queue.Enqueue(matchedEntryPoint);
                }
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Includes?.Schema != null)
            {
                foreach (var inc in current.Includes.Schema)
                {
                    if (string.IsNullOrWhiteSpace(inc?.Name)) continue;
                    var target = FindSchemaInGraph(graph, inc.Name);
                    if (target != null && !string.IsNullOrWhiteSpace(target.Name))
                    {
                        string targetFileName = Path.GetFileName(target.Name);
                        if (visitedNames.Add(targetFileName))
                        {
                            result.Add(target);
                            queue.Enqueue(target);
                        }
                    }
                }
            }

            if (current.Imports?.Schema != null)
            {
                foreach (var imp in current.Imports.Schema)
                {
                    if (string.IsNullOrWhiteSpace(imp?.Name)) continue;
                    var target = FindSchemaInGraph(graph, imp.Name);
                    if (target != null && !string.IsNullOrWhiteSpace(target.Name))
                    {
                        string targetFileName = Path.GetFileName(target.Name);
                        if (visitedNames.Add(targetFileName))
                        {
                            result.Add(target);
                            queue.Enqueue(target);
                        }
                    }
                }
            }
        }

        return result;
    }

    private static Schema? FindSchemaInGraph(Graph graph, string schemaName)
    {
        string fileName = Path.GetFileName(schemaName);
        return graph.Schema.FirstOrDefault(s =>
            s.Name.EqualsIgnoreCase(schemaName) ||
            (!string.IsNullOrWhiteSpace(s.Name) && Path.GetFileName(s.Name).EqualsIgnoreCase(fileName)));
    }
}