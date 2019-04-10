using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace LinqToXsd
{
    [Verb(nameof(CommandLineOptions.config), HelpText = "Configuration for code generation.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class ConfigurationOptions: OptionsAbstract
    {
        [Option('e', nameof(Example), HelpText = "gen an example XML configuration file for use with LinqToXsd.")]
        public bool Example { get; set; }
    }
}