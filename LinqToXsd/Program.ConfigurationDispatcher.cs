using System;
using System.Linq;
using System.Xml.Linq;
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
            internal static void HandleGenerateExampleConfig()
            {
                var egConfig = ConfigurationProvider.ProvideExampleConfigurationXml();

                var egConfigXmlFile = "exampleConfiguration.xml";

                Colors.WriteLine($"Saving to: {egConfigXmlFile.White()}".Green());

                egConfig.Save(egConfigXmlFile);
            }

            /// <summary>
            /// Creates a configuration for use with LinqToXsd based on the namespaces found in XSD documents.
            /// </summary>
            /// <param name="configOpts"></param>
            public static void HandleAutoGenConfig(ConfigurationOptions configOpts)
            {
                ConfigurationProvider.GenerateConfigurationFiles(
                    configOpts.Output,
                    configOpts.FilesOrFolders.ToArray(),
                    configOpts.FoldersWereGiven,
                    configOpts.SchemaReaders,
                    ProgressReporter);
            }
        }
    }
}