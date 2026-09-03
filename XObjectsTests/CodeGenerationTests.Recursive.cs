using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using OneOf;

namespace Xml.Schema.Linq.Tests;

public partial class CodeGenerationTests: BaseTester
{
    /// <summary>
    /// Regression test for an XSD with recursively nested group definitions that previously caused a StackOverflowException during code generation.
    /// </summary>
    [Test]
    public void RecursiveStackOverflowBugTest()
    {
        var buggySchemaFilename = "RecursiveGroupStackoverflowBug.schema.xsd";
        var buggySchema = AllTestFiles.FileInfo.New(AllTestFiles.AllFiles.Single(f => f.EndsWith(buggySchemaFilename)));
        
        Assert.IsNotNull(buggySchema);
        Assert.True(buggySchema.Length > 0);

        OneOf<CSharpSyntaxTree, ExceptionDispatchInfo> genResult = Utilities.GenerateSyntaxTreeOrError(buggySchema, AllTestFiles);
        if (genResult.IsT1)
        {
            genResult.AsT1.Throw();
        }

        var tree = genResult.AsT0;
        var diags = Utilities.GetSyntaxAndCompilationDiagnostics(tree);
        Assert.IsEmpty(diags);
    }
}