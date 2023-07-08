using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq
{
    /// <summary>
    /// Static methods to support multiple ways of generating code.
    /// </summary>
    public static class XObjectsCoreGenerator
    {
        /// <summary>
        /// Creates a new instance of <see cref="LinqToXsdSettings"/>, optionally by loading from an XML file.
        /// </summary>
        /// <param name="fromXmlFile">Null or empty value will simply return a default instance.</param>
        /// <returns></returns>
        public static LinqToXsdSettings LoadLinqToXsdSettings(string fromXmlFile = null)
        {
            var settings = new LinqToXsdSettings();
            if (fromXmlFile.IsNotEmpty()) settings.Load(fromXmlFile);

            return settings;
        }

        /// <summary>
        /// Creates a new instance of <see cref="LinqToXsdSettings"/> from an already parsed <see cref="XDocument"/>.
        /// </summary>
        /// <returns></returns>
        public static LinqToXsdSettings LoadLinqToXsdSettings(XDocument xdoc)
        {
            var settings = new LinqToXsdSettings();
            settings.Load(xdoc);

            return settings;
        }

        /// <summary>
        /// Generates code for a sequence of file paths and an instance of a <see cref="LinqToXsdSettings"/>.
        /// </summary>
        /// <param name="xsdFilePaths"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static Dictionary<string, TextWriter> Generate(IEnumerable<string> xsdFilePaths, LinqToXsdSettings settings)
        {
            if (xsdFilePaths == null) throw new ArgumentNullException(nameof(xsdFilePaths));

            return xsdFilePaths
                .SelectMany(file => Generate(file, settings))
                // Multiple XSD files may import the same namespace, e.g. in case of a shared schema.
                // In this case we arbitrary keep the first occurence.
                .Distinct(new FileNameComparer())
                .ToDictionary(x => x.filename, x => x.writer);
        }

        /// <summary>
        /// Generates code using a given <see cref="xsdFilePath"/>, and an optionally, the file path to a configuration file.
        /// </summary>
        /// <param name="xsdFilePath"></param>
        /// <param name="linqToXsdSettingsFilePath"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
        public static IEnumerable<(string filename, TextWriter writer)> Generate(string xsdFilePath, string linqToXsdSettingsFilePath = null)
        {
            if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
            var settings = LoadLinqToXsdSettings(linqToXsdSettingsFilePath);

            return Generate(xsdFilePath, settings);
        }

        /// <summary>
        /// Generates code using a given <see cref="xsdFilePath"/> for a single file, and an optional <see cref="LinqToXsdSettings"/> instance.
        /// </summary>
        /// <param name="xsdFilePath"></param>
        /// <param name="settings">If null, uses default or </param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
        public static IEnumerable<(string filename, TextWriter writer)> Generate(string xsdFilePath, LinqToXsdSettings settings = null)
        {
            if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
            if (settings == null) settings = new LinqToXsdSettings();

            var xmlReader = XmlReader.Create(xsdFilePath, new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Parse,
                CloseInput = true
            });

            using (xmlReader) {
                var schemaSet = xmlReader.ToXmlSchemaSet();

                var xsdFolder = Path.GetDirectoryName(xsdFilePath);

                return Generate(schemaSet, settings)
                    .Select(x =>
                    {
                        // When SplitCodeFiles by Namespace is configured,
                        // Generate() returns a tuple (clrNamespace, writer) per CLR namespace that we need to map to a file.
                        // Otherwise, Generate() returns a single (null, writer) tuple.
                        var filename = string.IsNullOrEmpty(x.clrNamespace)
                            ? xsdFilePath
                            : settings.NamespaceFileMap.TryGetValue(x.clrNamespace, out string nsFile)
                            ? Path.Combine(xsdFolder, nsFile)
                            : throw new LinqToXsdException(
                                $"CLR namespace {x.clrNamespace} has no output file configured, which is required when using SplitCodeFiles by Namespace configuration.");

                        return (filename, x.writer);
                    });
            }
        }

        /// <summary>
        /// Generates code using a given <paramref name="schemaSet"/> of XSDs.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <returns>
        ///     A single (null, StringWriter) when configuration doesn't split files per namespace.
        ///     Otherwise, one StringWriter per CLR namespace.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
        public static IEnumerable<(string clrNamespace, TextWriter writer)> Generate(XmlSchemaSet schemaSet)
        {
            var settings = new LinqToXsdSettings();
            return Generate(schemaSet, settings);
        }

        /// <summary>
        /// Generates code using a given <paramref name="schemaSet"/> of XSD's and a <see cref="LinqToXsdSettings"/> instance.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <param name="settings"></param>
        /// <returns>
        ///     A single (null, StringWriter) when configuration doesn't split files per namespace.
        ///     Otherwise, one StringWriter per CLR namespace.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="settings"/> is <see langword="null"/></exception>
        public static IEnumerable<(string clrNamespace, TextWriter writer)> Generate(XmlSchemaSet schemaSet, LinqToXsdSettings settings)
        {
            return GenerateCodeCompileUnits(schemaSet, settings)
                .Select(x =>
                {
                    var writer = x.unit.ToStringWriter();

                    if (settings.NullableReferences)
                    {
                        // HACK: CodeDom doesn't allow us to add #pragmas.
                        // In <auto-generated> code, CS mandates a "#nullable enable" pragma.
                        // So we add the pragma inside the generated text directly
                        writer.InsertFilePragma("#nullable enable annotations");
                    }

                    return (x.clrNamespace, (TextWriter)writer);
                });
        }

        /// <summary>
        /// Creates <see cref="CodeCompileUnit"/>s from a given <see cref="XmlSchemaSet"/> and <see cref="LinqToXsdSettings"/>.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <param name="settings"></param>
        /// <returns>
        ///     A single (null, CodeCompileUnit) when configuration doesn't split files per namespaces.
        ///     Otherwise, one CodeCompileUnit per CLR namespace.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="settings"/> is <see langword="null"/></exception>
        public static IEnumerable<(string clrNamespace, CodeCompileUnit unit)> GenerateCodeCompileUnits(XmlSchemaSet schemaSet, LinqToXsdSettings settings)
        {
            if (schemaSet == null) throw new ArgumentNullException(nameof(schemaSet));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var xsdConverter = new XsdToTypesConverter(settings);
            var mapping = xsdConverter.GenerateMapping(schemaSet);

            var codeGenerator = new CodeDomTypesGenerator(settings);
            var namespaces = codeGenerator.GenerateTypes(mapping);

            return settings.SplitFilesByNamespace
                ? namespaces.GroupBy(ns => ns.Name).Select(g => (g.Key, BuildUnit(g)))
                : new[] { ((string)null, BuildUnit(namespaces)) };

            static CodeCompileUnit BuildUnit(IEnumerable<CodeNamespace> namespaces)
            {
                var ccu = new CodeCompileUnit();
                foreach (var ns in namespaces)
                    ccu.Namespaces.Add(ns);
                return ccu;
            }
        }

        /// <summary>
        /// Generates code by searching for an accompanying configuration file, whereby each configuration file is named the same as the XSD file, but with an
        /// .config extension (i.e. schemaFileName.xsd.config). Will skip over XSDs that have no accompanying .config file.
        /// </summary>
        /// <param name="schemaFiles"></param>
        /// <returns></returns>
        public static Dictionary<string, TextWriter> Generate(IEnumerable<string> schemaFiles)
        {
            // xsd file paths are keys, the FileInfo's to their config files are values
            var dictOfSchemasAndTheirConfigs = schemaFiles.Select(s => new KeyValuePair<string, FileInfo>(s,
                                                               new FileInfo($"{s}.config")))
                                                           .ToDictionary(k => k.Key, v => v.Value);
            return dictOfSchemasAndTheirConfigs
                .Where(kvp => kvp.Value.Exists)
                .SelectMany(kvp => Generate(kvp.Key, kvp.Value.FullName))
                // Multiple XSD files may import the same namespace, e.g. in case of a shared schema.
                // In this case we arbitrary keep the first occurence.
                .Distinct(new FileNameComparer())
                .ToDictionary(x => x.filename, x => x.writer);
        }

        class FileNameComparer : IEqualityComparer<(string filename, TextWriter writer)>
        {
            public bool Equals((string filename, TextWriter writer) x, (string filename, TextWriter writer) y)
                => x.filename == y.filename;

            public int GetHashCode((string filename, TextWriter writer) obj)
                => obj.filename?.GetHashCode() ?? 0;
        }
    }
}