using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CommandLine;

namespace LinqToXsd
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal abstract class OptionsAbstract
    {
        private List<string> schemaFiles;

        /// <summary>
        /// CLI argument: One or more schema files or folders containing schema files.
        /// </summary>
        [Value(1, HelpText = "One or more schema files or folders containing schema files. If folders are given, then the files referenced in xs:include or xs:import directives are not imported twice. Usage: 'LinqToXsd <file1.xsd>,<file2.xsd>' or 'LinqToXsd <folder1>,<folder2>'.", Required = true)]
        public IEnumerable<string> SchemaFiles
        {
            get
            {
                var dirs = schemaFiles.Where(sf => File.GetAttributes(sf).HasFlag(FileAttributes.Directory)).ToArray();
                var files = schemaFiles.Except(dirs).ToList();
                var xsdFilesUnderDirs = dirs.Select(d => new DirectoryInfo(d).GetFiles("*.xsd", SearchOption.AllDirectories)).SelectMany(s => s);
                files.AddRange(xsdFilesUnderDirs.Select(fi => fi.FullName));

                var xDocs = files.ToDictionary(f => f, XDocument.Load).Where(kvp => kvp.Value.IsAnXmlSchema())
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                var filteredIncludeAndImportRefs = xDocs.FilterOutSchemasThatAreIncludedOrImported().Select(kvp => kvp.Key);

                return files.Except(filteredIncludeAndImportRefs).Distinct();
            }
            set => schemaFiles = value?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Returns <see cref="XmlReader"/> instances of every file specified in <see cref="SchemaFiles"/>.
        /// </summary>
        public Dictionary<string, XmlReader> SchemaReaders
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

        public abstract string Output { get; set; }
    }
}
