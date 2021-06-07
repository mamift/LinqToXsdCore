using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CommandLine;
using Xml.Schema.Linq;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    /// <summary>
    /// Base class for creating new verbs (to place under <see cref="CommandLineOptions"/>).
    /// <para>Note to future devs: make everything <c>virtual</c>!</para>
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal abstract class OptionsAbstract: IDisposable
    {
        internal const string OutputHelpText = "(string) Output file name or folder. When specifying multiple input XSDs or input folders, and this value is a file, all output is merged into a single file. If this value is a folder, multiple output files are output to this folder.";

        internal const string FilesOrFoldersHelpText = "(string[]) One or more schema files or folders containing schema files. Separate multiple files using a comma (,). If folders are given, then the files referenced in xs:include or xs:import directives are not imported twice. Usage: 'LinqToXsd [verb] <file1.xsd>,<file2.xsd>' or 'LinqToXsd [verb] <folder1>,<folder2>'. You can also include folder and file paths in the same invocation: 'LinqToXsd [verb] <file1.xsd>,<folder1>'";

        /// <summary>
        /// Determines if at least one folder path was given in <see cref="FilesOrFolders"/>.
        /// </summary>
        /// <returns></returns>
        public virtual bool FoldersWereGiven => FileSystemUtilities.HasFolderPaths(FilesOrFolders);

        /// <summary>
        /// Determines if at least one file path were given in <see cref="FilesOrFolders"/>.
        /// </summary>
        /// <returns></returns>
        public virtual bool FilesWereGiven => FileSystemUtilities.HasFilePaths(FilesOrFolders);

        protected List<string> filesOrFolders = new List<string>();

        protected Dictionary<string, XmlReader> schemaReaders = new Dictionary<string, XmlReader>();

        protected List<string> resolvedSchemaFiles = new List<string>();

        /// <summary>
        /// CLI argument: The file or folder paths given at the CL.
        /// </summary>
        [Value(1, HelpText = FilesOrFoldersHelpText, Required = true)]
        public virtual IEnumerable<string> FilesOrFolders
        {
            get => filesOrFolders;
            set
            {
                // this fixes a curious issue in the CommandLine parser that sometimes pops up
                var possibleUnparsedCommas = value
                                             .Select(v => v.Replace("\\", @"\"))
                                             .SelectMany(pf => pf.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                             .Select(v => v.Trim('\\', '/')); // removes trailing slashes for directories
                filesOrFolders = possibleUnparsedCommas.ToList();
            }
        }

        /// <summary>
        /// Resolves the file or folder paths in <see cref="FilesOrFolders"/> property as just files, filtering to only include *.xsd files under
        /// any folder paths present.
        /// <para>Computed on every read.</para>
        /// </summary>
        public virtual IEnumerable<string> SchemaFiles
        {
            get
            {
                if (resolvedSchemaFiles.Any()) return resolvedSchemaFiles;

                resolvedSchemaFiles = FileSystemUtilities.ResolvePossibleFileAndFolderPathsToProcessableSchemas(FilesOrFolders);

                return resolvedSchemaFiles;
            }
        }

        /// <summary>
        /// Returns <see cref="XmlReader"/> instances of every file specified in <see cref="SchemaFiles"/>.
        /// </summary>
        public virtual Dictionary<string, XmlReader> SchemaReaders
        {
            get
            {
                var schemasFiles = SchemaFiles.ToArray(); // save a reference otherwise it gets enumerated twice
                if (!schemasFiles.Any()) return new Dictionary<string, XmlReader>();
                if (schemaReaders.Any()) return schemaReaders;

                var xmlReaderSettings = new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Parse,
                    CloseInput = true
                };
                
                schemaReaders = schemasFiles.ToDictionary(f => f, f => XmlReader.Create(f, xmlReaderSettings));
                return schemaReaders;
            }
        }

        /// <summary>
        /// CLI argument: output file name or folder.
        /// </summary>
        [Option('o', nameof(Output), HelpText = OutputHelpText)]
        public virtual string Output { get; set; }

        /// <summary>
        /// Default method for disposing of the <see cref="XmlReader"/>s in the <see cref="SchemaReaders"/> property.
        /// </summary>
        public virtual void Dispose()
        {
            foreach (var kvp in schemaReaders) {
                if (!kvp.Value.ReadState.HasFlag(ReadState.Closed)) kvp.Value.Close();
                kvp.Value.Dispose();
            }
        }

        protected OptionsAbstract() { }

        ~OptionsAbstract() { Dispose(); }
    }
}
