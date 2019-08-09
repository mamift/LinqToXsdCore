using System.IO;
using System.Linq;
using Alba.CsConsoleFormat.Fluent;
using Xml.Schema.Linq;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    public static partial class Program
    {
        internal static class ConfigurationDispatcher
        {
            /// <summary>
            /// Outputs an example configuration file for use with LinqToXsd.
            /// </summary>
            /// <param name="configOpts"></param>
            internal static void HandleGenerateExampleConfig(ConfigurationOptions configOpts)
            {
                if (!configOpts.SchemaFiles.Any() && configOpts.FoldersWereGiven) {
                    PrintLn("Folder inputs(s) were provided, but no XSD files were found inside!".Red());
                    PrintLn("To generate a single example configuration file, specify an output file name or folder.".Yellow());
                    return;
                }

                var egConfigXml = ConfigurationProvider.ProvideExampleConfigurationXml();

                var defaultFileName = "exampleConfiguration.xml";
                var outputWasGiven = configOpts.Output.IsNotEmpty();
                var outputFilePath = outputWasGiven ? configOpts.Output : defaultFileName;

                var possibleGivenFileName = Path.GetFileName(outputFilePath);
                var noGivenFileName = possibleGivenFileName.IsEmpty() || !Path.HasExtension(possibleGivenFileName);
                if (noGivenFileName) { // assume this is a directory that may or may not exist
                    outputFilePath = Path.Combine(outputFilePath, defaultFileName);
                }
                
                PrintLn("Saving to:".Green());
                PrintLn($"\t{outputFilePath}".White());

                egConfigXml.Save(outputFilePath);
            }

            /// <summary>
            /// Creates a configuration for use with LinqToXsd based on the namespaces found in XSD documents.
            /// </summary>
            /// <param name="configOpts"></param>
            internal static void HandleAutoGenConfig(ConfigurationOptions configOpts)
            {
                if (configOpts.FilesOrFolders.Any()) {
                    var folders = configOpts.FilesOrFolders.Select(Path.GetDirectoryName).Distinct().ToList();
                    
                    var folderString = folders.ToDelimitedString("\n \t", delimitAfterLast: true);
                    PrintLn("Looking under: ".Green());
                    PrintLn($"\t{folderString}".White());
                }

                ConfigurationProvider.GenerateConfigurationFiles(
                    possibleOutputFile: configOpts.Output,
                    inputFiles: configOpts.FilesOrFolders.ToArray(),
                    foldersWereGiven: configOpts.FoldersWereGiven,
                    schemaReaders: configOpts.SchemaReaders,
                    progress: ProgressReporter);
            }
        }
    }
}