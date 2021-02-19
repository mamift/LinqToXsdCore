using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Resolvers;
using System.Xml.Schema;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    public static class Utilities
    {
        public static string WarningMessage(object expected, object actual, [CallerMemberName] string caller = "")
        {
            return caller + "() failed; expected " + expected + ", got " + actual;
        }

        /// <summary>
        /// Used specifically for unit testing, invokes the
        /// <see cref="XObjectsCoreGenerator.Generate(IEnumerable{string},LinqToXsdSettings)"/>
        /// method for generating C# code.
        /// </summary>
        /// <param name="xsdFileName"></param>
        /// <returns></returns>
        public static SourceText GenerateSourceText(string xsdFileName)
        {
            var possibleSettingsFile = $"{xsdFileName}.config";
            KeyValuePair<string, TextWriter> code = File.Exists(possibleSettingsFile)
                ? XObjectsCoreGenerator.Generate(xsdFileName, possibleSettingsFile)
                : XObjectsCoreGenerator.Generate(xsdFileName, default(string));

            return SourceText.From(code.Value.ToString());
        }

        /// <summary>
        /// Used specifically for unit testing, invokes the
        /// <see cref="XObjectsCoreGenerator.Generate(XmlSchemaSet,LinqToXsdSettings)"/>
        /// method for generating C# code.
        /// </summary>
        /// <param name="xmlSchemaSet"></param>
        /// <param name="xsdFileName">Required for loading any configuration files. Accepts relative and absolute.</param>
        /// <returns></returns>
        public static SourceText GenerateSourceText(XmlSchemaSet xmlSchemaSet, string xsdFileName)
        {
            var possibleSettingsFile = $"{xsdFileName}.config";
            Configuration config = File.Exists(possibleSettingsFile)
                ? Configuration.Load(possibleSettingsFile)
                : Configuration.GetBlankConfigurationInstance();
            var settings = config.ToLinqToXsdSettings();

            var code = XObjectsCoreGenerator.Generate(xmlSchemaSet, settings);

            return SourceText.From(code.ToString());
        }

        /// <summary>
        /// Creates an <see cref="XmlSchemaSet"/> from the given <see cref="FileInfo"/> <paramref name="xsdFile"/>.
        /// </summary>
        /// <param name="xsdFile"></param>
        /// <returns></returns>
        public static XmlSchemaSet CompileXmlSchemaSet(FileInfo xsdFile)
        {
            if (xsdFile == null) throw new ArgumentNullException(nameof(xsdFile));
            if (!xsdFile.Extension.EndsWith("xsd")) throw new InvalidOperationException("Must be invoked on an XSD file!");

            var folderWithAdditionalXsdFiles = xsdFile.DirectoryName;
            var directoryInfo = new DirectoryInfo(folderWithAdditionalXsdFiles);
            var additionalXsds = directoryInfo.GetFiles("*.xsd");

            var xmlPreloadedResolver = new XmlPreloadedResolver();

            foreach (var xsd in additionalXsds) {
                xmlPreloadedResolver.Add(new Uri($"file://{xsd.FullName}"), File.OpenRead(xsd.FullName));
            }

            var xmlReaderSettings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true
            };
            var xmlSchemaSet = XmlReader.Create(xsdFile.FullName, xmlReaderSettings)
                .ToXmlSchemaSet(xmlPreloadedResolver);

            return xmlSchemaSet;
        }

        /// <summary>
        /// Generates C# code from a given <paramref name="xsdFile"/> and then returns the <see cref="CSharpSyntaxTree"/> of
        /// the generated code.
        /// </summary>
        /// <param name="xsdFile"></param>
        /// <returns></returns>
        public static CSharpSyntaxTree GenerateSyntaxTree(FileInfo xsdFile)
        {
            var atomXsdSchemaSet = CompileXmlSchemaSet(xsdFile);

            var sourceText = GenerateSourceText(atomXsdSchemaSet, xsdFile.FullName);
            using var writer = new StreamWriter(xsdFile.FullName + ".cs");
            sourceText.Write(writer);

            var tree = CSharpSyntaxTree.ParseText(sourceText, CSharpParseOptions.Default);

            return tree as CSharpSyntaxTree;
        }
    }
}
