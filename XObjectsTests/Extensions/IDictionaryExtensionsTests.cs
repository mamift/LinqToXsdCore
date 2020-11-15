using System.Collections.Generic;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests.Extensions
{
    [TestFixture]
    public class IDictionaryExtensionsTests
    {
        [Test]
        public void IsEqualsPositiveTest()
        {
            var one = new Dictionary<string, int>() {
                {"do1", 1},
                {"do2", 2},
            };
            var two = new Dictionary<string, int>() {
                {"do1", 1},
                {"do2", 2},
            };

            var isEquals = ((IDictionary<string, int>) one).IsEquals(((IDictionary<string, int>) two));

            Assert.IsTrue(isEquals);
        }

        [Test]
        public void IsEqualsNegativeTest()
        {
            var one = new Dictionary<string, int>() {
                {"do1", 1},
                {"do2", 1},
            };
            var two = new Dictionary<string, int>() {
                {"do1", 1},
                {"do2", 2},
            };

            var isEquals = ((IDictionary<string, int>) one).IsEquals(((IDictionary<string, int>) two));

            Assert.IsFalse(isEquals);
        }
    }
}