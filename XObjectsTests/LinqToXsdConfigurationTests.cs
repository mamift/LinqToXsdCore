using System.Collections.Generic;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests;

public class LinqToXsdConfigurationTests
{
    [Test]
    public void TestSetDefaultVisibility()
    {
        var config = new Configuration();
        var ns1 = new Namespace() {
            DefaultVisibility = Namespace.DefaultVisibilityEnum.@internal
        };
        config.Namespaces = new Namespaces() {
            Namespace = new List<Namespace>() {
                ns1
            }
        };

        var expected = ns1.DefaultVisibility.ToString();
        Assert.AreEqual(expected, "internal");
    }
}