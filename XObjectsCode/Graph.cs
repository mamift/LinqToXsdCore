#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq.CodeGen;

/// <summary>
/// Represents a graph of XSD files in a folder (including subfolders).
/// </summary>
public partial class Graph
{
    public IFileSystem? FileSystem { get; set; }

    /// <summary>
    /// Given a folder path, build a <see cref="Graph"/> from all XSD files in the folder and subfolder.
    /// This graph tracks which XSD files import/include each other in the folder and can be used to determine dependencies between XSD files.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="searchOption"></param>
    /// <param name="skipSchemasWithNoImportsOrIncludes"></param>
    /// <returns></returns>
    public static Graph BuildFromFolder(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly,
        bool skipSchemasWithNoImportsOrIncludes = false)
    {
        DirectoryInfo dir = new DirectoryInfo(directory);
        return BuildFromFolder(dir, searchOption, skipSchemasWithNoImportsOrIncludes);
    }

    public static Graph BuildFromFolder(DirectoryInfo dir, SearchOption searchOption = SearchOption.TopDirectoryOnly,
        bool skipSchemasWithNoImportsOrIncludes = false)
    {
        FileInfo[] files = dir.GetFiles("*.xsd", searchOption);
        var graph = new Graph() {
            Folder = dir.FullName
        };

        foreach (FileInfo file in files)
        {
            using FileStream fileStream = file.OpenRead();
            XDocument xDocument = XDocument.Load(fileStream);
            if (!xDocument.IsAnXmlSchema()) continue;

            List<XElement> includes = xDocument.GetXsdIncludeElements().ToList();
            List<XElement> imports = xDocument.GetXsdImportElements().ToList();

            var schemaEl = new Schema() {
                Name = file.Name,
                RelativePath = file.FullName.Replace(dir.FullName, "."),
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

        PopulateIncludedByAndImportedBy(graph);

        return graph;
    }

    public static Graph BuildFromFiles(IEnumerable<string> filePaths, IFileSystem? fs = null)
    {
        if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));

        var paths = filePaths.ToList();
        if (!paths.Any()) return new Graph();

        var graph = new Graph();

        // Gather file infos either from the provided IFileSystem or the real System.IO
        var fileInfos = new List<(string FullName, string Name, Func<Stream> OpenStream)>();

        if (fs == null)
        {
            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                {
                    var dir = new DirectoryInfo(p);
                    foreach (var f in dir.GetFiles("*.xsd", SearchOption.AllDirectories))
                    {
                        fileInfos.Add((f.FullName, f.Name, () => f.OpenRead()));
                    }
                }
                else if (File.Exists(p))
                {
                    var f = new FileInfo(p);
                    fileInfos.Add((f.FullName, f.Name, () => f.OpenRead()));
                }
            }
        }
        else
        {
            graph.FileSystem = fs;
            foreach (var p in paths)
            {
                var dirInfo = fs.DirectoryInfo.New(p);
                if (dirInfo.Exists)
                {
                    foreach (var f in dirInfo.GetFiles("*.xsd", SearchOption.AllDirectories))
                    {
                        // Capture local variable for closure
                        var fLocal = f;
                        fileInfos.Add((fLocal.FullName, fLocal.Name, () => fLocal.OpenRead()));
                    }
                }
                else
                {
                    var fileInfo = fs.FileInfo.New(p);
                    if (fileInfo.Exists)
                    {
                        var fiLocal = fileInfo;
                        fileInfos.Add((fiLocal.FullName, fiLocal.Name, () => fiLocal.OpenRead()));
                    }
                }
            }
        }

        foreach (var fi in fileInfos)
        {
            using var stream = fi.OpenStream();
            XDocument xDocument = XDocument.Load(stream);
            if (!xDocument.IsAnXmlSchema()) continue;

            List<XElement> includes = xDocument.GetXsdIncludeElements().ToList();
            List<XElement> imports = xDocument.GetXsdImportElements().ToList();

            var schemaEl = new Schema() {
                Name = fi.Name,
                RelativePath = fi.FullName
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

            graph.Schema.Add(schemaEl);
        }

        PopulateIncludedByAndImportedBy(graph);

        return graph;
    }

    /// <summary>
    /// Returns all unique schema names in the graph.
    /// </summary>
    /// <returns>A list of distinct schema names.</returns>
    public List<string> GetAllSchemaNames()
    {
        return Schema.Select(s => {
            if (string.IsNullOrWhiteSpace(s.Name))
                throw new InvalidOperationException("Graph is in an invalid state: a schema name was null or empty!");

            return s.Name;
        }).ToList();
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
        var includedNames = Schema
            .Where(s => s.Includes?.Schema is { Count: > 0 })
            .SelectMany(s => s.Includes.Schema)
            .Select(i => Path.GetFileName(i.Name))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Schema.Where(sc => !string.IsNullOrWhiteSpace(sc.Name) &&
                                  (includedNames.Contains(sc.Name) || includedNames.Contains(Path.GetFileName(sc.Name)))).ToList();
    }

    public List<Schema> GetSchemasThatAreImportedByOthers()
    {
        var importedNames = Schema
            .Where(s => s.Imports?.Schema is { Count: > 0 })
            .SelectMany(s => s.Imports.Schema)
            .Select(i => Path.GetFileName(i.Name))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Schema.Where(sc => !string.IsNullOrWhiteSpace(sc.Name) &&
                                  (importedNames.Contains(sc.Name) || importedNames.Contains(Path.GetFileName(sc.Name)))).ToList();
    }

    /// <summary>
    /// Returns all schemas that are part of the connected graph (i.e. schemas that import/include other schemas, or are imported/included by other schemas).
    /// </summary>
    /// <returns>A list of schemas that have at least one incoming or outgoing dependency connection.</returns>
    public List<Schema> GetConnectedSchemas()
    {
        if (Schema.Count == 0)
            return new List<Schema>();

        var allReferencedNames = Schema
            .SelectMany(EnumerateDependencies)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Schema.Where(s =>
        {
            if (EnumerateDependencies(s).Any())
                return true;

            if (!string.IsNullOrWhiteSpace(s.Name))
            {
                string fileName = Path.GetFileName(s.Name);
                if (allReferencedNames.Contains(s.Name) || allReferencedNames.Contains(fileName))
                    return true;
            }

            return false;
        }).ToList();
    }

    /// <summary>
    /// Returns the distinct names of all schemas that are part of the connected graph (i.e. schemas that import/include other schemas, or are imported/included by other schemas).
    /// </summary>
    /// <returns>A list of schema names that have dependency connections.</returns>
    public List<string> GetConnectedSchemaNames()
    {
        return GetConnectedSchemas()
            .Select(s => s.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns all schemas that are isolated/disconnected (i.e. schemas that neither import/include other schemas, nor are imported/included by other schemas).
    /// </summary>
    /// <returns>A list of standalone schemas with no dependency connections.</returns>
    public List<Schema> GetDisconnectedSchemas()
    {
        var connected = GetConnectedSchemas().ToHashSet();
        return Schema.Where(s => !connected.Contains(s)).ToList();
    }

    /// <summary>
    /// Returns the distinct names of all schemas that are isolated/disconnected.
    /// </summary>
    /// <returns>A list of standalone schema names with no dependency connections.</returns>
    public List<string> GetDisconnectedSchemaNames()
    {
        return GetDisconnectedSchemas()
            .Select(s => s.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Using the output from <see cref="GetEntryPointSchemas"/>, loads them all into their own <see cref="XmlSchemaSet"/> or sets.
    /// <para>Each entrypoint schema represents it's own <see cref="XmlSchemaSet"/>; this means some schemas in a graph that are 'standalone' (not included/imported by others and not including/importing others),
    /// will be loaded into their own <see cref="XmlSchemaSet"/>.</para>
    /// <para>Any schema linked (i.e. imported/included) will be loaded into the <see cref="XmlSchemaSet"/> of the schema that imports/includes it.</para>
    /// </summary>
    /// <returns></returns>
    public List<XmlSchemaSet> GetXmlSchemaSetFromEntryPointSchemas()
    {
        var entryPointSchemas = GetEntryPointSchemas();

        throw new NotImplementedException();
    }

    public List<Schema> GetEntryPointSchemas()
    {
        List<string> entryPointNames = GetEntryPointSchemaNames();

        return this.SchemaField
            .Where(sc => entryPointNames.Any(e => e.EqualsIgnoreCase(sc.Name)))
            .ToList();
    }

    /// <summary>
    /// Gets all schemas that include and import others, but are themselves NOT included nor imported by others.
    /// </summary>
    /// <returns></returns>
    public List<Schema> GetRootSchemas()
    {
        var schemasWithOutgoing = GetSchemasThatImportsOrIncludeOthers();
        if (schemasWithOutgoing.Count == 0)
            return new List<Schema>();

        var included = GetSchemasThatAreIncludedByOthers().ToHashSet();
        var imported = GetSchemasThatAreImportedByOthers().ToHashSet();

        return schemasWithOutgoing
            .Where(s => !included.Contains(s) && !imported.Contains(s))
            .ToList();
    }

    /// <summary>
    /// Finds the minimum set of graph schema entry points that should be added to an <see cref="System.Xml.Schema.XmlSchemaSet"/>.
    /// One schema representative is selected from each source strongly connected component in the include/import graph.
    /// </summary>
    /// <returns>Schema file names suitable as entry points.</returns>
    public List<string> GetEntryPointSchemaNames()
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

    private static void PopulateIncludedByAndImportedBy(Graph graph)
    {
        if (graph.Schema == null || graph.Schema.Count == 0)
            return;

        var includedByMap = new Dictionary<Schema, List<string>>();
        var importedByMap = new Dictionary<Schema, List<string>>();

        foreach (Schema source in graph.Schema)
        {
            if (string.IsNullOrWhiteSpace(source.Name)) continue;

            if (source.Includes?.Schema != null)
            {
                foreach (Schema inc in source.Includes.Schema)
                {
                    if (string.IsNullOrWhiteSpace(inc.Name)) continue;
                    string incFileName = Path.GetFileName(inc.Name);

                    foreach (Schema target in graph.Schema)
                    {
                        if (string.IsNullOrWhiteSpace(target.Name)) continue;
                        if (target.Name.EqualsIgnoreCase(inc.Name) ||
                            target.Name.EqualsIgnoreCase(incFileName) ||
                            Path.GetFileName(target.Name).EqualsIgnoreCase(incFileName))
                        {
                            if (!includedByMap.TryGetValue(target, out var list))
                            {
                                list = new List<string>();
                                includedByMap[target] = list;
                            }
                            if (!list.Contains(source.Name, StringComparer.OrdinalIgnoreCase))
                            {
                                list.Add(source.Name);
                            }
                        }
                    }
                }
            }

            if (source.Imports?.Schema != null)
            {
                foreach (Schema imp in source.Imports.Schema)
                {
                    if (string.IsNullOrWhiteSpace(imp.Name)) continue;
                    string impFileName = Path.GetFileName(imp.Name);

                    foreach (Schema target in graph.Schema)
                    {
                        if (string.IsNullOrWhiteSpace(target.Name)) continue;
                        if (target.Name.EqualsIgnoreCase(imp.Name) ||
                            target.Name.EqualsIgnoreCase(impFileName) ||
                            Path.GetFileName(target.Name).EqualsIgnoreCase(impFileName))
                        {
                            if (!importedByMap.TryGetValue(target, out var list))
                            {
                                list = new List<string>();
                                importedByMap[target] = list;
                            }
                            if (!list.Contains(source.Name, StringComparer.OrdinalIgnoreCase))
                            {
                                list.Add(source.Name);
                            }
                        }
                    }
                }
            }
        }

        foreach (var kvp in includedByMap)
        {
            if (kvp.Value.Count > 0)
            {
                kvp.Key.IncludedBy = string.Join(";", kvp.Value);
            }
        }

        foreach (var kvp in importedByMap)
        {
            if (kvp.Value.Count > 0)
            {
                kvp.Key.ImportedBy = string.Join(";", kvp.Value);
            }
        }
    }

    /// <summary>
    /// After a <see cref="Graph"/> instance has been built , this method will return a list of <see cref="IFileInfo"/> objects representing the XSD files in the graph.
    /// <para>If <see cref="FileSystem"/> is not null, it will use paths provided by that instead.</para>
    /// </summary>
    /// <returns></returns>
    internal List<IFileInfo>? ToFileInfos()
    {
        if (!Schema.Any())
            return null;

        var fs = FileSystem ?? new FileSystem();

        var result = new List<IFileInfo>();

        foreach (Schema schema in Schema)
        {
            if (string.IsNullOrWhiteSpace(schema.RelativePath))
                continue;

            string path = schema.RelativePath;

            // RelativePath from BuildFromFolder starts with '.' (e.g. "./foo.xsd")
            // Resolve against Folder when available
            if (!string.IsNullOrWhiteSpace(Folder) && path.StartsWith("."))
            {
                string pathStr = path.Substring(1)
                    .TrimStart(fs.Path.DirectorySeparatorChar, fs.Path.AltDirectorySeparatorChar);
                string combinedPath = fs.Path.Combine(Folder, pathStr);
                path = fs.Path.GetFullPath(combinedPath);
            }

            result.Add(fs.FileInfo.New(path));
        }

        return result;
    }
}