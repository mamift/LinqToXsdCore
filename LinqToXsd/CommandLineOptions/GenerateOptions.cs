using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using CommandLine;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    /// <summary>
    /// Instantiated by the CommandLineParser library.
    /// </summary>
    [Verb("gen", HelpText = "Code generation options.")]
    public class GenerateOptions
    {
        private List<string> schemaFiles = new List<string>();
        private string output;

        [Value(1, HelpText = "One or more schema files.", Required = true)]
        public IEnumerable<string> SchemaFiles
        {
            get => schemaFiles;
            set => schemaFiles = value?.ToList() ?? new List<string>();
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

        [Option('o', nameof(Output), HelpText = "Output file name. When specifying multiple XSD's, the output will be merged into a single file, whereby the output file name is taken from the first input file.")]
        public string Output
        {
            get
            {
                if (output.IsNotEmpty()) return output;
                var file = SchemaFiles.First();
                return Path.ChangeExtension(file, ".cs");
            }
            set => output = value;
        }

        [Option('c', nameof(Config), HelpText = "Specify the file path to an configuration file.")]
        public string Config { get; set; }

        [Option('a', nameof(Assembly), HelpText = "Generate an assembly (.dll). " + nameof(Output) + " is ignored if this is given.")]
        public string Assembly { get; set; }

        [Option('e', nameof(EnableServiceReference), HelpText = "Enable code output for use as a service reference.")]
        public bool EnableServiceReference { get; set; }
    }
}