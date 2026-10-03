using System.Linq;

using NUnit.Framework;

using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq.Tests
{
    public class EnumFacetTests
    {
        [Test]
        public void WhenValueIsOnlySymbolsThenEachSymbolExpandsToItsFullWord()
        {
            Assert.AreEqual("EqualsEquals", new EnumFacet("==").Member);
            Assert.AreEqual("ExclamationMarkEquals", new EnumFacet("!=").Member);
            Assert.AreEqual("LessThanEquals", new EnumFacet("<=").Member);
            Assert.AreEqual("GreaterThanEquals", new EnumFacet(">=").Member);
            Assert.AreEqual("AmpersandAmpersand", new EnumFacet("&&").Member);
            Assert.AreEqual("PipePipe", new EnumFacet("||").Member);
        }

        [Test]
        public void WhenValueIsSingleSymbolThenMemberIsItsFullWord()
        {
            Assert.AreEqual("LessThan", new EnumFacet("<").Member);
            Assert.AreEqual("Plus", new EnumFacet("+").Member);
            Assert.AreEqual("ExclamationMark", new EnumFacet("!").Member);
        }

        [Test]
        public void WhenValueContainsLettersThenLegacyUnderscoreReplacementIsKept()
        {
            Assert.AreEqual("en_fr", new EnumFacet("en-fr").Member);
            Assert.AreEqual("x_", new EnumFacet("x!").Member);
            Assert.AreEqual("y_x", new EnumFacet("y^x").Member);
        }

        [Test]
        public void WhenValueIsKeywordThenMemberIsEscapedWithAtSign()
        {
            Assert.AreEqual("@int", new EnumFacet("int").Member);
        }

        [Test]
        public void WhenValueHasUnknownSymbolThenMemberFallsBackToUnderscore()
        {
            // the interrobang has no entry in NameGenerator.SymbolFullWords
            Assert.AreEqual("_", new EnumFacet("‽").Member);
        }

        [Test]
        public void WhenFacetsCollideThenMembersAreDisambiguatedAndUnique()
        {
            var facets = EnumFacet.CreateUniqueFacets(new[] { "*", "Asterisk" });

            Assert.AreEqual(2, facets.Count);

            var star = facets.Single(facet => facet.Value == "*");
            var asterisk = facets.Single(facet => facet.Value == "Asterisk");

            Assert.AreEqual("Asterisk", asterisk.Member);
            Assert.That(star.Member, Does.StartWith("Asterisk_"));
            Assert.That(star.Member, Is.Not.EqualTo("Asterisk"));
            Assert.AreEqual(2, facets.Select(facet => facet.Member).Distinct().Count());
        }

        [Test]
        public void WhenFacetsCollideThenDisambiguationIsIndependentOfFacetOrder()
        {
            var forward = EnumFacet.CreateUniqueFacets(new[] { "a-b", "a_b" });
            var reversed = EnumFacet.CreateUniqueFacets(new[] { "a_b", "a-b" });

            Assert.AreEqual(
                forward.Single(facet => facet.Value == "a-b").Member,
                reversed.Single(facet => facet.Value == "a-b").Member);
            Assert.AreEqual(
                forward.Single(facet => facet.Value == "a_b").Member,
                reversed.Single(facet => facet.Value == "a_b").Member);
        }

        [Test]
        public void WhenValuesAreDuplicatedThenOnlyOneFacetIsCreated()
        {
            var facets = EnumFacet.CreateUniqueFacets(new[] { "==", "==", "!=" });

            Assert.AreEqual(2, facets.Count);
            Assert.AreEqual(1, facets.Count(facet => facet.Value == "=="));
        }
    }
}
