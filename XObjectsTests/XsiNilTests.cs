using LinqToXsd.Schemas.Test.NilTest;
using NUnit.Framework;
using System;
using System.IO;

namespace Xml.Schema.Linq.Tests
{
    public class XsiNilTests
    {
        [Test]
        public void Serialize()
        {
            // Serialize document

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
            var writer = new StringWriter();
            doc.Save(writer);
            var xml = writer.ToString();

            // Verify DateOnly types are serialized into the expected format
            Assert.IsTrue(xml.Contains("<OptionalEl xsi:nil=\"true\">"), "OptionalEl incorrectly serialized");

            // // Parse document
            // doc = Root.Parse(xml);

            // // Verify everything has round-tripped
            // Assert.AreEqual(doc.adate, new DateOnly(2023, 06, 30));
            // Assert.AreEqual(doc.atime, new TimeOnly(14, 39));
            // Assert.AreEqual(doc.edate, new DateOnly(2023, 12, 25));
            // Assert.AreEqual(doc.etime, new TimeOnly(8, 15, 22));
            // Assert.AreEqual(doc.edatetime, new DateTime(2023, 06, 30, 14, 39, 00));
        }
    }
}
