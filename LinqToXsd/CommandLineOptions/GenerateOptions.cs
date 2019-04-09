using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using CommandLine;
using Xml.Schema.Linq;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    /// <summary>
    /// Instantiated by the CommandLineParser library.
    /// </summary>
    [Verb("gen", HelpText = "Code generation options.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class GenerateOptions
    {
        private List<string> schemaFiles = new List<string>();
        private string output;

        /// <summary>
        /// CLI argument: one or more schema files.
        /// </summary>
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

        /// <summary>
        /// CLI argument: output file name.
        /// </summary>
        [Option('o', nameof(Output), HelpText = "Output file name. When specifying multiple XSD's, this value is ignored. For specifying multiple output files for multiple input files, supply configuration XML document.")]
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

        /// <summary>
        /// CLI argument: file path to configuration XML file.
        /// </summary>
        [Option('c', nameof(Config), HelpText = "Specify the file path to an configuration file.")]
        public string Config { get; set; }

        /// <summary>
        /// If the <see cref="Config"/> property resolves to an actual file, then this property returns an actual <see cref="LinqToXsdSettings"/>
        /// instance of that configuration file.
        /// </summary>
        public LinqToXsdSettings ConfigInstance 
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Config) || !File.Exists(Config)) return null;
                var newConfig = new LinqToXsdSettings();
                newConfig.Load(Config);

                return newConfig;
            }
        }

        /// <summary>
        /// CLI argument: generate an assembly by given name.
        /// </summary>
        [Option('a', nameof(Assembly), HelpText = "Generate an assembly (.dll). " + nameof(Output) + " is ignored if this is given.")]
        public string Assembly { get; set; }

        /// <summary>
        /// CLI argument: imports 'System.Xml.Serialization' namespace into code or assembly.
        /// </summary>
        [Option('e', nameof(EnableServiceReference), HelpText = "Enable code output for use as a service reference; imports the 'System.Xml.Serialization' namespace into the generated code.")]
        public bool EnableServiceReference { get; set; }
    }
}