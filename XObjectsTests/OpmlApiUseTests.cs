using NUnit.Framework;
using Opml;

namespace Xml.Schema.Linq.Tests
{
    [TestFixture]
    public class OpmlApiUseTests
    {
        [Test]
        public void TestUseOfUnknownType()
        {
            Assert.DoesNotThrow(() => {
                var outline = new Outline() {
                    language = Unknown.unknown.ToString()
                };
            });

            Assert.DoesNotThrow(() => {
                var outline = new Outline() {
                    language = "en-AU"
                };

                outline.language = "en-GB";
            });

            Assert.Throws<LinqToXsdException>(() => {
                var outline = new Outline() {
                    language = Unknown.unknown
                };
            });
        }
    }
}