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
        private string config;

        /// <summary>
        /// CLI argument: file path to configuration XML file.
        /// </summary>
        [Option('c', nameof(Config), HelpText =
            "Specify the file or folder path to one or more configuration file(s). Multiple configuration files are merged as one. Incompatible with -" +
            nameof(AutoConfig))]
        public string Config
        {
            get => config;
            set
            {
                if (autoConfig) throw new IncompatibleArgumentException(nameof(AutoConfig));
                config = value;
            }
        }

        private LinqToXsdSettings linqToXsdSettings;

        /// <summary>
        /// If the <see cref="Config"/> property resolves to an actual file, then this property returns an <see cref="linqToXsdSettings"/>
        /// instance of that configuration file.
        /// <para>If this resolves to a folder, then all those configuration files are merged into a single instance.</para>
        /// </summary>
        public LinqToXsdSettings GetConfigInstance(IProgress<string> progress = null)
        {
            if (linqToXsdSettings != null) return linqToXsdSettings; // don't run twice
            if (Config.IsEmpty()) return null;
            if (!File.Exists(Config) && !Directory.Exists(Config)) return null;

            var fileInfo = new FileInfo(Config);
            linqToXsdSettings = fileInfo.Attributes.HasFlag(FileAttributes.Directory)
                ? ConfigurationProvider.Load(new DirectoryInfo(fileInfo.FullName), progress) // load directory
                : XObjectsCoreGenerator.LoadLinqToXsdSettings(Config); // load file

            return linqToXsdSettings;
        }

        /// <summary>
        /// CLI argument: imports 'System.Xml.Serialization' namespace into code or assembly.
        /// </summary>
        [Option('e', nameof(EnableServiceReference), HelpText = "Imports the 'System.Xml.Serialization' namespace into the generated code.")]
        public bool EnableServiceReference { get; set; }

        private bool autoConfig;

        [Option('a', nameof(AutoConfig), HelpText =
            "Specify one folder containing XSDs and their accompanying configuration files. This argument associate a configuration file with an XSD when one follows the naming convention: 'schema.xsd' + 'schema.xsd.config'. The XSD and .config file must be in the same directory as each other; use this parameter to individually associate an XSD with its own configuration settings to prevent those settings being overriden or merged as the -" +
            nameof(Config) + " argument would do. Only accepts folder paths. Incompatible with -" + nameof(Config) + ". Will only generate code for XSDs that have an accompanying .config file. If no output is generated, run the 'config' verb on the folder first.")]
        public bool AutoConfig
        {
            get => autoConfig;
            set
            {
                if (config.IsNotEmpty()) throw new IncompatibleArgumentException(nameof(Config));
                autoConfig = value;
            }
        }
    }
}