using System.Xml.Linq;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests.XsdFeatureTests;

public class ElementFeatureTests
{
    [Test]
    public void TestDefaultElementsValues()
    {
        var xml = $"""
                   <name xmlns="urn:LinqToXsdCore:Elements:ListTypeWithDefaults" />
                   """;

        var name = urn.LinqToXsdCore.Elements.ListTypeWithDefaults.name.Parse(xml);

        Assert.NotNull(name);
        Assert.NotNull(name.names);
        Assert.True(name.names.Count == 3);
    }

    
    [Test]
    public void TestDefaultElementsValuesThenSetCustomValues()
    {
        var xml = $"""
                   <name xmlns="urn:LinqToXsdCore:Elements:ListTypeWithDefaults" />
                   """;

        var name = urn.LinqToXsdCore.Elements.ListTypeWithDefaults.name.Parse(xml);

        Assert.NotNull(name);
        Assert.NotNull(name.names);
        Assert.True(name.names.Count == 3);

        name.names = ["alex", "anton"];

        var xmlElement = name.Untyped;

        Assert.IsNotEmpty(xmlElement.Descendants());

        XElement firstChild = (XElement)xmlElement.FirstNode;
        XElement lastChild = (XElement)xmlElement.LastNode;

        Assert.True(firstChild!.Value == "alex");
        Assert.True(lastChild!.Value == "anton");
    }
}