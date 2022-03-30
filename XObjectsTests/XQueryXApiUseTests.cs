using System;
using NUnit.Framework;
using W3C.XQueryX;

namespace Xml.Schema.Linq.Tests;

[TestFixture]
public class XQueryXApiUseTests
{
    [Test]
    public void TestFunctionalConstructorOfTypeWithInnerEnumTypeBadValue()
    {
        Assert.Throws<ArgumentException>(() => {
            var orderingMode = new orderingModeDecl("badValue");
        });
        
        Assert.Throws<ArgumentException>(() => {
            var boundarySpaceDecl = new boundarySpaceDecl("badValue");
        });
        
        Assert.Throws<ArgumentException>(() => {
            var constructionDecl = new constructionDecl("badValue");
        });
    }

    [Test]
    public void TestFunctionalConstructorOfTypeWithInnerEnumTypeGoodValue()
    {
        Assert.DoesNotThrow(() => new orderingModeDecl());
        Assert.DoesNotThrow(() => new boundarySpaceDecl());
        Assert.DoesNotThrow(() => new constructionDecl());

        Assert.DoesNotThrow(() => {
            var orderedName = nameof(orderingModeDeclType.ordered);
            var unorderedName = nameof(orderingModeDeclType.unordered);

            var orderingMode1 = new orderingModeDecl(orderedName);
            var orderingMode2 = new orderingModeDecl(unorderedName);
        });
        
        Assert.DoesNotThrow(() => {
            var preserveName = nameof(boundarySpaceDeclType.preserve);
            var stripName = nameof(boundarySpaceDeclType.strip);

            var boundarySpaceDecl1 = new boundarySpaceDecl(preserveName);
            var boundarySpaceDecl2 = new boundarySpaceDecl(stripName);
        });
        
        Assert.DoesNotThrow(() => {
            var preserveName = nameof(constructionDeclType.preserve);
            var stripName = nameof(constructionDeclType.strip);

            var constructionDecl1 = new constructionDecl(preserveName);
            var constructionDecl2 = new constructionDecl(stripName);
        });
    }
}