using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Microsoft.CSharp;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq
{
    public class XObjectsCoreGenerator
    {
        public static StringWriter Generate(XmlReader schemaReader, LinqToXsdSettings configuration)
        {
            var schemaSet = new XmlSchemaSet();
            schemaSet.Add(null, schemaReader);
            return Generate(schemaSet, configuration);
        }

        public static StringWriter Generate(XmlSchemaSet schemaSet, LinqToXsdSettings configuration)
        {
            var xsdConverter = new XsdToTypesConverter(configuration);
            var mapping = xsdConverter.GenerateMapping(schemaSet);

            var codeGenerator = new CodeDomTypesGenerator(configuration);
            var ccu = new CodeCompileUnit();
            foreach(var codeNs in codeGenerator.GenerateTypes(mapping)) 
                ccu.Namespaces.Add(codeNs);

            var stringWriter = new StringWriter();

            var provider = new CSharpCodeProvider();
            provider.GenerateCodeFromCompileUnit(ccu, stringWriter, new CodeGeneratorOptions());

            return stringWriter;
        }
    }
}