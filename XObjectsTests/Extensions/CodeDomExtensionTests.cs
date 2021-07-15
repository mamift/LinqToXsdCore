using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Resolvers;
using System.Xml.Schema;
using LinqToXsd.Schemas;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests.Extensions
{
    [TestFixture]
    public class CodeDomExtensionTests
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
            CodeCompileUnit ccu = XObjectsCoreGenerator.GenerateCodeCompileUnit(Shared.XmlSpecSchemaSet.Value,
                Shared.XmlSpecLinqToXsdSettings.Value);

            List<StringWriter> classStringWriters = ccu.ToClassStringWriters().ToList();

            Assert.IsNotEmpty(classStringWriters);

            foreach (var classWriter in classStringWriters) {
                var classCode = classWriter.ToString();

                Assert.IsNotEmpty(classCode);
            }
        }

        [Test]
        public void ToNamespaceStringWritersTest()
        {
            CodeCompileUnit ccu =
                XObjectsCoreGenerator.GenerateCodeCompileUnit(Shared.XmlSpecSchemaSet.Value,
                    Shared.XmlSpecLinqToXsdSettings.Value);

            List<StringWriter> classStringWriters = ccu.ToNamespaceStringWriters().ToList();

            Assert.IsNotEmpty(classStringWriters);
            Assert.IsTrue(classStringWriters.Count == 1);

            foreach (var namespaceWriter in classStringWriters) {
                var namespaceCode = namespaceWriter.ToString();

                Assert.IsNotEmpty(namespaceCode);
            }
        }
    }
}