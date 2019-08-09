using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xml.Schema.Linq.Extensions;

// ReSharper disable once CheckNamespace
namespace Xml.Schema.Linq
{
    internal partial class Configuration
    {
        /// <summary>
        /// Adds helpful XML comments if there are <see cref="Xml.Schema.Linq.Namespaces" /> and <see cref="Namespace"/> elements present,
        /// and returns the current <see cref="Configuration"/> instance as a regular <see cref="XDocument"/> which can then be output
        /// to a file so users can understand the configuration elements.
        /// </summary>
        /// <returns></returns>
        public XDocument AddHelpfulComments()
        {
            var doc = new XDocument(Untyped);

            var visibilityComment = new XComment("The @DefaultVisibility attribute controls the access modifier on generated class code. " +
                                                 "Only 'public' and 'internal' are acceptable values. Unrecognised values default to 'public'.");
            var moreNamespacesComment = new XComment("Add more of your own XML Schema-to-CLR namespace mapping elements here");

            var descendants = doc.Descendants().ToList();

            var namespacesElement = descendants.FirstOrDefault(d => d.Name.LocalName == nameof(Linq.Namespaces));
            namespacesElement?.Add(moreNamespacesComment);

            var firstNamespaceElement = descendants.FirstOrDefault(d => d.Name.LocalName == nameof(Namespace));
            firstNamespaceElement?.AddBeforeSelf(visibilityComment);

            return doc;
        }

        /// <summary>
        /// Saves the current instance without overwriting an existing file. Adds a number to the end of the file name.
        /// </summary>
        /// <param name="fileNameOrFullPath"></param>
        public void SaveNoOverwrite(string fileNameOrFullPath)
        {
            if (fileNameOrFullPath == null) throw new ArgumentNullException(nameof(fileNameOrFullPath));

            if (!Path.IsPathRooted(fileNameOrFullPath))
                fileNameOrFullPath = Path.GetFullPath(fileNameOrFullPath);

            var fileNameComponent = Path.GetFileName(fileNameOrFullPath);
            var initialFileName = fileNameComponent; // save for referencing when we find and replace at the end
            var initialDir = Path.GetDirectoryName(fileNameOrFullPath);

            if (initialDir.IsEmpty()) throw new InvalidOperationException();

            // ReSharper disable once AssignNullToNotNullAttribute
            // yes, this needs to be refresh using Path.Combine each iteration
            while (File.Exists(Path.Combine(initialDir, fileNameComponent))) {
                var splitFileName = fileNameComponent.Split('.');
                var firstHalf = splitFileName.First();
                firstHalf = firstHalf.AppendNumberToString();
                fileNameComponent = string.Join(".", firstHalf, splitFileName.Last());
            }

            var @out = fileNameOrFullPath.Replace(initialFileName, fileNameComponent);
            Save(@out);
        }

        /// <summary>
        /// Merges the namespaces present in another <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public Configuration MergeNamespaces(Configuration config)
        {
            foreach (var ns in config.Namespaces.Namespace)
                Namespaces.Namespace.Add(ns);

            Namespaces.Namespace = Namespaces.Namespace.Distinct(new NamespaceEqualityValueComparer()).ToList();

            return this;
        }

        /// <summary>
        /// Converts this instance to a legacy <see cref="LinqToXsdSettings"/> instance.
        /// </summary>
        /// <returns></returns>
        public LinqToXsdSettings ToLinqToXsdSettings()
        {
            var settings = new LinqToXsdSettings();
            settings.Load(new XDocument(Untyped));
            return settings;
        }

        /// <summary>
        /// Loads a new <see cref="Configuration"/> instance by reading an existing XSD schema and creating default values.
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        /// <exception cref="T:Xml.Schema.Linq.LinqToXsdException">Invalid XSD file; or it has no namespace declaration for W3C XML Schema.</exception>
        public static Configuration LoadForSchema(XDocument xDoc)
        {
            if (!xDoc.IsAnXmlSchema())
                throw new LinqToXsdException("This does not appear to be a valid XSD file. " +
                                             "It has no namespace declaration for W3C XML Schema.");
            if (xDoc.Root == null) throw new LinqToXsdException("Cannot parse this XSD file.");

            var egConfig = GetBlankConfigurationInstance();

            var namespaceAttrs = xDoc.Root.Attributes().Where(attr => attr.IsNamespaceDeclaration ||
                                                                      attr.Name == XName.Get("targetNamespace"))
                                     .ToArray();
            var theXsdNamespace = namespaceAttrs
                                  .Where(attr => attr.Name.LocalName == XName.Get("xs") &&
                                                 attr.Value == "http://www.w3.org/2001/XMLSchema").ToArray();

            var namespacesToRead = namespaceAttrs.Except(theXsdNamespace);
            foreach (var udn in namespacesToRead.Distinct(new XAttributeValueEqualityComparer())) {
                var uriToClrNamespaceValue =
                    Regex.Replace(udn.Value.Replace("https", "").Replace("http", ""), @"[\W]+", ".").Trim('.');
                var newNamespace = Namespace.New(udn.Value, uriToClrNamespaceValue);
                egConfig.Namespaces.Namespace.Add(newNamespace);
            }

            return egConfig;
        }

        /// <summary>
        /// Returns a new, default <see cref="Configuration"/> instance with no <see cref="Namespaces"/> present.
        /// </summary>
        /// <returns></returns>
        public static Configuration GetBlankConfigurationInstance()
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

        /// <summary>
        /// Returns an example <see cref="Configuration"/> instance with one <see cref="Namespace"/> element present.
        /// </summary>
        /// <returns></returns>
        public static Configuration GetExampleConfigurationInstance()
        {
            var blank = GetBlankConfigurationInstance();
            var newNamespace = Namespace.New("http://www.microsoft.com/xml/schema/linq", "Xml.Schema.Linq");
            blank.Namespaces.Namespace.Add(newNamespace);

            return blank;
        }
    }
}