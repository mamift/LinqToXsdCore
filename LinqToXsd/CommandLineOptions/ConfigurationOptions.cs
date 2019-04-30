using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace LinqToXsd
{
    [Verb(nameof(CommandLineOptions.config), HelpText = "Configuration for code generation.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class ConfigurationOptions: OptionsAbstract
    {
        [Option('e', nameof(Example), HelpText = "Produce an example XML configuration file for a given XSD file. e.g. LinqToXsd config 'file.xsd' -e'. Also: optionally specify this parameter with folder paths and it will create configuration files for multiple XSD files.")]
        public bool Example { get; set; }

        [Option('o', nameof(Output), HelpText = "When you specify multiple input files with --"+nameof(Example)+", use this to specify an output file for the configuration file that's generated. e.g. LinqToXsd config 'file.xsd' -e -o 'file.xsd.config'")]
        public override string Output { get; set; }
    }
}