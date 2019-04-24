using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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

        private List<string> schemaFiles;

        /// <summary>
        /// CLI argument: One or more schema files or folders containing schema files.
        /// </summary>
        [Value(1, HelpText = SchemaFilesHelpText, Required = true)]
        public IEnumerable<string> SchemaFiles
        {
            get
            {
                var dirs = schemaFiles.Where(sf => File.GetAttributes(sf).HasFlag(FileAttributes.Directory)).ToArray();
                var files = schemaFiles.Except(dirs).ToList();
                var xsdFilesUnderDirs = dirs.SelectMany(d => new DirectoryInfo(d).GetFiles("*.xsd", SearchOption.AllDirectories));
                files.AddRange(xsdFilesUnderDirs.Select(fi => fi.FullName));

                var xDocs = files.ToDictionary(f => f, XDocument.Load).Where(kvp => kvp.Value.IsAnXmlSchema())
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                var filteredIncludeAndImportRefs = xDocs.FilterOutSchemasThatAreIncludedOrImported().Select(kvp => kvp.Key);

                return files.Except(filteredIncludeAndImportRefs).Distinct();
            }

            set
            {
                // this fixes a curious issue in the CommmandLine parser that sometimes pops up
                var possibleUnparsedCommas = value
                    .Select(v => v.Replace("\\", @"\"))
                    .SelectMany(pf => pf.Split(',', StringSplitOptions.RemoveEmptyEntries));
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
                if (!SchemaFiles.Any()) return new Dictionary<string, XmlReader>();

                var xmlReaderSettings = new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Parse
                };
                return SchemaFiles.ToDictionary(f => f, f => XmlReader.Create(f, xmlReaderSettings));
            }
        }

        /// <summary>
        /// CLI argument: output file name or folder.
        /// </summary>
        [Option('o', nameof(Output), HelpText = OutputHelpText)]
        public virtual string Output { get; set; }
    }
}
