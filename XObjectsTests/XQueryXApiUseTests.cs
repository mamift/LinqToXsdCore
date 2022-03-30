using NUnit.Framework;
using W3C.XQueryX;

namespace Xml.Schema.Linq.Tests;

[TestFixture]
public class XQueryXApiUseTests
{
    [Test]
    public void TestFunctionalConstructorOfTypeWithInnerEnumType()
    {
        Assert.DoesNotThrow(() => new orderingModeDecl());

        Assert.DoesNotThrow(() => {
            var orderedName = nameof(orderingModeDeclType.ordered);
            var unorderedName = nameof(orderingModeDeclType.unordered);

            var orderingMode1 = new orderingModeDecl(orderedName);
            var orderingMode2 = new orderingModeDecl(unorderedName);
        });
    }
}