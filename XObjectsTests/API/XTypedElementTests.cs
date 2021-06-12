using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using LinqToXsd.Schemas.AbstractTypeTest;
using Microsoft.Schemas.AspNetSiteMaps;
using Microsoft.Schemas.SharePoint;
using NUnit.Framework;
using W3C.XSD;

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

        /// <summary>
        /// Tests that the <see cref="XTypedServices.Parse{T}"/> works on internal generated types.
        /// </summary>
        [Test]
        public void TestInternalTypeConstruction()
        {
            var egConfigString = "<?xml version=\"1.0\" encoding=\"utf-8\"?> <Configuration xmlns=\"http://www.microsoft.com/xml/schema/linq\"> <Namespaces> <Namespace DefaultVisibility=\"public\" Schema=\"http://www.w3.org/2005/Atom\" Clr=\"XObjectsTests.Schemas.Atom\" /> </Namespaces> <Validation> <VerifyRequired>false</VerifyRequired> </Validation> <Transformation> <Deanonymize strict=\"false\" /> </Transformation> </Configuration>";

            Assert.DoesNotThrow(() => {
                Configuration configInstance = Configuration.Parse(egConfigString);
            });
        }

        /// <summary>
        /// Tests that the <see cref="XTypedServices.Parse{T}"/> works on public generated types.
        /// </summary>
        [Test]
        public void TestPublicTypeConstruction()
        {
            var ncName = new localSimpleType() {
                restriction = new restriction() {
                    enumeration = {
                        new enumeration(new noFixedFacet() { value = "default" }),
                        new enumeration(new noFixedFacet() { value = "preserve" })
                    }
                }
            };

            var xsdExample = new schema() {
                targetNamespace = new Uri("http://www.w3.org/XML/1998/namespace"),
                attribute = new List<attribute>() {
                    new attribute(new topLevelAttribute() { name = "base", type = new XmlQualifiedName("anyURI") }),
                    new attribute(new topLevelAttribute() { name = "lang", type = new XmlQualifiedName("language") }),
                    new attribute(new topLevelAttribute() { name = "space", simpleType = ncName })
                },
                attributeGroup = new List<attributeGroup>() {
                    new attributeGroup(new namedAttributeGroup() { 
                        name = "specialAttrs",
                        attribute = {
                            new topLevelAttribute() { @ref = new XmlQualifiedName("base") },
                            new topLevelAttribute() { @ref = new XmlQualifiedName("lang") },
                            new topLevelAttribute() { @ref = new XmlQualifiedName("space") },
                        }
                    })
                }
            };

            Assert.DoesNotThrow(() => { xsdExample.lang = "en-GB"; });

            var xsdString = xsdExample.ToString();

            Assert.DoesNotThrow(() => {
                schema schema = schema.Parse(xsdString);

                Assert.IsNotNull(schema.lang as string);
            });
        }

        [Test]
        public void TestXsiTypeAddedToAmbiguousElementNames()
        {
            var o = new LinqToXsd.Schemas.AbstractTypeTest.Action
            {
                ActionInfo = new Record
                {
                    id = "7"
                },
                Commands = new BaseCommand[]
                {
                    new UpdateCommand
                    {
                        value = "updated"
                    }
                }
            };

            o.Untyped.Add(new XAttribute(XNamespace.Xmlns + "xsi", XmlSchema.InstanceNamespace));

            var expectedXml = @"<Action xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://example.org/AbstractTest"">
      <ActionInfo  id=""7"" xsi:type=""Record"" />
      <Commands  value=""updated"" xsi:type=""UpdateCommand""/>
    </Action>";


            var actualXml = o.ToString();
            
            Assert.IsTrue(XNode.DeepEquals(XElement.Parse(expectedXml), XElement.Parse(actualXml)),
                String.Format("{0} \n does not equal \n{1}", actualXml, expectedXml));
        }

        [Test]
        public void TestAncestorIsRootElement()
        {
            var example = siteMap.Load(@"AspNetSiteMaps\example.sitemap");

            List<siteMapNodeType> children = example.Query.Descendants<siteMapNodeType>().ToList();

            List<siteMapType> ancestors = children.SelectMany(c => c.Query.Ancestors<siteMapType>()).Distinct().ToList();

            Assert.True(ancestors.Count == 1);
        }

        [Test]
        public void TestAncestorOfRecursiveChild()
        {
            var example = siteMap.Load(@"AspNetSiteMaps\example.sitemap");

            List<siteMapNodeType> descendents = example.Query.Descendants<siteMapNodeType>().ToList();

            var theDescendentThatIsAChildOfASiteMapNode = descendents.FirstOrDefault(d => d.url.ToString() == "~/Restaurant/ChooseMainPack.aspx");

            Assert.IsNotNull(theDescendentThatIsAChildOfASiteMapNode);
            
            var childrenOfSiteMapNode = descendents.Where(c => c.Query.Ancestors<siteMapNodeType>().Any()).ToList();

            Assert.Contains(theDescendentThatIsAChildOfASiteMapNode, childrenOfSiteMapNode);

            var ancestor = theDescendentThatIsAChildOfASiteMapNode.Query.Ancestors<siteMapNodeType>().FirstOrDefault();

            Assert.IsNotNull(ancestor);

            Assert.True(ancestor.url.ToString() == "~/Restaurant/ChoosePacks.aspx");
        }
    }
}