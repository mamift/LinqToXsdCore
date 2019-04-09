using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace LinqToXsd
{
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class CommandLineOptions
    {
        [Option(null, HelpText = "Options for configuring LinqToXsd.")]
        public ConfigurationOptions Config { get; set; }

        [Option(null, HelpText = "Options for generating code.")]
        public GenerateOptions Generate { get; set; }
    }
}