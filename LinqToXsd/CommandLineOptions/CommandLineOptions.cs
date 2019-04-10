using System.Diagnostics.CodeAnalysis;

namespace LinqToXsd
{
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global"), SuppressMessage("ReSharper", "InconsistentNaming")]
    internal struct CommandLineOptions
    {
        public ConfigurationOptions config { get; set; }

        public GenerateOptions gen { get; set; }
    }
}