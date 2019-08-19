using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Microsoft.CSharp;
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
        /// Generates code for a sequence of file paths and an instance of a <see cref="LinqToXsdSettings"/>.
        /// </summary>
        /// <param name="xsdFilePaths"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static Dictionary<string, TextWriter> Generate(IEnumerable<string> xsdFilePaths, LinqToXsdSettings settings)
        {
            if (xsdFilePaths == null) throw new ArgumentNullException(nameof(xsdFilePaths));
            
            var textWriters = xsdFilePaths.Select(file => Generate(file, settings));

            return textWriters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Generates code using a given <see cref="xsdFilePath"/>, and an optionally, the file path to a configuration file.
        /// </summary>
        /// <param name="xsdFilePath"></param>
        /// <param name="linqToXsdSettingsFilePath"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
        public static KeyValuePair<string, TextWriter> Generate(string xsdFilePath, string linqToXsdSettingsFilePath = null)
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
        public static KeyValuePair<string, TextWriter> Generate(string xsdFilePath, LinqToXsdSettings settings = null)
        {
            if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
            if (settings == null) settings = new LinqToXsdSettings();

            var xmlReader = XmlReader.Create(xsdFilePath, new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Parse,
                CloseInput = true
            });

            using (xmlReader) {
                var schemaSet = xmlReader.ToXmlSchemaSet();

                var codeWriter = Generate(schemaSet, settings);

                return new KeyValuePair<string, TextWriter>(xsdFilePath, codeWriter);
            }
        }

        /// <summary>
        /// Generates code using a given <paramref name="schemaSet"/> of XSD's and a <see cref="LinqToXsdSettings"/> instance.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="settings"/> is <see langword="null"/></exception>
        public static TextWriter Generate(XmlSchemaSet schemaSet, LinqToXsdSettings settings)
        {
            var ccu = GenerateCodeCompileUnit(schemaSet, settings);

            return ccu.ToStringWriter();
        }

        /// <summary>
        /// Creates a <see cref="CodeCompileUnit"/> from a given <see cref="XmlSchemaSet"/> and <see cref="LinqToXsdSettings"/>.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="settings"/> is <see langword="null"/></exception>
        public static CodeCompileUnit GenerateCodeCompileUnit(XmlSchemaSet schemaSet, LinqToXsdSettings settings)
        {
            if (schemaSet == null) throw new ArgumentNullException(nameof(schemaSet));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var xsdConverter = new XsdToTypesConverter(settings);
            var mapping = xsdConverter.GenerateMapping(schemaSet);

            var codeGenerator = new CodeDomTypesGenerator(settings);
            var ccu = new CodeCompileUnit();
            foreach(var codeNs in codeGenerator.GenerateTypes(mapping)) 
                ccu.Namespaces.Add(codeNs);

            return ccu;
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
            var textWriters =
                dictOfSchemasAndTheirConfigs.Where(kvp => kvp.Value.Exists).Select(kvp =>
                    Generate(kvp.Key, kvp.Value.FullName));

            return textWriters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}