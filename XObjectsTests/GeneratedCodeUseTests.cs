using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests;

public class GeneratedCodeUseTests: BaseTester
{
    [Test]
    public void MinskXmlBasicLoadTest()
    {
        MockFileSystem fs = GetFileSystemForAssemblyName("LinqToXsd.Schemas");

        Assert.IsNotNull(fs);

        string minskXml = fs.AllFiles.Single(f => f.EndsWith("minsk.xml"));
        IFileInfo minskXmlFi = fs.FileInfo.New(minskXml);

        Assert.NotNull(minskXmlFi);
        Assert.True(minskXmlFi.Length > 0);

        using StreamReader streamReader = minskXmlFi.OpenText();
        var grammar = Mamift.Minsk.Grammar.Load(streamReader);

        Assert.NotNull(grammar);
        Assert.Null(grammar.Imports);
        Assert.NotNull(grammar.Keywords);
        Assert.IsNotEmpty(grammar.Keywords.Keyword);

        Assert.NotNull(grammar.PrecedenceLevels);
        Assert.IsNotEmpty(grammar.PrecedenceLevels.Level);

        Assert.NotNull(grammar.Rules);
        Assert.IsNotEmpty(grammar.Rules.ChoiceRule);
        Assert.IsNotEmpty(grammar.Rules.ExpressionRule);
        Assert.IsEmpty(grammar.Rules.Extend);
        Assert.IsNotEmpty(grammar.Rules.NodeRule);

        Assert.NotNull(grammar.Tokens);
        Assert.IsNotEmpty(grammar.Tokens.Token);

        Assert.NotNull(grammar.Trivia);
        Assert.IsNotEmpty(grammar.Trivia.TriviaRule);

        Assert.NotNull(grammar.@namespace);
        Assert.NotNull(grammar.name);
    }
}