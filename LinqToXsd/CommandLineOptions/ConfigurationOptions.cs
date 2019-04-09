using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace LinqToXsd
{
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class ConfigurationOptions
    {
        [Option(nameof(Example), HelpText = "Generate an example XML configuration file for use with LinqToXsd.")]
        public bool Example { get; set; }
    }
}