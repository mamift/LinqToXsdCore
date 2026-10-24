using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            "(string) Specify the file or folder path to one or more configuration file(s). Multiple configuration files are merged as one. Incompatible with -" +
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
        public LinqToXsdSettings? GetConfigInstance(IProgress<string> progress = null)
        {
            if (linqToXsdSettings != null) return linqToXsdSettings; // don't run twice
            if (Config.IsEmpty()) return null;
            if (!File.Exists(Config) && !Directory.Exists(Config)) return null;

            var fileInfo = new FileInfo(Config);
            if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                linqToXsdSettings = ConfigurationProvider.Load(new DirectoryInfo(fileInfo.FullName), progress);
            else
                linqToXsdSettings = XObjectsCoreGenerator.LoadLinqToXsdSettings(Config);

            return linqToXsdSettings;
        }

        /// <summary>
        /// CLI argument: imports 'System.Xml.Serialization' namespace into code or assembly.
        /// </summary>
        [Option('e', nameof(EnableServiceReference), HelpText = "(bool) Imports the 'System.Xml.Serialization' namespace into the generated code.")]
        public bool EnableServiceReference { get; set; }

        /// <summary>
        /// Determines whether <see cref="OptionsAbstract.Output"/> refers to a single output file rather than a folder.
        /// <para>A value ending in <c>.cs</c> (and that isn't an existing folder) is a file; anything else, including a
        /// folder that doesn't exist yet, is treated as a folder.</para>
        /// </summary>
        public bool OutputIsFile
        {
            get
            {
                var output = Output;
                if (output.IsEmpty()) return false;
                if (Directory.Exists(output)) return false;
                if (File.Exists(output)) return true;

                var lastCharacter = output[output.Length - 1];
                var endsWithSeparator = lastCharacter == Path.DirectorySeparatorChar || lastCharacter == Path.AltDirectorySeparatorChar;
                if (endsWithSeparator) return false;

                return string.Equals(Path.GetExtension(output), ".cs", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Loads the configuration files that <see cref="XObjectsCoreGenerator.Generate(IEnumerable{string}, IWarnableObserver{string})"/>
        /// would use for each of <see cref="OptionsAbstract.SchemaFiles"/>, i.e. a file named 'schema.xsd.config' alongside each schema.
        /// </summary>
        /// <returns>One <see cref="LinqToXsdSettings"/> per schema that has an accompanying configuration file.</returns>
        public IEnumerable<LinqToXsdSettings> LoadConfigInstancesBesideSchemas()
        {
            foreach (var schemaFile in SchemaFiles)
            {
                var configFile = $"{schemaFile}.config";
                if (!File.Exists(configFile)) continue;

                yield return XObjectsCoreGenerator.LoadLinqToXsdSettings(configFile);
            }
        }

        private bool autoConfig;

        [Option('a', nameof(AutoConfig), HelpText =
            "(bool) Specify this with a folder containing XSDs and accompanying configuration files. This argument associate a configuration file with an XSD when one follows the naming convention: 'schema.xsd' + 'schema.xsd.config' - this is the default convention used by the 'config -e' verb when you specify a folder. Use this parameter to associate an XSD with its own configuration settings to prevent those settings being overriden or merged as the -" +
            nameof(Config) + " argument would do. Only accepts folder paths. Incompatible with -" + nameof(Config) + ". Will only generate code for XSDs that have an accompanying .config file. If no output is generated, run the 'config' verb on the folder first. Any legacy output files (e.g. 'schema.xsd.cs') found next to the XSDs are deleted automatically before the new output ('schema.xsd-g.cs') is written.")]
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