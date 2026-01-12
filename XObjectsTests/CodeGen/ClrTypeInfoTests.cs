using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using XObjects;

namespace Xml.Schema.Linq.Tests.CodeGen;

public class ClrTypeInfoTests: BaseTester
{
    [Test, TestCase("StuDateAndTime.xsd")]
    public void CreateSimpleTypeForAnonymousSimpleTypeUnion(string endsWithFilePattern)
    {
        var xsd = GetTestFileAsXmlSchemaSet(endsWithFilePattern);

        var anonUnionTypes = xsd.RetrieveAllAnonymousSimpleUnionTypes();

        Assert.NotNull(anonUnionTypes);

        foreach (var simpleType in anonUnionTypes) {
            var type = ClrSimpleTypeInfo.CreateSimpleTypeInfo(simpleType.Value);
        }
    }
}