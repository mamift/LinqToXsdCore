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
            Func<IEnumerable<string>, IEnumerable<string>> exclusionFilter = null)
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
            IEnumerable<string> filteredOut = exclusionFilter(enumeratedFileAndOrFolderPaths);
            return files.Except(filteredOut).Distinct().ToList();
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