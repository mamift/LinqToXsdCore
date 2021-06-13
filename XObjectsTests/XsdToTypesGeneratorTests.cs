using System;
using System.IO;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq.Tests
{
    public class XsdToTypesGeneratorTests
    {
        [Test]
        public void TestMappingsGenerated()
        {
            var folder = Path.Combine(Environment.CurrentDirectory, @"Toy schemas\EnumsForElements");

            var xsds = new DirectoryInfo(folder).EnumerateFiles("*.xsd", SearchOption.AllDirectories);

            foreach (var xsd in xsds) {
                var schemaSet = Utilities.CompileXmlSchemaSet(xsd);
                var settingsForSchema = XObjectsCoreGenerator.LoadLinqToXsdSettings($"{xsd.FullName}.config");
                var xsdConverter = new XsdToTypesConverter(settingsForSchema);
                var mapping = xsdConverter.GenerateMapping(schemaSet);
                var ccu = XObjectsCoreGenerator.GenerateCodeCompileUnit(schemaSet, settingsForSchema);
            }
        }
    }
}