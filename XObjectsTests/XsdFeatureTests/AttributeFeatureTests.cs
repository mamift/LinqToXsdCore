using NUnit.Framework;

namespace Xml.Schema.Linq.Tests.XsdFeatureTests;

public class AttributeFeatureTests
{
    [Test]
    public void AttributeFeatureTestOtherNames1()
    {
        var xml = """
                  <person xmlns="urn:LinqToXsdCore:ListTypeWithDefaults" otherNames="mono duo trio">
                  </person>
                  """;

        var person = urn.LinqToXsdCore.ListTypeWithDefaults.person.Parse(xml);

        Assert.NotNull(person.otherNames);

        Assert.True(person.otherNames.Contains("mono"));
        Assert.True(person.otherNames.Contains("duo"));
        Assert.True(person.otherNames.Contains("trio"));
    }

    [Test]
    public void AttributeFeatureTestOtherNames2()
    {
        var xml = """
                  <person xmlns="urn:LinqToXsdCore:ListTypeWithDefaults" otherNames="">
                  </person>
                  """;

        var person = urn.LinqToXsdCore.ListTypeWithDefaults.person.Parse(xml);

        Assert.NotNull(person.otherNames);
        Assert.True(person.otherNames.Count == 0);
    }

    [Test]
    public void AttributeFeatureTestTagsDefaults()
    {
        var xml = """
                  <person xmlns="urn:LinqToXsdCore:ListTypeWithDefaults">
                  </person>
                  """;

        var person = urn.LinqToXsdCore.ListTypeWithDefaults.person.Parse(xml);

        Assert.NotNull(person.tags);
        Assert.True(person.tags.Count == 2);
    }
}