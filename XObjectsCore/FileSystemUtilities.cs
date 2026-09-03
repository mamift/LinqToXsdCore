#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Resolvers;
using System.Xml.Schema;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq
{
    public static class FileSystemUtilities
    {
        /// <summary>
        /// Resolve file and folder paths to just files. That is, this method will accept a sequence of strings that can be either file or directory paths,
        /// and it returns another sequence of strings that are ONLY file paths.
        /// </summary>
        /// <param name="sequenceOfFileAndOrFolderPaths"></param>
        /// <param name="subdirFileFilter">Filter for filtering subdirectories only. Use <paramref name="exclusionFilter"/> to filter files in the immediate child folder.</param>
        /// <param name="exclusionFilter">Optionally provide a <see cref="Func{TResult}"/> that filters the <paramref name="sequenceOfFileAndOrFolderPaths"/> </param>
        /// <returns></returns>
        /// <exception cref="T:System.IO.IOException">This file is being used by another process.</exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">When any of the paths given represents a directory and is invalid, such as being on an unmapped drive, or the directory cannot be found.</exception>
        /// <exception cref="T:System.IO.PathTooLongException">The fully qualified path and file name is 260 or more characters.</exception>
        public static List<string> ResolveFileAndFolderPathsToJustFiles(IEnumerable<string> sequenceOfFileAndOrFolderPaths,
            string subdirFileFilter = "*.*",
            Func<IEnumerable<string>, IEnumerable<string>?>? exclusionFilter = null)
        {
            if (sequenceOfFileAndOrFolderPaths == null) throw new ArgumentNullException(nameof(sequenceOfFileAndOrFolderPaths));

            List<string> enumeratedFileAndOrFolderPaths = sequenceOfFileAndOrFolderPaths.ToList();

            if (!enumeratedFileAndOrFolderPaths.Any())
                throw new InvalidOperationException("There are no file or folder paths present in the enumerable!");

            string[] dirs = enumeratedFileAndOrFolderPaths.Where(sf => File.GetAttributes(sf).HasFlag(FileAttributes.Directory)).ToArray();
            List<string> files = enumeratedFileAndOrFolderPaths.Except(dirs).Select(Path.GetFullPath).ToList();
            IEnumerable<FileInfo> filteredFiles = dirs.SelectMany(d => new DirectoryInfo(d).GetFiles(subdirFileFilter, SearchOption.AllDirectories));
            files.AddRange(filteredFiles.Select(fi => fi.FullName));
            if (exclusionFilter == null) return files;
            // whatever is in this result will be filtered out of the return value
            IEnumerable<string>? filteredOut = exclusionFilter(enumeratedFileAndOrFolderPaths);
            if (filteredOut != null) return files.Except(filteredOut).Distinct().ToList();
            return files.Distinct().ToList();
        }

        public static bool HasFolderPaths(IEnumerable<string> sequenceOfFileAndOrFolderPaths) => 
            sequenceOfFileAndOrFolderPaths.Any(Directory.Exists);

        public static bool HasFilePaths(IEnumerable<string> sequenceOfFileAndOrFolderPaths) =>
            sequenceOfFileAndOrFolderPaths.Any(File.Exists);
        
        /// <summary>
        /// THe next logical step after <see cref="ResolveFileAndFolderPathsToJustFiles"/>, takes those resolved file paths
        /// and then narrows down the list to a list of file paths that refer to schemas that LinqToXsd can processes.
        /// <para>Uses SCC-based entry point detection (<see cref="XDocumentExtensions.FindEntryPointSchemas"/>) to find the
        /// minimum set of XSDs whose transitive imports/includes cover all XSDs in the set. This correctly handles
        /// cyclic import/include relationships (e.g. A imports B and B imports A).</para>
        /// </summary>
        /// <param name="filesOrFolders"></param>
        /// <returns></returns>
        public static List<string> ResolvePossibleFileAndFolderPathsToProcessableSchemas(IEnumerable<string> filesOrFolders)
        {
            List<string> files = ResolveFileAndFolderPathsToJustFiles(filesOrFolders, "*.xsd", files => files.Where(s => !s.EndsWith(".xsd")));

            // convert files to XDocuments and check if they are proper W3C schemas
            IEnumerable<(string fileName, XDocument schema)> pairs = files.Select(f => (fileName: f, schema: XDocument.Load(f)));
            Dictionary<string, XDocument> xDocs = pairs.Where(kvp => kvp.schema.IsAnXmlSchema())
                .ToDictionary(t => t.fileName, t => t.schema);

            if (xDocs.Count == 0)
                return new List<string>();

            // Use SCC-based entry point detection to correctly handle cycles in the import graph
            List<string> resolvedSchemaFiles = xDocs.FindEntryPointSchemas();

            return resolvedSchemaFiles;
        }

        /// <summary>
        /// Scans a folder for XSD files and produces a textual report showing which schemas each schema imports or
        /// includes. Each line follows the format:
        /// <c>ImporterFile.xsd &lt;- imp: ImportedFile.xsd, inc: IncludedFile.xsd</c>
        /// or <c>ImporterFile.xsd &lt;- (none)</c> if the schema imports/includes no other schema.
        /// </summary>
        /// <param name="folderPath">Path to a folder containing XSD files.</param>
        /// <param name="dirSearch"></param>
        /// <returns>An array of formatted lines, one per XSD found in the folder, sorted alphabetically by file name.</returns>
        public static string[] GenerateImportIncludeReport(string folderPath, SearchOption dirSearch = SearchOption.AllDirectories)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentNullException(nameof(folderPath));

            var dir = new DirectoryInfo(folderPath);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

            FileInfo[] xsdFiles = dir.GetFiles("*.xsd", dirSearch);
            if (xsdFiles.Length == 0)
                return Array.Empty<string>();

            XName includeXName = XDocumentExtensions.IncludeXName;
            XName importXName = XDocumentExtensions.ImportXName;
            XName schemaLocationXName = XName.Get("schemaLocation");

            // Build a forward map: for each XSD (by filename), collect what it imports/includes.
            // Key: importer file name, Value: list of "imp: ImportedFileName" or "inc: IncludedFileName"
            var importsBy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (FileInfo xsdFile in xsdFiles)
            {
                XDocument xDoc;
                try
                {
                    xDoc = XDocument.Load(xsdFile.FullName);
                }
                catch
                {
                    continue; // skip unparseable files
                }

                if (!xDoc.IsAnXmlSchema())
                    continue;

                string importerName = xsdFile.Name;

                // Collect all xs:import and xs:include elements
                foreach (var element in xDoc.Descendants(importXName))
                {
                    var sl = element.Attribute(schemaLocationXName);
                    if (sl != null && !string.IsNullOrWhiteSpace(sl.Value))
                    {
                        string referencedFileName = Path.GetFileName(sl.Value);
                        if (!string.IsNullOrEmpty(referencedFileName))
                            AddImportEntry(importsBy, importerName, $"imp: {referencedFileName}");
                    }
                }

                foreach (var element in xDoc.Descendants(includeXName))
                {
                    var sl = element.Attribute(schemaLocationXName);
                    if (sl != null && !string.IsNullOrWhiteSpace(sl.Value))
                    {
                        string referencedFileName = Path.GetFileName(sl.Value);
                        if (!string.IsNullOrEmpty(referencedFileName))
                            AddImportEntry(importsBy, importerName, $"inc: {referencedFileName}");
                    }
                }
            }

            // Produce one line per XSD file
            var lines = new List<string>();
            foreach (FileInfo xsdFile in xsdFiles.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                string fileName = xsdFile.Name;
                if (importsBy.TryGetValue(fileName, out var imports) && imports.Count > 0)
                {
                    lines.Add($"{fileName} <- {string.Join(", ", imports.OrderBy(i => i, StringComparer.OrdinalIgnoreCase))}");
                }
                else
                {
                    lines.Add($"{fileName} <- (none)");
                }
            }

            return lines.ToArray();
        }

        private static void AddImportEntry(Dictionary<string, List<string>> importsBy, string importerName, string entry)
        {
            if (!importsBy.TryGetValue(importerName, out var list))
            {
                list = new List<string>();
                importsBy[importerName] = list;
            }

            if (!list.Contains(entry))
                list.Add(entry);
        }

        /// <summary>
        /// Assuming that other XSDs exist in the same directory as the given <paramref name="fileName"/>, this will pre-load those
        /// additional XSDs into an <see cref="XmlPreloadedResolver"/> and use them if they are referenced by the file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Returns a compiled <see cref="XmlSchemaSet"/></returns>
        public static XmlSchemaSet PreLoadXmlSchemas(string fileName)
        {
            if (fileName.IsEmpty()) throw new ArgumentNullException(nameof(fileName));

            var xsdFile = new FileInfo(fileName);
            var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(fileName));
            FileInfo[] additionalXsds = directoryInfo.GetFiles("*.xsd");

            var xmlPreloadedResolver = new XmlPreloadedResolver();

            foreach (FileInfo xsd in additionalXsds) {
                xmlPreloadedResolver.Add(new Uri($"file://{xsd.FullName}"), File.OpenRead(xsd.FullName));
            }

            var xmlReaderSettings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true
            };
            
            XmlSchemaSet xmlSchemaSet = XmlReader.Create(xsdFile.FullName, xmlReaderSettings)
                .ToXmlSchemaSet(xmlPreloadedResolver);

            return xmlSchemaSet;
        }
    }
}