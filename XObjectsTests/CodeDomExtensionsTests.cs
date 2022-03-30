using System;
using System.CodeDom;
using System.Xml.Schema;
using Fasterflect;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests;

[TestFixture]
public class CodeDomExtensionsTests
{
    [Test]
    public void TestIsEquivalentTypeReference()
    {
        var xmlSchemaElement = new XmlSchemaType() {
            Name = "test"
        };

        var exampleClrTypeRef = new ClrTypeReference(nameof(String), typeof(string).Namespace,
            xmlSchemaElement, false, false);

        exampleClrTypeRef.SetFieldValue("clrName", "String");
        exampleClrTypeRef.SetFieldValue("typeNs", "System");
        exampleClrTypeRef.SetFieldValue("clrFullTypeName", typeof(string).FullName);

        var codeTypeRef = new CodeTypeReference(typeof(string));

        var isEquivalent = exampleClrTypeRef.IsEquivalentTypeReference(codeTypeRef);

        Assert.True(isEquivalent);
    }
}