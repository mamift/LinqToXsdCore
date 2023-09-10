using System;
using System.CodeDom;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Resolvers;
using System.Xml.Schema;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests.Extensions
{
    [TestFixture]
    public class CodeDomExtensionTests: BaseTester
    {
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
            var xmlSpecSchemaSet = Utilities.PreLoadXmlSchemas(xmlSpecXsd, TestFiles);

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