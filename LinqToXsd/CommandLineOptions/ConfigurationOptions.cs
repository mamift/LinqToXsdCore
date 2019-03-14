using CommandLine;

namespace LinqToXsd
{
    public class ConfigurationOptions
    {
        [Option(nameof(Example), HelpText = "Generate an example XML configuration file for use with LinqToXsd.")]
        public bool Example { get; set; }
    }
}