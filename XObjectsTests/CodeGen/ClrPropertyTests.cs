using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Tests.Extensions;
using XObjects;

namespace Xml.Schema.Linq.Tests.CodeGen;

public class ClrPropertyTests: BaseTester
{
    [Test]
    public void TestSimpleTypeUnionMappingNames()
    {
        MockFileInfo akk = AllTestFiles.GetMockFileInfo(f => f.EndsWith("AkomaNtoso\\akomantoso30.xsd"));
        MockFileInfo akkConfig = AllTestFiles.GetMockFileInfo(f => f.EndsWith("AkomaNtoso\\akomantoso30.xsd.config"));
        var config = Configuration.Load(akkConfig.ToStreamReader());

        var schemaSet = Utilities.GetXmlSchemaSet(akk, AllTestFiles);
        var defaultSettings = schemaSet.ToDefaultMergedConfiguration(config).ToLinqToXsdSettings();
        var xsdConverter = new XsdToTypesConverter(defaultSettings);
        var mapping = xsdConverter.GenerateMapping(schemaSet);

        // the docDate has an attribute that is a simple type union of xsd:date & xsd:dateTime
        ClrTypeInfo? docDateElMapping = mapping.Types.Single(t => t.schemaName == "docDate");
        Assert.IsInstanceOf<ClrContentTypeInfo>(docDateElMapping);

        var docDateElMappingContentInfo = (ClrContentTypeInfo)docDateElMapping;

        var date1Attr = docDateElMappingContentInfo.Content.OfType<ClrPropertyInfo>()
            .Single(a => a.Origin == SchemaOrigin.Attribute && a.PropertyName == "date1");
        
        Assert.True(date1Attr.IsUnion);
        Assert.True(date1Attr.TypeReference.IsUnion);
        var fullClrTypeName = date1Attr.TypeReference.GetClrFullTypeName("global::", mapping.NameMappings, defaultSettings, out var typeName);

        Assert.True(fullClrTypeName == typeof(object).FullName);
        
        var simpleTypeClrDefName = date1Attr.TypeReference.GetSimpleTypeClrTypeDefName("global::", mapping.NameMappings);
        
        Assert.True(simpleTypeClrDefName.StartsWith(Constants.SimpleTypeUnionOfPrefix.ToLowerCaseFirstChar()));
        
        Assert.True(date1Attr.TypeReference.SchemaObject is XmlSchemaSimpleType);

        var union = date1Attr.TypeReference.SchemaObject as XmlSchemaSimpleType;
        Assert.IsNotNull(union);
        Assert.IsTrue(union!.Datatype!.Variety == XmlSchemaDatatypeVariety.Union);
        Assert.IsTrue(union.Content is XmlSchemaSimpleTypeUnion);
    }
}