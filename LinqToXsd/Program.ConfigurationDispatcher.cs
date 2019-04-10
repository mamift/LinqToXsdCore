using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                var exampleConfig = new Configuration {
                    Namespaces = new Namespaces {
                        Namespace = new List<Namespace> {
                            new Namespace {
                                Schema = new Uri("http://www.microsoft.com/xml/schema/linq"),
                                Clr = "Xml.Schema.Linq"
                            }
                        }
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
                var xsdFileReaders = configOpts.SchemaReaders;

                foreach (var xsd in xsdFileReaders)
                {
                    var xDoc = XDocument.Load(xsd);
                    var rootElement = 
                }
            }
        }
    }
}