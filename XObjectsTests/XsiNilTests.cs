using LinqToXsd.Schemas.Test.NilTest;
using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Xml.Schema.Linq.Tests
{
    public class XsiNilTests
    {
        private static void AddGlobalXsiNamespace(XTypedElement element)
        {
            element.Untyped.Add(new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"));
        }

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
            AddGlobalXsiNamespace(doc);

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

        [Test]
        public void Deserialize()
        {
            var doc = Root.Parse("""
                <Root xmlns="http://linqtoxsd.schemas.org/test/nil-test.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                    <RequiredRef xsi:nil="true" />
                    <RequiredVal xsi:nil="true" />
                    <RequiredEl xsi:nil="true" />
                    <OptionalRef xsi:nil="true" />
                    <OptionalVal xsi:nil="true" />
                    <OptionalEl xsi:nil="true" />
                    <ListRef xsi:nil="true" />
                    <ListVal xsi:nil="true" />
                    <ListEl xsi:nil="true" />
                </Root>
                """);

            Assert.IsNull(doc.OptionalRef);
            Assert.IsNull(doc.OptionalVal);
            Assert.IsNull(doc.OptionalEl);
            Assert.IsNull(doc.RequiredRef);
            Assert.IsNull(doc.RequiredVal);
            Assert.IsNull(doc.RequiredEl);
            Assert.AreEqual(1, doc.ListRef.Count);
            Assert.IsNull(doc.ListRef[0]);
            Assert.AreEqual(1, doc.ListVal.Count);
            Assert.IsNull(doc.ListVal[0]);
            Assert.AreEqual(1, doc.ListEl.Count);
            Assert.IsNull(doc.ListEl[0]);
        }

        [Test]
        public void UpdateElement()
        {
            var doc = new Root();
            // For more predictable serialization
            AddGlobalXsiNamespace(doc);

            doc.RequiredRef = null;
            doc.RequiredEl = null;
            var xml = doc.Untyped.ToString();

            Assert.IsTrue(xml.Contains("<RequiredRef xsi:nil=\"true\" />"));
            Assert.IsTrue(xml.Contains("<RequiredEl xsi:nil=\"true\" />"));

            doc.RequiredRef = "abc";
            doc.RequiredEl = new ValueHolder { Value = "abc" };
            xml = doc.Untyped.ToString();

            Assert.IsTrue(xml.Contains("<RequiredRef>abc</RequiredRef>"));
            Assert.IsTrue(Regex.IsMatch(xml, @"<RequiredEl>\s*<Value>abc</Value>\s*</RequiredEl>"));

            doc.RequiredRef = null;
            doc.RequiredEl = null;
            xml = doc.Untyped.ToString();

            Assert.IsTrue(xml.Contains("<RequiredRef xsi:nil=\"true\" />"));
            Assert.IsTrue(xml.Contains("<RequiredEl xsi:nil=\"true\" />"));
        }

        [Test]
        public void UpdateList()
        {
            var doc = new Root();
            // For more predictable serialization
            AddGlobalXsiNamespace(doc);

            doc.ListRef.Add(null);
            doc.ListRef.Add("abc");
            doc.ListRef.Add(null);
            doc.ListEl.Add(null);
            doc.ListEl.Add(new ValueHolder { Value = "abc" });
            doc.ListEl.Add(null);
            var xml = doc.Untyped.ToString();

            Assert.IsTrue(Regex.IsMatch(xml, "<ListRef xsi:nil=\"true\" />\\s*<ListRef>abc</ListRef>\\s*<ListRef xsi:nil=\"true\" />"));
            Assert.IsTrue(Regex.IsMatch(xml, "<ListEl xsi:nil=\"true\" />\\s*<ListEl>\\s*<Value>abc</Value>\\s*</ListEl>\\s*<ListEl xsi:nil=\"true\" />"));

            doc.ListRef.Remove(null);
            doc.ListRef[0] = null;
            doc.ListRef[1] = "xyz";
            doc.ListEl.Remove(null);
            doc.ListEl[0] = null;
            doc.ListEl[1] = new ValueHolder { Value = "xyz" };
            xml = doc.Untyped.ToString();

            Assert.IsTrue(Regex.IsMatch(xml, "<ListRef xsi:nil=\"true\" />\\s*<ListRef>xyz</ListRef>"));
            Assert.IsTrue(Regex.IsMatch(xml, "<ListEl xsi:nil=\"true\" />\\s*<ListEl>\\s*<Value>xyz</Value>\\s*</ListEl>"));

            doc.ListRef[0] = "abc";
            doc.ListRef[1] = null;
            doc.ListEl[0] = new ValueHolder { Value = "abc" };
            doc.ListEl[1] = null;
            xml = doc.Untyped.ToString();

            Assert.IsTrue(Regex.IsMatch(xml, "<ListRef>abc</ListRef>\\s*<ListRef xsi:nil=\"true\" />"));
            Assert.IsTrue(Regex.IsMatch(xml, "<ListEl>\\s*<Value>abc</Value>\\s*</ListEl>\\s*<ListEl xsi:nil=\"true\" />"));
        }
    }
}
