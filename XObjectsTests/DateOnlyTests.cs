using LinqToXsd.Schemas.Test.DateOnlyTest;
using NUnit.Framework;
using System;
using System.IO;

namespace Xml.Schema.Linq.Tests
{
    public class DateOnlyTests
    {
        [Test]
        public void DateOnlyTypesCanRoundtrip()
        {
            // Serialize document

            var doc = new root
            {
                adate = new DateOnly(2023, 06, 30),
                atime = new TimeOnly(14, 39),
                edate = new DateOnly(2023, 12, 25),
                etime = new TimeOnly(8, 15, 22),
                edatetime = new DateTime(2023, 06, 30, 14, 39, 00),
            };
            var writer = new StringWriter();
            doc.Save(writer);
            var xml = writer.ToString();

            // Verify DateOnly types are serialized into the expected format
            Assert.IsTrue(xml.Contains("a-date=\"2023-06-30\""), "DateOnly attribute incorrectly serialized");
            Assert.IsTrue(xml.Contains("a-time=\"14:39:00\""), "TimeOnly attribute incorrectly serialized");
            Assert.IsTrue(xml.Contains("<e-date>2023-12-25</e-date>"), "DateOnly element incorrectly serialized");
            Assert.IsTrue(xml.Contains("<e-time>08:15:22</e-time>"), "TimeOnly element incorrectly serialized");

            // Parse document
            doc = root.Parse(xml);

            // Verify everything has round-tripped
            Assert.AreEqual(doc.adate, new DateOnly(2023, 06, 30));
            Assert.AreEqual(doc.atime, new TimeOnly(14, 39));
            Assert.AreEqual(doc.edate, new DateOnly(2023, 12, 25));
            Assert.AreEqual(doc.etime, new TimeOnly(8, 15, 22));
            Assert.AreEqual(doc.edatetime, new DateTime(2023, 06, 30, 14, 39, 00));
        }
    }
}
