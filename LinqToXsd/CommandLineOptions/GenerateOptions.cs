using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CommandLine;
using Xml.Schema.Linq;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    /// <summary>
    /// Instantiated by the CommandLineParser library.
    /// </summary>
    [Verb(nameof(CommandLineOptions.gen), HelpText = "Code generation options.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class GenerateOptions: OptionsAbstract
    {
        private string output;

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
                if (Config.IsEmpty() || !File.Exists(Config)) return null;
                return XObjectsCoreGenerator.LoadLinqToXsdSettings(Config);
            }
        }

        /// <summary>
        /// CLI argument: imports 'System.Xml.Serialization' namespace into code or assembly.
        /// </summary>
        [Option('e', nameof(EnableServiceReference), HelpText = "Enable code output for use as a service reference; imports the 'System.Xml.Serialization' namespace into the generated code.")]
        public bool EnableServiceReference { get; set; }
    }
}