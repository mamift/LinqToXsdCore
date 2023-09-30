using System.Linq;
using NUnit.Framework;
using W3C.XSD;

namespace Xml.Schema.Linq.Tests;

public class XsdCodeUseTests: BaseTester
{
    private schema parsedXmlSchema;

    [SetUp]
    public void Setup()
    {
        var filename = AllTestFiles.AllFiles.FirstOrDefault(f => f.Contains("XSD") && f.EndsWith("W3C XMLSchema v1.xsd"));
        var xsdFile = AllTestFiles.FileInfo.New(filename);
        var xsdFileXmlText = xsdFile.OpenText().ReadToEnd();
        parsedXmlSchema = schema.Parse(xsdFileXmlText);
    }

    [Test]
    public void SchemaLangSetValueTest()
    {
        Assert.DoesNotThrow(() => {
            parsedXmlSchema.lang = "EN-AU";
        });
    }

    [Test]
    public void SchemaLangUseTest()
    {
        Assert.IsNotNull(parsedXmlSchema);

        Assert.IsNotNull(parsedXmlSchema.lang);
        Assert.True(parsedXmlSchema.lang is string);
    }
}