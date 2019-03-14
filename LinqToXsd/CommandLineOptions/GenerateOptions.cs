using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using CommandLine;

namespace LinqToXsd
{
    [Verb("gen", HelpText = "Code generation options.")]
    public class GenerateOptions
    {
        private List<string> schemaFiles = new List<string>();

        [Value(1, HelpText = "One or more schema files.", Required = true)]
        public IEnumerable<string> SchemaFiles
        {
            get => schemaFiles;
            set => schemaFiles = value.ToList();
        }

        /// <summary>
        /// Returns <see cref="XmlReader"/> instances of every file specified in <see cref="SchemaFiles"/>.
        /// </summary>
        public IEnumerable<XmlReader> SchemaReaders
        {
            get
            {
                if (!SchemaFiles.Any()) return new XmlReader[0];
                var xmlReaderSettings = new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Parse
                };
                return SchemaFiles.Select(sf => XmlReader.Create(sf, xmlReaderSettings));
            }
        }

        /// <summary>
        /// Returns <see cref="XmlSchemaSet"/>s of every instance of an <see cref="XmlReader"/> in <see cref="SchemaReaders"/>.
        /// </summary>
        public IEnumerable<XmlSchemaSet> SchemaSets
        {
            get
            {
                if (!SchemaReaders.Any()) return new XmlSchemaSet[0];
                return SchemaReaders.Select(sr =>
                {
                    var xmlSchemaSet = new XmlSchemaSet();
                    xmlSchemaSet.Add(null, sr);
                    return xmlSchemaSet;
                });
            }
        }

        [Option(nameof(Output), HelpText = "Output file name. When specifying multiple XSD's, the output will be merged into a single file.")]
        public string Output { get; set; }

        [Option(nameof(Configuration), HelpText = "Specify the file path to an configuration file.")]
        public string Configuration { get; set; }

        [Option(nameof(Assembly), HelpText = "Generate an assembly (.dll). " + nameof(Output) + " is ignored if this is given.")]
        public string Assembly { get; set; }

        [Option(nameof(EnableServiceReference), HelpText = "Enable code output for use as a service reference.")]
        public bool EnableServiceReference { get; set; }
    }
}