using NUnit.Framework;
using System;
using System.IO;

namespace Xml.Schema.Linq.Tests
{
    public class DatesTests
    {
        [Test]
        public void DateOnlyTypesCanRoundtrip()
        {
            // Serialize document

            var doc = new LinqToXsd.Schemas.Test.DateOnlyTest.root
            {
                adate = new DateOnly(2023, 06, 30),
                atime = new TimeOnly(14, 39),
                edate = new DateOnly(2023, 12, 25),
                etime = new TimeOnly(8, 15, 22),
                edatetime = new DateTime(2023, 06, 30, 14, 39, 00),
                vdate = new DateOnly(2023, 06, 07), // vdate has restriction > 2000-01-01
                vtime = new TimeOnly(16, 00, 00), // vtime has pattern \d{2}:00:00
            };
            var writer = new StringWriter();
            doc.Save(writer);
            var xml = writer.ToString();

            // Verify DateOnly types are serialized into the expected format
            Assert.IsTrue(xml.Contains("a-date=\"2023-06-30\""), "DateOnly attribute incorrectly serialized");
            Assert.IsTrue(xml.Contains("a-time=\"14:39:00\""), "TimeOnly attribute incorrectly serialized");
            Assert.IsTrue(xml.Contains("<e-date>2023-12-25</e-date>"), "DateOnly element incorrectly serialized");
            Assert.IsTrue(xml.Contains("<e-time>08:15:22</e-time>"), "TimeOnly element incorrectly serialized");
            Assert.IsTrue(xml.Contains("<e-datetime>2023-06-30T14:39:00</e-datetime>"), "DateTime element incorrectly serialized");
            Assert.IsTrue(xml.Contains("<v-date>2023-06-07</v-date>"), "Validated DateOnly incorrectly serialized");
            Assert.IsTrue(xml.Contains("<v-time>16:00:00</v-time>"), "Validated TimeOnly incorrectly serialized");

            // Parse document
            doc = LinqToXsd.Schemas.Test.DateOnlyTest.root.Parse(xml);

            // Verify everything has round-tripped
            Assert.AreEqual(new DateOnly(2023, 06, 30), doc.adate);
            Assert.AreEqual(new TimeOnly(14, 39), doc.atime);
            Assert.AreEqual(new DateOnly(2023, 12, 25), doc.edate);
            Assert.AreEqual(new TimeOnly(8, 15, 22), doc.etime);
            Assert.AreEqual(new DateTime(2023, 06, 30, 14, 39, 00), doc.edatetime);
            Assert.AreEqual(new DateOnly(2023, 06, 07), doc.vdate);
            Assert.AreEqual(new TimeOnly(16, 00, 00), doc.vtime);
        }

        [Test]
        public void DateTimeOffsetCanRoundtrip()
        {
            // Serialize document

            var time = new DateTimeOffset(2023, 06, 30, 14, 39, 00, new TimeSpan(12, 45, 00));
            var dt = new DateTime(2023, 06, 01, 16, 42, 00);

            var doc = new LinqToXsd.Schemas.Test.DateTimeOffsetTest.root
            {
                adatetime = time,
                edatetime = time,
                edate = dt,
                etime = dt,
            };
            var writer = new StringWriter();
            doc.Save(writer);
            var xml = writer.ToString();

            // Verify DateTimeOffsets types are serialized into the expected format
            Assert.IsTrue(xml.Contains("a-datetime=\"2023-06-30T14:39:00+12:45\""), "DateTimeOffset attribute incorrectly serialized");
            Assert.IsTrue(xml.Contains("<e-datetime>2023-06-30T14:39:00+12:45</e-datetime>"), "DateTimeOffset element incorrectly serialized");
            Assert.IsTrue(xml.Contains("<e-date>2023-06-01</e-date>"), "DateTime (as date) incorrectly serialized");
            Assert.IsTrue(xml.Contains("<e-time>16:42:00</e-time>"), "DateTime (as time) incorrectly serialized");

            // Parse document
            doc = LinqToXsd.Schemas.Test.DateTimeOffsetTest.root.Parse(xml);

            // Verify everything has round-tripped
            Assert.AreEqual(time, doc.adatetime);
            Assert.AreEqual(time, doc.edatetime);
            Assert.AreEqual(dt.Date, doc.edate);
            Assert.AreEqual(DateTime.Today.Add(dt.TimeOfDay), doc.etime);
        }
    }
}
