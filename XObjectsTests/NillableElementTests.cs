using System.Collections.Generic;
using LandXml_v1_2;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests;

public class NillableElementTests
{
    [Test]
    public void NonNullListValueForLandXmlNillableAdverseSeElementTest()
    {
        var landXmlObj = new Superelevation() {
            nillableAdverseSE = new List<adverseSEType?>() {
                adverseSEType.adverse,
                adverseSEType.non_adverse
            }
        };

        Assert.IsNotNull(landXmlObj.nillableAdverseSE);
    }

    [Test]
    public void NullListValueForLandXmlNillableAdverseSeElementTest()
    {
        var landXmlObj = new Superelevation() {
            nillableAdverseSE = null,
        };

        Assert.NotNull(landXmlObj.nillableAdverseSE);
        Assert.IsEmpty(landXmlObj.nillableAdverseSE);
    }
}