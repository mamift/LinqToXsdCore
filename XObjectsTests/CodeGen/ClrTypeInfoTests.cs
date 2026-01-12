using System.CodeDom;
using System.Collections.Generic;
using System.Xml.Schema;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;
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
            Assert.IsInstanceOf<XmlSchemaSimpleTypeUnion>(simpleType.Value);

            ClrSimpleTypeInfo? type = ClrSimpleTypeInfo.CreateSimpleTypeInfo(simpleType.Value);

            Assert.NotNull(type);
            var unionTypeInfo = type as UnionSimpleTypeInfo;
            unionTypeInfo.clrtypeName = simpleType.Value.Name;
            Assert.True(unionTypeInfo != null);

            var typeDef = TypeBuilder.CreateSimpleType(unionTypeInfo, new Dictionary<XmlSchemaObject, string>(),
                new LinqToXsdSettings());

            string typeDefCodeStr = typeDef.ToCodeString();
            Assert.NotNull(typeDefCodeStr);

            CodeExpression? code1 = SimpleTypeCodeDomHelper.CreateSimpleTypeDef(unionTypeInfo, new Dictionary<XmlSchemaObject, string>(), new LinqToXsdSettings(), false);
            CodeExpression? code2 = SimpleTypeCodeDomHelper.CreateSimpleTypeDef(unionTypeInfo, new Dictionary<XmlSchemaObject, string>(), new LinqToXsdSettings(), true);

            string codeString1 = code1.ToCodeString();
            string codeString2 = code2.ToCodeString();

            Assert.NotNull(codeString1);
            Assert.NotNull(codeString2);

            Assert.IsTrue(codeString1 == codeString2);
        }
    }
}