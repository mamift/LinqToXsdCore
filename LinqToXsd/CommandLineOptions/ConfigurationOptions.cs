using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace LinqToXsd
{
    [Verb(nameof(CommandLineOptions.config), HelpText = "Configuration for code generation.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class ConfigurationOptions: OptionsAbstract
    {
        [Option('e', nameof(Example), HelpText = "Produce an example XML configuration file for use with LinqToXsd. Optionally specify this parameter with some files paths to XSD files and it will create a configuration file using namespaces found in the given XSD files. e.g. LinqToXsd config 'file.xsd' -e'")]
        public bool Example { get; set; }
    }
}