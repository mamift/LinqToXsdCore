using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xml.Schema.Linq;

namespace LinqToXsd
{
    public static partial class Program
    {
        internal struct ConfigurationDispatcher
        {
            /// <summary>
            /// Outputs an example configuration file for use with LinqToXsd.
            /// </summary>
            internal static void HandleGenerateExampleConfig()
            {
                var exampleConfig = ProvideExampleConfig();
                exampleConfig.Namespaces.Namespace.Add(new Namespace {
                    Schema = new Uri("http://www.microsoft.com/xml/schema/linq"),
                    Clr = "Xml.Schema.Linq"
                });

                var egConfigXmlFile = "exampleConfiguration.xml";
                while (File.Exists(egConfigXmlFile))
                {
                    var strings = egConfigXmlFile.Split('.');
                    var firstHalf = strings.First();
                    firstHalf = firstHalf.AppendNumberToString();
                    egConfigXmlFile = string.Join(".", firstHalf, strings.Last());
                }

                Console.WriteLine($"Saving to: {egConfigXmlFile}");
                exampleConfig.Save(egConfigXmlFile);
            }

            /// <summary>
            /// Creates a configuration for use with LinqToXsd based on the namespaces found in XSD documents.
            /// </summary>
            /// <param name="configOpts"></param>
            public static void HandleAutoGenConfig(ConfigurationOptions configOpts)
            {
                var egConfig = ProvideExampleConfig();

                foreach (var xsd in configOpts.SchemaReaders)
                {
                    var xDoc = XDocument.Load(xsd.Value);
                    if (xDoc.Root == null)
                    {
                        Console.WriteLine($"Cannot parse this XML file: {xsd.Key}");
                        continue;
                    }

                    var namespaceAttrs = xDoc.Root.Attributes().Where(attr => attr.IsNamespaceDeclaration).ToArray();
                    var theXsdNamespace = namespaceAttrs
                                          .Where(attr => attr.Value == "http://www.w3.org/2001/XMLSchema").ToArray();
                    var hasXmlnsForXsd = theXsdNamespace.Any();
                    if (!hasXmlnsForXsd)
                        Console.WriteLine("This does not appear to be a valid XSD file. It has no namespace declaration for W3C XML Schema.");

                    var nsComparer = new XAttributeNamespaceValueEqualityComparer();
                    foreach (var udn in namespaceAttrs.Except(theXsdNamespace).Distinct(nsComparer))
                    {
                        var unmangleUriToClrNs = Regex.Replace(udn.Value.Replace("https", "").Replace("http", ""), @"[\W]+", ".").Trim('.');
                        egConfig.Namespaces.Namespace.Add(new Namespace {
                            Schema = new Uri(udn.Value),
                            Clr = unmangleUriToClrNs
                        });
                    }
                }

                var output = $"{configOpts.SchemaFiles.First()}.config";
                egConfig.SaveNoOverwrite(output);
            }

            /// <summary>
            /// Returns a new, skeletal <see cref="Configuration"/> instance.
            /// </summary>
            /// <returns></returns>
            private static Configuration ProvideExampleConfig()
            {
                return new Configuration {
                    Namespaces = new Namespaces {
                        Namespace = new List<Namespace>()
                    },
                    Transformation = new Transformation {
                        Deanonymize = new Deanonymize {
                            strict = false
                        }
                    },
                    Validation = new Validation {
                        VerifyRequired = new VerifyRequired(false)
                    }
                };
            }
        }
    }
}