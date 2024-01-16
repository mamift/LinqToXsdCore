using LinqToXsd.Schemas.Test.NilTest;
using NUnit.Framework;
using System;
using System.IO;
using System.Xml.Linq;

namespace Xml.Schema.Linq.Tests
{
    public class XsiNilTests
    {
        [Test]
        public void Serialize()
        {
            var doc = new Root
            {
                OptionalEl = null,
                OptionalRef = null,
                OptionalVal = null,
                RequiredEl = null,
                RequiredRef = null,
                RequiredVal = null,
                ListEl = { null },
                ListRef = { null },
                ListVal = { null },
            };
            // Add root namespace prefix for predictability.
            // It works without this, but there's a namespace declaration on each child element,
            // with somewhat unpredictable names.
            doc.Untyped.Add(new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"));

            var writer = new StringWriter();
            doc.Save(writer);
            var xml = writer.ToString();
            
            Assert.IsTrue(xml.Contains("<OptionalEl xsi:nil=\"true\" />"), "OptionalEl incorrectly serialized");
            Assert.IsTrue(xml.Contains("<OptionalRef xsi:nil=\"true\" />"), "OptionalRef incorrectly serialized");
            Assert.IsTrue(xml.Contains("<OptionalVal xsi:nil=\"true\" />"), "OptionalVal incorrectly serialized");
            Assert.IsTrue(xml.Contains("<RequiredEl xsi:nil=\"true\" />"), "RequiredEl incorrectly serialized");
            Assert.IsTrue(xml.Contains("<RequiredRef xsi:nil=\"true\" />"), "RequiredRef incorrectly serialized");
            Assert.IsTrue(xml.Contains("<RequiredVal xsi:nil=\"true\" />"), "RequiredVal incorrectly serialized");
            Assert.IsTrue(xml.Contains("<ListEl xsi:nil=\"true\" />"), "ListEl incorrectly serialized");
            Assert.IsTrue(xml.Contains("<ListRef xsi:nil=\"true\" />"), "ListRef incorrectly serialized");
            Assert.IsTrue(xml.Contains("<ListVal xsi:nil=\"true\" />"), "ListVal incorrectly serialized");
        }
    }
}
