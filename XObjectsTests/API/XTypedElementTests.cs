using System.Xml.Linq;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests.API
{
    public class XTypedElementTests
    {
        [Test]
        public void TestInternalTypeConversion()
        {
            var defaultNamespaceEl = new XElement(XName.Get(nameof(Namespace)));
            defaultNamespaceEl.SetAttributeValue(XName.Get(nameof(Namespace.Schema)), string.Empty);

            Assert.DoesNotThrow(() => {
                var @namespace = (Namespace) defaultNamespaceEl;
                Assert.IsNotNull(@namespace);
            });
        }
    }
}