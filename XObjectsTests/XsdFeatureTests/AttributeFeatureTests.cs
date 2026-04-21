using System.Collections.Generic;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests.XsdFeatureTests;

public class AttributeFeatureTests
{
    [Test]
    public void AttributeFeatureTestOtherNamesWithCustomValues()
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
    public void AttributeFeatureTestMilestoneYearsWithDefaultValues()
    {
        var xml = """
                  <person xmlns="urn:LinqToXsdCore:ListTypeWithDefaults">
                  </person>
                  """;

        var person = urn.LinqToXsdCore.ListTypeWithDefaults.person.Parse(xml);

        Assert.NotNull(person.milestoneYears);
        Assert.IsNotEmpty(person.milestoneYears);
    }

    [Test]
    public void AttributeFeatureTestOtherNamesWithEmptyAttrValue()
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
        Assert.IsNotEmpty(person.tags);
    }

    [Test]
    public void AttributeFeatureTestTagsDefaultsThenSetCustomValues()
    {
        var xml = """
                  <person xmlns="urn:LinqToXsdCore:ListTypeWithDefaults">
                  </person>
                  """;

        var person = urn.LinqToXsdCore.ListTypeWithDefaults.person.Parse(xml);

        Assert.NotNull(person.tags);
        Assert.IsNotEmpty(person.tags);

        person.tags = new List<string>() { "male", "adult" };

        Assert.IsNotEmpty(person.tags);
        Assert.True(person.tags.Count == 2);
    }
}