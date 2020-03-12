using System;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using W3C.XSD;

namespace Xml.Schema.Linq.Tests.API
{
    public class XTypedServicesTests
    {
        [Test]
        public void TestAnyAtomicTypeConversionToTWhereTIsString()
        {
            var xAttribute = new XAttribute(XName.Get("value"), "abcdef");
            Assert.DoesNotThrow(() => {
                var d = XTypedServices.ParseValue<string>(xAttribute, XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.AnyAtomicType).Datatype);

                Assert.IsTrue(d.GetTypeCode() == TypeCode.String);
                Assert.IsTrue(d == "abcdef");
            });
        }
        
        [Test]
        public void TestAnyAtomicTypeConversionFromString()
        {
            Assert.DoesNotThrow(() => {
                var ncName = new localSimpleType() {
                    restriction = new restriction() {
                        enumeration = {
                            new enumeration(new noFixedFacet() { value = "default" }),
                            new enumeration(new noFixedFacet() { value = "preserve" })
                        }
                    }
                };

                var str = XTypedServices.GetXmlString("default",
                    XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.AnyAtomicType).Datatype, XElement.Parse("<element />"));
            });
        }

        [Test]
        public void TestBooleanParsing()
        {
            var xAttributeTrue = new XAttribute(XName.Get("fixed", ""), "true");
            var xAttributeFalse = new XAttribute(XName.Get("fixed", ""), "false");

            var trueResult = XTypedServices.ParseValue<bool>(xAttributeTrue, XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean).Datatype);
            var falseResult = XTypedServices.ParseValue<bool>(xAttributeFalse, XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean).Datatype);

            Assert.IsTrue(trueResult);
            Assert.IsFalse(falseResult);
        }
    }
}