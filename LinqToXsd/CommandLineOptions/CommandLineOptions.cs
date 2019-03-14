using CommandLine;

namespace LinqToXsd
{
    public class CommandLineOptions
    {
        [Option]
        public ConfigurationOptions Config { get; set; }

        [Option]
        public GenerateOptions Generate { get; set; }
    }
}