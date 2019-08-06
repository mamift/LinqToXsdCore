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
                    Colors.WriteLine("A folder was provided, but no XSD files were found!".Red());
                    Colors.WriteLine("To generate a single example configuration file, specify an output file name.".Yellow());
                    return;
                }

                var egConfig = ConfigurationProvider.ProvideExampleConfigurationXml();

                var egConfigXmlFile = "exampleConfiguration.xml";

                Colors.WriteLine($"Saving to: {egConfigXmlFile.White()}".Green());

                egConfig.Save(egConfigXmlFile);
            }

            /// <summary>
            /// Creates a configuration for use with LinqToXsd based on the namespaces found in XSD documents.
            /// </summary>
            /// <param name="configOpts"></param>
            internal static void HandleAutoGenConfig(ConfigurationOptions configOpts)
            {
                if (configOpts.FilesOrFolders.Any()) {
                    var folders = configOpts.FilesOrFolders.Select(Path.GetDirectoryName).Distinct().ToList();
                    if (!folders.Any()) {
                        Colors.WriteLine("No XSD files found.");
                        goto gen;
                    }

                    var folderString = folders.ToDelimitedString("\r\n \t", true);
                    Colors.WriteLine("Looking under: ".White());
                    Colors.WriteLine($"\t{folderString}".Green());
                }

                gen:
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