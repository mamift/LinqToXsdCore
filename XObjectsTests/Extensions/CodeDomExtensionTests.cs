using System;
using System.CodeDom;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Resolvers;
using System.Xml.Schema;
using Fasterflect;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests.Extensions
{
    [TestFixture]
    public class CodeDomExtensionTests: BaseTester
    {
        [Test]
        public void TestIsEquivalentTypeReference()
        {
            var xmlSchemaElement = new XmlSchemaType() {
                Name = "test"
            };

            var exampleClrTypeRef = new ClrTypeReference(nameof(String), typeof(string).Namespace,
                xmlSchemaElement, false, false);

            exampleClrTypeRef.SetFieldValue("clrName", "String");
            exampleClrTypeRef.SetFieldValue("typeNs", "System");
            exampleClrTypeRef.SetFieldValue("clrFullTypeName", typeof(string).FullName);

            var codeTypeRef = new CodeTypeReference(typeof(string));

            var isEquivalent = exampleClrTypeRef.IsEquivalentTypeReference(codeTypeRef);

            Assert.True(isEquivalent);
        }

        [Test]
        public void IsEquivalentEnumDeclarationTestTrue()
        {
            var enumOne = new CodeTypeDeclaration() {
                Name = "DesignerTyper",
                IsEnum = true,
                Members = { new CodeMemberField("DesignerType", "TestingMember1") }
            };

            var enumTwo = new CodeTypeDeclaration() {
                Name = "DesignerTyper",
                IsEnum = true,
                Members = { new CodeMemberField("DesignerType", "TestingMember1") }
            };

            var isEquivalent = enumOne.IsEquivalentEnumDeclaration(enumTwo);

            Assert.IsTrue(isEquivalent);
        }

        [Test]
        public void IsEquivalentEnumDeclarationTestFalse()
        {
            var enumOne = new CodeTypeDeclaration() {
                Name = "DesignerTyper",
                IsEnum = true,
                Members = { new CodeMemberField("DesignerType", "TestingMemberA") }
            };

            var enumTwo = new CodeTypeDeclaration() {
                Name = "DesignerTyper",
                IsEnum = true,
                Members = { new CodeMemberField("DesignerType", "TestingMember1") }
            };

            var isEquivalent = enumOne.IsEquivalentEnumDeclaration(enumTwo);

            Assert.IsFalse(isEquivalent);
        }

        [Test]
        public void IsEquivalentEnumDeclarationTestMembersCount()
        {
            var enumOne = new CodeTypeDeclaration() {
                Name = "DesignerTyper",
                IsEnum = true,
                Members = { new CodeMemberField("DesignerType", "TestingMemberA") }
            };

            var enumTwo = new CodeTypeDeclaration() {
                Name = "DesignerTyper",
                IsEnum = true,
                Members = {
                    new CodeMemberField("DesignerType", "TestingMember1"),
                    new CodeMemberField("DesignerType", "A")
                }
            };

            var isEquivalent = enumOne.IsEquivalentEnumDeclaration(enumTwo);

            Assert.IsFalse(isEquivalent);
        }

        [Test]
        public void ToClassStringWritersTest()
        {
            var xmlSpecXsd = @"XMLSpec\xmlspec.xsd";
            var xmlSpecXsdConfigFile = @"XMLSpec\xmlspec.xsd.config";
            var xmlSpecXsdConfig = Configuration.Load(GetFileStreamReader(xmlSpecXsdConfigFile));
            var xmlSpecSchemaSet = Utilities.GetAssemblyFileSystem(typeof(W3C.XMLSpec.listclass).Assembly).PreLoadXmlSchemas(xmlSpecXsd);

            Assert.IsNotNull(xmlSpecSchemaSet);
            Assert.IsTrue(xmlSpecSchemaSet.IsCompiled);

            var ccus = XObjectsCoreGenerator.GenerateCodeCompileUnits(xmlSpecSchemaSet,
                xmlSpecXsdConfig.ToLinqToXsdSettings());

            var classStringWriters = ccus.SelectMany(x => x.unit.ToClassStringWriters()).ToList();

            Assert.IsNotEmpty(classStringWriters);

            foreach (var one in classStringWriters) {
                var classString = one.ToString();

                Assert.IsNotEmpty(classString);
            }
        }
    }
}