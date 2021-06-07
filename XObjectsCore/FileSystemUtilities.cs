using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
        /// <param name="filter"></param>
        /// <param name="sequenceFilterFunctor">Optionally provide a <see cref="Func{TResult}"/> that filters the <paramref name="sequenceOfFileAndOrFolderPaths"/> </param>
        /// <returns></returns>
        /// <exception cref="T:System.IO.IOException">This file is being used by another process.</exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">When any of the paths given represents a directory and is invalid, such as being on an unmapped drive, or the directory cannot be found.</exception>
        /// <exception cref="T:System.IO.PathTooLongException">The fully qualified path and file name is 260 or more characters.</exception>
        public static List<string> ResolveFileAndFolderPathsToJustFiles(IEnumerable<string> sequenceOfFileAndOrFolderPaths,
            string filter = "*.*",
            Func<IEnumerable<string>, IEnumerable<string>> sequenceFilterFunctor = null)
        {
            if (sequenceOfFileAndOrFolderPaths == null) throw new ArgumentNullException(nameof(sequenceOfFileAndOrFolderPaths));

            var enumeratedFileAndOrFolderPaths = sequenceOfFileAndOrFolderPaths.ToList();

            if (!enumeratedFileAndOrFolderPaths.Any())
                throw new InvalidOperationException("There are no file or folder paths present in the enumerable!");

            var dirs = enumeratedFileAndOrFolderPaths.Where(sf => File.GetAttributes(sf).HasFlag(FileAttributes.Directory)).ToArray();
            var files = enumeratedFileAndOrFolderPaths.Except(dirs).Select(Path.GetFullPath).ToList();
            var filteredFiles = dirs.SelectMany(d => new DirectoryInfo(d).GetFiles(filter, SearchOption.AllDirectories));
            files.AddRange(filteredFiles.Select(fi => fi.FullName));
            if (sequenceFilterFunctor == null) return files;
            // whatever is in this result will be filtered out of the return value
            var filteredOut = sequenceFilterFunctor(enumeratedFileAndOrFolderPaths) ?? new List<string>(); // can't be certain that the return value is not null
            return files.Except(filteredOut).Distinct().ToList();
        }

        public static bool HasFolderPaths(IEnumerable<string> sequenceOfFileAndOrFolderPaths) => 
            sequenceOfFileAndOrFolderPaths.Any(Directory.Exists);

        public static bool HasFilePaths(IEnumerable<string> sequenceOfFileAndOrFolderPaths) =>
            sequenceOfFileAndOrFolderPaths.Any(File.Exists);

        /// <summary>
        /// THe next logical step after <see cref="ResolveFileAndFolderPathsToJustFiles"/>, takes those resolved file paths
        /// and then narrows down the list to a list of file paths that refer to schemas that LinqToXsd can processes.
        /// <para>Filters out schemas that are themselves included or imported (invokes
        /// <see cref="XDocumentExtensions.FilterOutSchemasThatAreIncludedOrImported"/>).</para>
        /// </summary>
        /// <param name="filesOrFolders"></param>
        /// <returns></returns>
        public static List<string> ResolvePossibleFileAndFolderPathsToProcessableSchemas(IEnumerable<string> filesOrFolders)
        {
            var files = ResolveFileAndFolderPathsToJustFiles(filesOrFolders, "*.xsd");

            // convert files to XDocuments and check if they are proper W3C schemas
            var pairs = files.Select(f => new KeyValuePair<string, XDocument>(f, XDocument.Load(f)));
            var xDocs = pairs.Where(kvp => kvp.Value.IsAnXmlSchema())
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var filteredIncludeAndImportRefs = xDocs.FilterOutSchemasThatAreIncludedOrImported().Select(kvp => kvp.Key).ToList();
            
            var resolvedSchemaFiles = files.Except(filteredIncludeAndImportRefs).Distinct().ToList();


            if (filteredIncludeAndImportRefs.Count == files.Count && !resolvedSchemaFiles.Any()) {
                throw new LinqToXsdException("Cannot decide which XSD files to process as the specified " +
                                             "XSD files or folder of XSD files recursively import and/or " +
                                             "include each other! In this case you must explicitly provide" +
                                             "a file path and not a folder path.");
            }

            return resolvedSchemaFiles;
        }
    }
}