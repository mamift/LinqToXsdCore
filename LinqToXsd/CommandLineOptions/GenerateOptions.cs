using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
        [Option('e', nameof(EnableServiceReference), HelpText = "Imports the 'System.Xml.Serialization' namespace into the generated code.")]
        public bool EnableServiceReference { get; set; }
    }
}