using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq
{
    public class ConfigurationProvider
    {
        /// <summary>
        /// Provides an example configuration XML.
        /// </summary>
        /// <returns></returns>
        public static XDocument ProvideExampleConfigurationXml()
        {
            var exampleConfig = Configuration.GetExampleConfigurationInstance();

            var comment = new XComment("Add more of your own XML namespace to CLR namespace entries here");
            var namespacesXName = XName.Get(nameof(Namespaces), exampleConfig.Namespaces.Namespace.First().Schema.AbsoluteUri);

            var exampleConfigUntyped = exampleConfig.Untyped;
            var namespacesElements = exampleConfigUntyped.Descendants(namespacesXName);
            var namespacesEl = namespacesElements.First();
            namespacesEl.Add(comment);

            return exampleConfigUntyped.Document;
        }

        /// <summary>
        /// Generates default configuration files for given input XSD files. This will write new files to the file system.
        /// </summary>
        /// <param name="possibleOutputFile"></param>
        /// <param name="inputFiles"></param>
        /// <param name="foldersWereGiven"></param>
        /// <param name="schemaReaders"></param>
        /// <param name="progress"></param>
        public static void GenerateConfigurationFiles(string possibleOutputFile, string[] inputFiles, bool foldersWereGiven,
            Dictionary<string, XmlReader> schemaReaders,
            IProgress<string> progress = null)
        {
            var egConfig = Configuration.GetExampleConfigurationInstance();
            var outputWasGiven = possibleOutputFile.IsNotEmpty();

            if (foldersWereGiven) {
                foreach (var xsd in schemaReaders) {
                    var schemaDoc = XDocument.Load(xsd.Value);
                    var config = Configuration.LoadForSchema(schemaDoc);

                    var outputConfig = xsd.Key.AppendIfNotPresent(".config");
                    progress?.Report($"Saving {outputConfig.Except(Environment.CurrentDirectory)}");
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

            var outputFile = outputWasGiven ? possibleOutputFile : outputFileName;
            var output = outputFile.AppendIfNotPresent(".config");
            progress?.Report($"Saving to {output}...");
            mergedOutput.Save(output);
        }

        /// <summary>
        /// Load configuration files (.xml, .config) from a <see cref="DirectoryInfo"/> and merge all the configuration instances
        /// into one and return it as a <see cref="LinqToXsdSettings"/> instance.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="progress"></param>
        /// <returns><c>null</c> if not configs are present in the <paramref name="directory"/>.</returns>
        public static LinqToXsdSettings Load(DirectoryInfo directory, IProgress<string> progress = null)
        {
            var configFiles = directory.EnumerateFiles("*", SearchOption.AllDirectories)
                                       .Where(f => f.Extension.EndsWith(".xml") || f.Extension.EndsWith(".config"))
                                       .ToArray();

            var configs = configFiles
                          .Select(f => { try { return Configuration.Load(f.FullName); } catch { return null; } })
                          .Where(c => c != null)
                          .ToArray();

            if (!configs.Any()) return null;
            progress?.Report($"Reading ({configs.Length}) configuration file(s) from: {directory.FullName}");
            var firstConfig = configs.First();
            if (configs.Length == 1) return firstConfig.ToLinqToXsdSettings();
            var configurationsToMerge = configs.Skip(1).ToArray();
            foreach (var config in configurationsToMerge)
                firstConfig.MergeNamespaces(config);

            progress?.Report($"Merged {configurationsToMerge.Length} + 1 configuration files...");
            return firstConfig.ToLinqToXsdSettings();
        }
    }
}