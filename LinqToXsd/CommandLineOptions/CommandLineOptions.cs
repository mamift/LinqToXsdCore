using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace LinqToXsd
{
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class CommandLineOptions
    {
        [Option]
        public ConfigurationOptions Config { get; set; }

        [Option]
        public GenerateOptions Generate { get; set; }
    }
}