using System.Diagnostics.CodeAnalysis;

namespace LinqToXsd
{
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global"), SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class CommandLineOptions
    {
        public ConfigurationOptions config { get; set; }

        public GenerateOptions gen { get; set; }
    }
}