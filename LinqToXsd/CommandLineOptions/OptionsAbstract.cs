using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CommandLine;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal abstract class OptionsAbstract
    {
        internal const string OutputHelpText = "Output file name or folder. When specifying multiple input XSDs or input folders, and this value is a file, all output is merged into a single file. If this value is a folder, multiple output files are output to this folder.";
        internal const string SchemaFilesHelpText = "One or more schema files or folders containing schema files. Separate multiple files using a comma (,). If folders are given, then the files referenced in xs:include or xs:import directives are not imported twice. Usage: 'LinqToXsd [verb] <file1.xsd>,<file2.xsd>' or 'LinqToXsd [verb] <folder1>,<folder2>'.";

        private List<string> schemaFiles = new List<string>();

        /// <summary>
        /// CLI argument: One or more schema files or folders containing schema files. Filters to include only .xsd files.
        /// </summary>
        [Value(1, HelpText = SchemaFilesHelpText, Required = true)]
        public IEnumerable<string> SchemaFiles
        {
            get
            {
                var files = FileSystemUtilities.ResolveFileAndFolderPathsToJustFiles(schemaFiles, "*.xsd");
                // convert files to XDocuments and check if they are proper W3C schemas
                var xDocs = files.Select(f => new KeyValuePair<string, XDocument>(f, XDocument.Load(f)))
                                 .Where(kvp => kvp.Value.IsAnXmlSchema())
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                var filteredIncludeAndImportRefs = xDocs.FilterOutSchemasThatAreIncludedOrImported().Select(kvp => kvp.Key);

                return files.Except(filteredIncludeAndImportRefs).Distinct();
            }

            set
            {
                // this fixes a curious issue in the CommandLine parser that sometimes pops up
                var possibleUnparsedCommas = value
                    .Select(v => v.Replace("\\", @"\"))
                    .SelectMany(pf => pf.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(v => v.Trim('\\','/')); // removes trailing slashes for directories
                schemaFiles = possibleUnparsedCommas.ToList();
            }
        }

        /// <summary>
        /// Returns <see cref="XmlReader"/> instances of every file specified in <see cref="SchemaFiles"/>.
        /// </summary>
        public virtual Dictionary<string, XmlReader> SchemaReaders
        {
            get
            {
                var schemasFiles = SchemaFiles.ToArray(); // save a reference otherwise it gets enumerated twice
                if (!schemasFiles.Any()) return new Dictionary<string, XmlReader>();

                var xmlReaderSettings = new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Parse
                };
                return schemasFiles.ToDictionary(f => f, f => XmlReader.Create(f, xmlReaderSettings));
            }
        }

        /// <summary>
        /// CLI argument: output file name or folder.
        /// </summary>
        [Option('o', nameof(Output), HelpText = OutputHelpText)]
        public virtual string Output { get; set; }
    }
}
