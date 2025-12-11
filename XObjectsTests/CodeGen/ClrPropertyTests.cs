using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Tests.Extensions;
using XObjects;

namespace Xml.Schema.Linq.Tests.CodeGen;

public class ClrPropertyTests: BaseTester
{
    [Test]
    public void T1()
    {
        MockFileInfo abstractTestXsd = AllTestFiles.GetMockFileInfo(f => f.EndsWith("abstracttest.xsd"));
        MockFileInfo abstractTestXsdConfig = AllTestFiles.GetMockFileInfo(f => f.EndsWith("abstracttest.xsd.config"));
        var config = Configuration.Load(abstractTestXsdConfig.ToStreamReader());

        var schemaSet = Utilities.GetXmlSchemaSet(abstractTestXsd, AllTestFiles);
        var defaultSettings = schemaSet.ToDefaultMergedConfiguration(config).ToLinqToXsdSettings();
        var xsdConverter = new XsdToTypesConverter(defaultSettings);
        var mapping = xsdConverter.GenerateMapping(schemaSet);

        var codeGenerator = new CodeDomTypesGenerator(defaultSettings);
        var namespaces = codeGenerator.GenerateTypes(mapping).ToList();

        Assert.NotNull(mapping);

        foreach (var type in mapping.Types) {
            
        }
    }
}