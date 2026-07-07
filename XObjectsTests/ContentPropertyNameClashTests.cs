using System.Xml.Linq;
using echem.contentBug;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests;

public class ContentPropertyNameClashTests: BaseTester
{
    [Test]
    public void TestThatNameClashedPropertiesStillGenerateProperXmlString()
    {
        echem.contentBug.Comment comment = new echem.contentBug.Comment();

        comment.Content1 = new Content() {
            TypedValue = "nmTokenString"
        };

        var xmlString = """
                        <Comment xmlns="urn:cidx:names:specification:ces:schema:all:5:3">
                          <Content>nmTokenString</Content>
                        </Comment>
                        """.Trim();

        Assert.IsTrue(XNode.DeepEquals(XElement.Parse(xmlString), comment.Untyped));
    }
}