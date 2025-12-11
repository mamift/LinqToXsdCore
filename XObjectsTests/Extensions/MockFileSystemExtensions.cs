using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Resolvers;
using System.Xml.Schema;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class MockFileSystemExtensions
{
    extension(MockFileSystem mfs)
    {
        public MockFileInfo GetMockFileInfo(Func<string, bool> searchForASingleFilePath)
        {
            var filePath = mfs.AllPaths.SingleOrDefault(searchForASingleFilePath);
            if (filePath is null) throw new InvalidOperationException();

            return new MockFileInfo(mfs, filePath);
        }

        public MockFileInfo GetMockFileInfo(string mockFilePath)
        {
            return new MockFileInfo(mfs, mockFilePath);
        }

        public List<IFileInfo> ResolveFileAndFolderPathsToMockFileInfos(IEnumerable<string> sequenceOfFileAndOrFolderPaths, string filter = "*.*")
        {
            if (sequenceOfFileAndOrFolderPaths == null) throw new ArgumentNullException(nameof(sequenceOfFileAndOrFolderPaths));

            var enumeratedFileAndOrFolderPaths = sequenceOfFileAndOrFolderPaths.ToList();

            if (!enumeratedFileAndOrFolderPaths.Any())
                throw new InvalidOperationException("There are no file or folder paths present in the enumerable!");

            var dirs = enumeratedFileAndOrFolderPaths.Where(sf => mfs.GetFile(sf).Attributes.HasFlag(FileAttributes.Directory)).ToArray();
            var files = enumeratedFileAndOrFolderPaths.Except(dirs).Select(f => mfs.FileInfo.New(f)).ToList();
            var filteredFiles = dirs.SelectMany(d =>
                new MockDirectoryInfo(mfs, d).GetFiles(filter, SearchOption.AllDirectories).Select(f => f)).ToList();
            files.AddRange(filteredFiles);
            return files;
        }

        public List<IFileInfo> ResolvePossibleFileAndFolderPathsToProcessableSchemas(IEnumerable<string> filesOrFolders)
        {
            var files = mfs.ResolveFileAndFolderPathsToMockFileInfos(filesOrFolders, "*.xsd");
            var filesComparisonList = files.Select(f => f.FullName);

            // convert files to XDocuments and check if they are proper W3C schemas
            var pairs = files.Select(f => (file: f, schema: XDocument.Parse(mfs.GetFile(f).TextContents)));
            var xDocs = pairs.Where(kvp => kvp.schema.IsAnXmlSchema())
                .ToDictionary(kvp => kvp.file, kvp => kvp.schema);

            var filteredIncludeAndImportRefs = xDocs.FilterOutSchemasThatAreIncludedOrImported().Select(kvp => kvp.Key).ToList();
            var filteredIncludeAndImportRefsComparisonList = filteredIncludeAndImportRefs.Select(f => f.FullName);

            var resolvedSchemaFilesFilteredList = filesComparisonList.Except(filteredIncludeAndImportRefsComparisonList).Distinct().ToList();
            var resolvedSchemaFiles = resolvedSchemaFilesFilteredList.Select(fn => mfs.FileInfo.New(fn)).ToList();

            if (filteredIncludeAndImportRefs.Count == files.Count && !resolvedSchemaFilesFilteredList.Any()) {
                throw new LinqToXsdException("Cannot decide which XSD files to process as the specified " +
                                             "XSD files or folder of XSD files recursively import and/or " +
                                             "include each other! In this case you must explicitly provide" +
                                             "a file path and not a folder path.");
            }

            return resolvedSchemaFiles;
        }

        /// <summary>
        /// Assuming that other XSDs exist in the same directory as the given <paramref name="fileName"/>, this will pre-load those
        /// additional XSDs into an <see cref="XmlPreloadedResolver"/> and use them if they are referenced by the file.
        /// </summary>
        /// <param name="mfs"></param>
        /// <param name="fileName"></param>
        /// <returns>Returns a compiled <see cref="XmlSchemaSet"/></returns>
        public XmlSchemaSet PreLoadXmlSchemas(string fileName)
        {
            if (fileName.IsEmpty()) throw new ArgumentNullException(nameof(fileName));

            var xsdFile = mfs.FileInfo.New(fileName);
            var directoryInfo = mfs.DirectoryInfo.New(xsdFile.DirectoryName!);
            var additionalXsds = directoryInfo.GetFiles("*.xsd")
                .Where(f => f.FullName != xsdFile.FullName);

            var xmlPreloadedResolver = new MockXmlUrlResolver(mfs);

            foreach (var xsd in additionalXsds) {
                var pathRoot = Path.GetPathRoot(xsd.FullName) ?? "";
                var unrooted = xsd.FullName.Replace(pathRoot, "");

                var xsdText = new StreamReader(xsd.OpenRead()).ReadToEnd();
                Assert.IsNotNull(xsdText);
                Assert.IsFalse(string.IsNullOrWhiteSpace(xsdText));
                xmlPreloadedResolver.Add(new Uri($"file://{xsd.FullName}", UriKind.Absolute), xsd);
            }

            var xmlReaderSettings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true
            };

            var xmlReader = XmlReader.Create(xsdFile.OpenRead(), xmlReaderSettings);
            XmlSchemaSet xmlSchemaSet = xmlReader.ToXmlSchemaSet(xmlPreloadedResolver);

            return xmlSchemaSet;
        }
    }
}