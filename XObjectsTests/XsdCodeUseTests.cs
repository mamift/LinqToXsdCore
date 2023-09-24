using System.Linq;
using NUnit.Framework;
using W3C.XSD;

namespace Xml.Schema.Linq.Tests;

public class XsdCodeUseTests: BaseTester
{
    [Test]
    public void SchemaLangUseTest()
    {
        var filename = AllTestFiles.AllFiles.FirstOrDefault(f => f.Contains("XSD") && f.EndsWith("W3C XMLSchema v1.xsd"));
        var xsdFile = AllTestFiles.FileInfo.New(filename);
        var xsdFileXmlText = xsdFile.OpenText().ReadToEnd();
        var d = schema.Parse(xsdFileXmlText);
    }
}