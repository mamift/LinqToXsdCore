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
                var exampleConfig = Configuration.ExampleConfigurationInstance;
                var configNsUri = new Uri("http://www.microsoft.com/xml/schema/linq");
                exampleConfig.Namespaces.Namespace.Add(new Namespace {
                    Schema = configNsUri,
                    Clr = "Xml.Schema.Linq",
                    //DefaultVisibility = "public"
                });

                var egConfigXmlFile = "exampleConfiguration.xml";

                var comment = new XComment("Add more of your own XML namespace to CLR namespace entries here");
                var namespacesXName = XName.Get(nameof(Namespaces), configNsUri.AbsoluteUri);
                var namespacesElements = exampleConfig.Untyped.Descendants(namespacesXName);
                var namespacesEl = namespacesElements.First();
                namespacesEl.Add(comment);

                Colors.WriteLine($"Saving to: {egConfigXmlFile}".Green());
                exampleConfig.Save(egConfigXmlFile);
            }

            /// <summary>
            /// Creates a configuration for use with LinqToXsd based on the namespaces found in XSD documents.
            /// </summary>
            /// <param name="configOpts"></param>
            public static void HandleAutoGenConfig(ConfigurationOptions configOpts)
            {
                var egConfig = Configuration.ExampleConfigurationInstance;
                var outputWasGiven = configOpts.Output.IsNotEmpty();
                var foldersWereGiven = configOpts.FoldersWereGiven;

                if (foldersWereGiven && outputWasGiven)
                    Colors.WriteLine($"Folders were given to the CLI; --{nameof(ConfigurationOptions.Output)} argument is ignored.".Gray());

                var inputFiles = configOpts.SchemaFiles.ToArray();
                var schemaReaders = configOpts.SchemaReaders;

                if (foldersWereGiven) {
                    foreach (var xsd in schemaReaders) {
                        var schemaDoc = XDocument.Load(xsd.Value);
                        var config = Configuration.LoadForSchema(schemaDoc);

                        var outputConfig = xsd.Key.AppendIfNotPresent(".config");
                        Colors.WriteLine($"Saving {outputConfig.Except(Environment.CurrentDirectory)}".Green());
                        config.Save(outputConfig);
                    }

                    return;
                }

                var mergedOutput = schemaReaders.Aggregate(egConfig, (theEgConfig, pair) => {
                    var loadedForXsd = Configuration.LoadForSchema(XDocument.Load(pair.Value));
                    return theEgConfig.MergeNamespaces(loadedForXsd);
                });

                // use a generic filename if multiple inputs were given
                var outputFileName = inputFiles.Length > 1 ? "LinqToXsdConfig.config" : inputFiles.First();

                var outputFile = outputWasGiven ? configOpts.Output : outputFileName;
                var output = outputFile.AppendIfNotPresent(".config");
                Colors.WriteLine($"Saving to {output}...".Green());
                mergedOutput.Save(output);
            }
        }
    }
}