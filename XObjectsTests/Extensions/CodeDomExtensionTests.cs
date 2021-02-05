using System.CodeDom;
using System.Linq;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests.Extensions
{
    [TestFixture]
    public class CodeDomExtensionTests
    {
        public static CodeTypeDeclaration GenerateEnum(string enumName, params string[] enumFields)
        {
            var theEnum = new CodeTypeDeclaration(enumName) {
                IsEnum = true,
                Attributes = MemberAttributes.Public,
            };

            CodeMemberField[] fields = enumFields.Select(f => new CodeMemberField(enumName, f)).ToArray();
            theEnum.Members.AddRange(fields);

            return theEnum;
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
        public void IsEquivalentCodeMemberField()
        {
            const string enumName = "PropertyBagParentTypeDefinition";
            var webField = new CodeMemberField(enumName, "Web");
            var folderField = new CodeMemberField(enumName, "Folder");
            var listItemField = new CodeMemberField(enumName, "ListItem");
            var fileItemField = new CodeMemberField(enumName, "File");

            var isEquivalent1 = webField.IsEquivalent(folderField);
            var isEquivalent2 = listItemField.IsEquivalent(fileItemField);

            Assert.IsFalse(isEquivalent1);
            Assert.IsFalse(isEquivalent2);
        }

        [Test]
        public void IsEquivalentCodeMemberFieldBetweenClones()
        {
            const string enumName = "PropertyBagParentTypeDefinition";
            var webField = new CodeMemberField(enumName, "Web");
            var webField2 = new CodeMemberField(enumName, "Web");
            
            var isEquivalent = webField.IsEquivalent(webField2);
            Assert.IsTrue(isEquivalent);
        }
    }
}