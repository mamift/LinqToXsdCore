using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace LinqToXsd
{
    [Verb(nameof(CommandLineOptions.config), HelpText = "Configuration for code generation.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class ConfigurationOptions: OptionsAbstract
    {
        internal const string ExampleHelpText = "Produce an example XML configuration file for a given XSD file. e.g. LinqToXsd config -e 'file.xsd''. Optionally specify this parameter with folder paths and it will create configuration files for multiple XSD files inside the folder(s). This will overwrite any existing configuration files.";

        internal new const string OutputHelpText = "When you specify multiple input files with --" + nameof(Example) + ", use this to specify an output file for the configuration file that's generated. e.g. LinqToXsd config 'file.xsd' -e -o 'file.xsd.config' - this can allow you use a single output file for multiple input XSD's. Optionally combine this with -e to produce a single example configuration file with default values: i.e. LinqToXsd config -e -o 'egConfig.xml'. Output can also be folder.";

        [Option('e', nameof(Example), HelpText = ExampleHelpText)]
        public bool Example { get; set; }

        /// <summary>
        /// This is to override the <see cref="OutputHelpText"/>.
        /// </summary>
        [Option('o', nameof(Output), HelpText = OutputHelpText)]
        public override string Output { get; set; }

        /// <summary>
        /// This overrides the base member to to set the <see cref="BaseAttribute.Required"/> to false.
        /// </summary>
        [Value(1, HelpText = FilesOrFoldersHelpText, Required = false)]
        public override IEnumerable<string> FilesOrFolders
        {
            get => base.FilesOrFolders;
            set => base.FilesOrFolders = value;
        }
    }
}