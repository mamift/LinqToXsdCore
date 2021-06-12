using System.Linq;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests.API
{
    public class XListTests
    {
        [Test]
        public void TestXListEnumerateTest()
        {
            Assert.DoesNotThrow(() => {
                var schema = W3C.XSD.schema.Load(@"XSD\\W3C XMLSchema v1.xsd");
                Assert.IsNotNull(schema);
                var elementsList = schema.element.ToList();
                Assert.IsNotEmpty(elementsList);
                Assert.IsTrue(elementsList.Count == 41);
            });
        }
    }
}