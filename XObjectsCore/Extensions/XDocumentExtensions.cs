using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Xml.Schema.Linq.Extensions
{
    public static class XDocumentExtensions
    {
        private const string W3CXmlSchemaNamespaceUri = "http://www.w3.org/2001/XMLSchema";
        public static readonly XName IncludeXName = XName.Get("include", W3CXmlSchemaNamespaceUri);
        public static readonly XName ImportXName = XName.Get("import", W3CXmlSchemaNamespaceUri);

        public static IEnumerable<XElement> GetXsdIncludeElements(this XDocument xDocument)
        {
            if (!xDocument.IsAnXmlSchema()) return Enumerable.Empty<XElement>();
            return xDocument.Descendants(IncludeXName);
        }

        public static IEnumerable<XElement> GetXsdImportElements(this XDocument xDocument)
        {
            if (!xDocument.IsAnXmlSchema()) return Enumerable.Empty<XElement>();
            return xDocument.Descendants(ImportXName);
        }

        /// <summary>
        /// Determines if the current <see cref="XDocument"/> is a W3C Xml Schema by checking for the presence of the
        /// W3C namespace URI in the root element.
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        public static bool IsAnXmlSchema(this XDocument xDoc)
        {
            if (xDoc?.Root == null) throw new ArgumentNullException(nameof(xDoc));

            return xDoc.Root.Name.LocalName == "schema" && xDoc.Root.Name.Namespace == W3CXmlSchemaNamespaceUri;
        }

        /// <summary>
        /// From an existing collection of <see cref="XDocument"/>s, filter out the ones that are themselves referenced in xs:include or xs:import
        /// directives from within other XML documents in the same collection. This ensures that they are not referenced twice.
        /// </summary>
        /// <remarks>This extension operates on a dictionary collection to ensure that the file name remains associated with it's relevant XDocument
        /// instance, as XDocuments do not contain any information about the file name or where the XML document was/is stored.</remarks>
        /// <param name="xDocs"></param>
        /// <returns></returns>
        [Obsolete("Use the " + nameof(FindEntryPointSchemas) + " method")]
        public static Dictionary<string, XDocument> FilterOutSchemasThatAreIncludedOrImported(this Dictionary<string, XDocument> xDocs)
        {
            List<KeyValuePair<string, XDocument>> actualSchemas = xDocs.Where(kvp => kvp.Value.IsAnXmlSchema()).ToList();
            IEnumerable<XElement> allImportReferences = actualSchemas.SelectMany(kvp => kvp.Value.Descendants(ImportXName));
            IEnumerable<XElement> allIncludeReferences = actualSchemas.SelectMany(kvp => kvp.Value.Descendants(IncludeXName));

            List<XElement> importAndIncludeElements = allIncludeReferences.Union(allImportReferences).ToList();
            XName schemaLocationXName = XName.Get("schemaLocation");

            IEnumerable<string> filesReferredToInImportAndIncludeElements = importAndIncludeElements
                                                            .SelectMany(iie => iie.Attributes(schemaLocationXName))
                                                            .Distinct(XAttributeValueEqualityComparer.Default)
                                                            .Select(attr => attr.Value);

            IEnumerable<KeyValuePair<string, XDocument>> theXDocsReferencedByImportOrInclude = from xDoc in xDocs
                                                      where filesReferredToInImportAndIncludeElements.Any(f =>
                                                          string.Equals(f, Path.GetFileName(xDoc.Key), StringComparison.InvariantCultureIgnoreCase))
                                                      select xDoc;

            return theXDocsReferencedByImportOrInclude.ToDictionary(key => key.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Builds a directed graph of import/include relationships between XSD schemas.
        /// For each schema (keyed by its file path), returns the set of schema file names it imports or includes.
        /// </summary>
        /// <param name="xDocs">Dictionary of file path → XDocument for each XSD in the set.</param>
        /// <returns>A dictionary where keys are file paths and values are the set of file paths that the key imports/includes.</returns>
        private static Dictionary<string, HashSet<string>> BuildImportGraph(this Dictionary<string, XDocument> xDocs)
        {
            var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            XName schemaLocationXName = XName.Get("schemaLocation");

            foreach (var kvp in xDocs)
            {
                if (!kvp.Value.IsAnXmlSchema()) continue;

                var imports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var importAndIncludeElements = kvp.Value.Descendants(ImportXName)
                    .Union(kvp.Value.Descendants(IncludeXName));

                foreach (var element in importAndIncludeElements)
                {
                    var schemaLocationAttr = element.Attribute(schemaLocationXName);
                    if (schemaLocationAttr == null || string.IsNullOrWhiteSpace(schemaLocationAttr.Value))
                        continue;

                    // Resolve the schemaLocation to a full path by matching against the known file names
                    var referencedFileName = Path.GetFileName(schemaLocationAttr.Value);
                    if (string.IsNullOrEmpty(referencedFileName))
                        continue;
                    var match = xDocs.Keys.FirstOrDefault(k => string.Equals(Path.GetFileName(k), referencedFileName, StringComparison.CurrentCultureIgnoreCase));
                    if (match != null)
                        imports.Add(match);
                }

                graph[kvp.Key] = imports;
            }

            return graph;
        }

        /// <summary>
        /// Finds the minimum set of entry-point schemas such that every XSD in the collection
        /// is transitively reachable from at least one entry point via xs:import/xs:include references.
        /// Uses Tarjan's algorithm to find strongly connected components (SCCs) which correctly handles
        /// cycles in the import graph, then picks one representative from each source SCC (in-degree 0
        /// in the condensed graph).
        /// </summary>
        /// <param name="xDocs">Dictionary of file path → XDocument for each XSD in the set.</param>
        /// <returns>The file paths of the minimum set of entry-point schemas.</returns>
        public static List<string> FindEntryPointSchemas(this Dictionary<string, XDocument> xDocs)
        {
            var graph = xDocs.BuildImportGraph();

            // If there's only one schema or no imports at all, all are entry points
            if (graph.Count <= 1)
                return graph.Keys.ToList();

            // Tarjan's SCC algorithm
            var index = 0;
            var stack = new Stack<string>();
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lowLink = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var onStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sccs = new List<List<string>>();

            void StrongConnect(string v)
            {
                indices[v] = index;
                lowLink[v] = index;
                index++;
                stack.Push(v);
                onStack.Add(v);

                if (graph.TryGetValue(v, out var neighbors))
                {
                    foreach (var w in neighbors)
                    {
                        if (!indices.ContainsKey(w))
                        {
                            StrongConnect(w);
                            lowLink[v] = Math.Min(lowLink[v], lowLink[w]);
                        }
                        else if (onStack.Contains(w))
                        {
                            lowLink[v] = Math.Min(lowLink[v], indices[w]);
                        }
                    }
                }

                // If v is a root node, pop the stack and form an SCC
                if (lowLink[v] == indices[v])
                {
                    var scc = new List<string>();
                    string w;
                    do
                    {
                        w = stack.Pop();
                        onStack.Remove(w);
                        scc.Add(w);
                    } while (!string.Equals(w, v, StringComparison.OrdinalIgnoreCase));

                    sccs.Add(scc);
                }
            }

            foreach (var v in graph.Keys)
            {
                if (!indices.ContainsKey(v))
                    StrongConnect(v);
            }

            // Build mapping: node → its SCC index
            var nodeToScc = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < sccs.Count; i++)
            {
                foreach (var node in sccs[i])
                    nodeToScc[node] = i;
            }

            // Compute in-degree of each SCC in the condensed graph
            var sccInDegree = new int[sccs.Count];
            foreach (var kvp in graph)
            {
                var fromScc = nodeToScc[kvp.Key];
                foreach (var target in kvp.Value)
                {
                    var toScc = nodeToScc[target];
                    if (fromScc != toScc)
                        sccInDegree[toScc]++;
                }
            }

            // Source SCCs (in-degree 0) each need one representative
            var entryPoints = new List<string>();
            for (int i = 0; i < sccs.Count; i++)
            {
                if (sccInDegree[i] == 0)
                {
                    // Pick the first schema in the SCC as the representative
                    entryPoints.Add(sccs[i][0]);
                }
            }

            // If no entry points found (all in one cycle with no source), pick any one
            if (entryPoints.Count == 0 && sccs.Count > 0)
                entryPoints.Add(sccs[0][0]);

            return entryPoints;
        }
    }
}
