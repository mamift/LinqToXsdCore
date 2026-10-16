using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Xml.Linq;

using LinqToXsd.Schemas.Test.EnumsTypes;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    /// <summary>
    /// Tests code generation and runtime behaviour for XSD enumeration values that are not
    /// valid C# identifiers (operators such as "==", "<=", values containing colons, and
    /// values whose member names collide with one another).
    /// </summary>
    public class OperatorEnumTests
    {
        private List<EnumDeclarationSyntax> GeneratedEnums { get; set; }

        [OneTimeSetUp]
        public void GenerateCode()
        {
            const string xsdFilePath = @"EnumsTest\EnumsTest.xsd";
            var testFiles = Utilities.GetAssemblyFileSystem(typeof(LanguageCodeEnum).Assembly);

            var tree = Utilities.GenerateSyntaxTree(xsdFilePath, testFiles);
            this.GeneratedEnums = tree.GetRoot().DescendantNodes()
                .OfType<EnumDeclarationSyntax>()
                .ToList();
        }

        [Test]
        public void WhenEnumValuesAreOnlySymbolsThenMembersExpandToFullWords()
        {
            var operatorEnum = GeneratedEnums.Single(e => e.Identifier.Text == nameof(OperatorEnum));
            var memberNames = operatorEnum.Members.Select(m => m.Identifier.Text).ToList();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "EqualsEquals", "ExclamationMarkEquals", "LessThan", "LessThanEquals",
                    "GreaterThan", "GreaterThanEquals", "AmpersandAmpersand", "PipePipe",
                },
                memberNames);
            Assert.AreEqual(memberNames.Count, memberNames.Distinct().Count());
        }

        [Test]
        public void WhenEnumValuesCollideThenGeneratedMembersAreUnique()
        {
            var collisionEnum = GeneratedEnums.Single(e => e.Identifier.Text == nameof(CollisionEnum));
            var memberNames = collisionEnum.Members.Select(m => m.Identifier.Text).ToList();

            Assert.AreEqual(2, memberNames.Count);
            Assert.AreEqual(memberNames.Count, memberNames.Distinct().Count());
            Assert.Contains("Asterisk", memberNames);
            Assert.IsTrue(memberNames.Any(name => name.StartsWith("Asterisk_")));
        }

        [Test]
        public void WhenEnumValueContainsSeparatorColonThenMembersAreUnique()
        {
            var colonEnum = GeneratedEnums.Single(e => e.Identifier.Text == nameof(ColonContainingEnum));
            var memberNames = colonEnum.Members.Select(m => m.Identifier.Text).ToList();

            CollectionAssert.AreEquivalent(new[] { "_12_30", "_23_59" }, memberNames);
        }

        [Test]
        public void WhenOperatorEnumValueIsSetThenOriginalSchemaValueIsWrittenToXml()
        {
            var operatorsToXmlValues = new Dictionary<OperatorEnum, string>
            {
                { OperatorEnum.EqualsEquals, "==" },
                { OperatorEnum.ExclamationMarkEquals, "!=" },
                { OperatorEnum.LessThan, "<" },
                { OperatorEnum.LessThanEquals, "<=" },
                { OperatorEnum.GreaterThan, ">" },
                { OperatorEnum.GreaterThanEquals, ">=" },
                { OperatorEnum.AmpersandAmpersand, "&&" },
                { OperatorEnum.PipePipe, "||" },
            };

            foreach (var pair in operatorsToXmlValues)
            {
                var element = new OperatorElementType
                {
                    Operator = pair.Key,
                    Time = ColonContainingEnum._12_30,
                    Shape = CollisionEnum.Asterisk,
                };

                // the setter stores the original schema value, not the C# member name
                Assert.AreEqual(pair.Value, ((XElement)element.Untyped.FirstNode).Value);

                // the getter converts the original schema value back into the enum member
                var reparsed = (OperatorElementType) XElement.Parse(element.Untyped.ToString());
                Assert.AreEqual(pair.Key, reparsed.Operator);
            }
        }

        [Test]
        public void WhenColonContainingEnumValueIsSetThenColonSurvivesRoundTrip()
        {
            var element = new OperatorElementType
            {
                Operator = OperatorEnum.EqualsEquals,
                Time = ColonContainingEnum._23_59,
                Shape = CollisionEnum.Asterisk,
            };

            // the colon inside the schema value must not be confused with the 'value:member' separator
            Assert.AreEqual("23:59", ((XElement)element.Untyped.FirstNode.NextNode).Value);

            var reparsed = (OperatorElementType) XElement.Parse(element.Untyped.ToString());
            Assert.AreEqual(ColonContainingEnum._23_59, reparsed.Time);
        }

        [Test]
        public void WhenCollidingEnumValueIsSetThenOriginalSchemaValueIsWrittenToXml()
        {
            var element = new OperatorElementType
            {
                Operator = OperatorEnum.LessThanEquals,
                Time = ColonContainingEnum._12_30,
                Shape = CollisionEnum.Asterisk_2F0C9F3D,
            };

            // the disambiguated member maps back to the original symbol value
            Assert.AreEqual("*", ((XElement)element.Untyped.LastNode).Value);

            var reparsed = (OperatorElementType) XElement.Parse(element.Untyped.ToString());
            Assert.AreEqual(CollisionEnum.Asterisk_2F0C9F3D, reparsed.Shape);

            var literal = (OperatorElementType) XElement.Parse(
                element.Untyped.ToString().Replace("<Shape>*</Shape>", "<Shape>Asterisk</Shape>"));
            Assert.AreEqual(CollisionEnum.Asterisk, literal.Shape);
        }
    }
}
