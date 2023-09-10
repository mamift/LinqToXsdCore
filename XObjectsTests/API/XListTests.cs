using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests.API
{
    public class XListTests: BaseTester
    {
        [Test]
        public void TestXListEnumerateTest()
        {
            var schemas = TestFiles.AllFiles.Where(f => f.EndsWith("W3C XMLSchema v1.xsd")).Select(f => TestFiles.FileInfo.New(f)).ToList();

            Assert.DoesNotThrow(() => {
                using var reader = new StreamReader(schemas.First().OpenRead());
                var schema = W3C.XSD.schema.Load(reader);
                Assert.IsNotNull(schema);
                var elementsList = schema.element.ToList();
                Assert.IsNotEmpty(elementsList);
                Assert.IsTrue(elementsList.Count == 41);
            });
        }
    }
}