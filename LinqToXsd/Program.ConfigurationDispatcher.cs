using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
                    Clr = "Xml.Schema.Linq"
                });

                var egConfigXmlFile = "exampleConfiguration.xml";

                var comment = new XComment("Add more of your own XML namespace to CLR namespace entries here");
                var namespacesXName = XName.Get(nameof(Namespaces), configNsUri.AbsoluteUri);
                var namespacesElements = exampleConfig.Untyped.Descendants(namespacesXName);
                var namespacesEl = namespacesElements.First();
                namespacesEl.Add(comment);

                Console.WriteLine($"Saving to: {egConfigXmlFile}");
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
                    Console.WriteLine($"Folders were given to the CLI; --{nameof(ConfigurationOptions.Output)} argument is ignored.");

                var inputFiles = configOpts.SchemaFiles.ToArray();
                var schemaReaders = configOpts.SchemaReaders;

                if (foldersWereGiven) {
                    foreach (var xsd in schemaReaders) {
                        var schemaDoc = XDocument.Load(xsd.Value);
                        var config = Configuration.LoadForSchema(schemaDoc);

                        var outputConfig = xsd.Key.AppendIfNotPresent(".config");
                        Console.WriteLine($"Saving {outputConfig.Except(Environment.CurrentDirectory)}");
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
                Console.WriteLine($"Saving to {output}...");
                mergedOutput.Save(output);
            }
        }
    }
}