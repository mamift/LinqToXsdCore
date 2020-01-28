using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Schemas.SharePoint;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests.API
{
    public class XTypedElementTests
    {
        /// <summary>
        /// Tests strong-typed conversion from weak-typed XML to an internal LinqToXsd type.
        /// </summary>
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

        /// <summary>
        /// Tests strong-typed conversion from weak-typed XML to a public LinqToXsd type.
        /// </summary>
        [Test]
        public void TestPublicTypeConversion()
        {
            var logicalJoinDefXml = "<LogicalJoinDefinition xmlns=\"http://schemas.microsoft.com/sharepoint/\"> <Or> <Eq> <FieldRef Name=\"FirstName\" /> </Eq> </Or> </LogicalJoinDefinition>";
            var logicalJoinDefElement = XElement.Parse(logicalJoinDefXml);

            LogicalJoinDefinition where = null;
            Assert.DoesNotThrow(() => {
                where = (LogicalJoinDefinition) logicalJoinDefElement;
                Assert.IsNotNull(where);
            });

            Assert.DoesNotThrow(() => {
                var newCamlQueryRoot = new CamlQueryRoot {
                    Where = @where
                };
                Assert.IsNotNull(newCamlQueryRoot.Where);
            });
        }
    }
}