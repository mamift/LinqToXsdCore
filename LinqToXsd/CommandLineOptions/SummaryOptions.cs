using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace LinqToXsd
{
    /// <summary>
    /// Instantiated by the CommandLineParser library for the 'sum' verb.
    /// </summary>
    [Verb(nameof(CommandLineOptions.sum), HelpText = "Summarises generated code files (.xsd-g.cs) into markdown.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class SummaryOptions: OptionsAbstract
    {
        internal new const string FilesOrFoldersHelpText = "(string[]) One or more .xsd-g.cs files, or a single folder containing .xsd-g.cs files. Separate multiple files using a comma (,). Usage: 'LinqToXsd sum <file1.xsd-g.cs>,<file2.xsd-g.cs>' or 'LinqToXsd sum <folder>'.";

        internal new const string OutputHelpText = "(string) Optional output folder for the generated markdown file(s). When omitted, each markdown file is written next to its source file.";

        /// <summary>
        /// This overrides the base member to customise the help text for the 'sum' verb.
        /// </summary>
        [Value(1, HelpText = FilesOrFoldersHelpText, Required = true)]
        public override IEnumerable<string> FilesOrFolders
        {
            get => base.FilesOrFolders;
            set => base.FilesOrFolders = value;
        }

        /// <summary>
        /// This overrides the base member to customise the help text for the 'sum' verb.
        /// </summary>
        [Option('o', nameof(Output), HelpText = OutputHelpText)]
        public override string Output { get; set; }
    }
}
