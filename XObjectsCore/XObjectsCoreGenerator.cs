using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Microsoft.CSharp;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq
{
    public static class XObjectsCoreGenerator
    {
        /// <summary>
        /// Creates a new instance of <see cref="LinqToXsdSettings"/>, optionally by loading from an XML file.
        /// </summary>
        /// <param name="fromXmlFile">Null or empty value will simply return a default instance.</param>
        /// <returns></returns>
        private static LinqToXsdSettings LoadLinqToXsdSettings(string fromXmlFile = null)
        {
            var settings = new LinqToXsdSettings();
            if (fromXmlFile.IsNotEmpty()) settings.Load(fromXmlFile);

            return settings;
        }

        /// <summary>
        /// Generates code for a sequence of file paths and an optional file path to an XML configuration file.
        /// </summary>
        /// <param name="xsdFilePaths"></param>
        /// <param name="linqToXsdSettingsFilePath"></param>
        /// <returns></returns>
        public static TextWriter Generate(IEnumerable<string> xsdFilePaths, string linqToXsdSettingsFilePath = null)
        {
            var xsdReaders = xsdFilePaths
                             .ToList().Select(file =>
                                 XmlReader.Create(file, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse }));

            var xmlSet = xsdReaders.ToXmlSchemaSet();
            var settings = LoadLinqToXsdSettings(linqToXsdSettingsFilePath);

            return Generate(xmlSet, settings);
        }

        /// <summary>
        /// Generates code using a given <see cref="xsdFilePath"/>, and an optionally, the file path to an 
        /// </summary>
        /// <param name="xsdFilePath"></param>
        /// <param name="linqToXsdSettingsFilePath"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
        public static TextWriter Generate(string xsdFilePath, string linqToXsdSettingsFilePath = null)
        {
            if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
            var settings = LoadLinqToXsdSettings(linqToXsdSettingsFilePath);

            return Generate(xsdFilePath, settings);
        }

        /// <summary>
        /// Generates code using a given <see cref="xsdFilePath"/>, and an optional <see cref="LinqToXsdSettings"/> instance. 
        /// </summary>
        /// <param name="xsdFilePath"></param>
        /// <param name="settings">If null, uses default or </param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
        public static TextWriter Generate(string xsdFilePath, LinqToXsdSettings settings = null)
        {
            if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
            if (settings == null) settings = new LinqToXsdSettings();

            var xmlReaderSettings = new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Parse
            };
            var xmlReader = XmlReader.Create(File.OpenRead(xsdFilePath), xmlReaderSettings);

            return Generate(xmlReader, settings);
        }

        /// <summary>
        /// Generates code using a given <paramref name="xmlReader"/> and a <see cref="LinqToXsdSettings"/> instance.
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="xmlReader"/> is <see langword="null"/></exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="settings"/> is <see langword="null"/></exception>
        public static TextWriter Generate(XmlReader xmlReader, LinqToXsdSettings settings)
        {
            if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var schemaSet = new XmlSchemaSet();
            schemaSet.Add(null, xmlReader);
            return Generate(schemaSet, settings);
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
            if (schemaSet == null) throw new ArgumentNullException(nameof(schemaSet));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var xsdConverter = new XsdToTypesConverter(settings);
            var mapping = xsdConverter.GenerateMapping(schemaSet);

            var codeGenerator = new CodeDomTypesGenerator(settings);
            var ccu = new CodeCompileUnit();
            foreach(var codeNs in codeGenerator.GenerateTypes(mapping)) 
                ccu.Namespaces.Add(codeNs);

            var stringWriter = new StringWriter();

            var provider = new CSharpCodeProvider();
            var codeGeneratorOptions = new CodeGeneratorOptions();
            provider.GenerateCodeFromCompileUnit(ccu, stringWriter, codeGeneratorOptions);

            return stringWriter;
        }

        /// <summary>
        /// Generates code from a given <paramref cref="schemaSet"/> and <paramref name="settings"/> then writes to the given
        /// <paramref name="outputFilePath"/>.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="settings"></param>
        public static void GenerateToFile(XmlSchemaSet schemaSet, LinqToXsdSettings settings, string outputFilePath)
        {
            var writer = Generate(schemaSet, settings);

            var sb = new StringBuilder();
            sb.Append(writer);

            using (var outputFileStream = File.Open(outputFilePath, FileMode.Create))
            {
                using (var fileWriter = new StreamWriter(outputFileStream))
                {
                    fileWriter.Write(sb);
                }
            }
        }
    }
}