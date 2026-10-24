using System.Collections.Generic;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests;

public class LinqToXsdConfigurationTests
{
    [Test]
    public void TestSetDefaultVisibility_EnumValueIsInternal()
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

    [TestCase(null, null)]
    [TestCase("Class", SplitCodeFiles.ByEnum.Class)]
    [TestCase("Namespace", SplitCodeFiles.ByEnum.Namespace)]
    public void TestSplitCodeFilesByIsReadFromConfiguration(string byAttributeValue, SplitCodeFiles.ByEnum? expected)
    {
        var splitCodeFilesElement = byAttributeValue == null ? string.Empty : $"<SplitCodeFiles By=\"{byAttributeValue}\" />";
        var configXml =
            $@"<Configuration xmlns=""http://www.microsoft.com/xml/schema/linq"">
  <CodeGeneration>
    <UseDateOnly>false</UseDateOnly>
    <UseTimeOnly>false</UseTimeOnly>
    <UseDateTimeOffset>false</UseDateTimeOffset>
    <NullableReferences>false</NullableReferences>
    {splitCodeFilesElement}
  </CodeGeneration>
  <Namespaces>
    <Namespace DefaultVisibility=""public"" Schema=""urn:test"" Clr=""urn.test"" />
  </Namespaces>
</Configuration>";

        var settings = new LinqToXsdSettings();
        settings.Load(System.Xml.Linq.XDocument.Parse(configXml));

        Assert.AreEqual(expected, settings.SplitCodeFilesBy);
    }
}