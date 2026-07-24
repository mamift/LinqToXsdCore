using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Xml.Schema.Linq.Tests.Extensions;

namespace Xml.Schema.Linq.Tests;

[TestFixture]
public class FileSystemUtilitiesTests
{
    [Test]
    public void TestResolvePossibleFileAndFolderPathsToProcessableSchemasGithubIssue71()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory).AscendToFolder("XObjectsTests").AscendByLevel(1).DescendToFolder("GithubIssue71");

        IEnumerable<string> filesOrFolders = dir.GetFiles().Select(f => f.FullName);
        List<string>? entryPointSchemas =
            FileSystemUtilities.ResolvePossibleFileAndFolderPathsToProcessableSchemas(filesOrFolders);

        Assert.NotNull(entryPointSchemas);
        Assert.IsNotEmpty(entryPointSchemas);
        // The minimum set of entry points: V2G_CI_AppProtocol.xsd (standalone, no imports)
        // plus one representative from the cyclic SCC {MsgBody, MsgDataTypes, MsgDef, MsgHeader}
        // which transitively covers xmldsig-core-schema.xsd via MsgHeader's import.
        Assert.AreEqual(2, entryPointSchemas.Count);
        Assert.That(entryPointSchemas.Any(f => f.EndsWith("V2G_CI_AppProtocol.xsd")), "Should include the standalone AppProtocol schema");
        Assert.That(entryPointSchemas.Any(f => f.EndsWith("V2G_CI_MsgDataTypes.xsd")), "Should include one representative from the cyclic SCC");
    }

    [Test,TestCaseSource(nameof(GetPhysicalFolderPathsForGeneratedSchemaLibraries))]
    public void TestResolvePossibleFileAndFolderPathsToProcessableSchemas(object folderPath)
    {
        var dir = new DirectoryInfo((string)folderPath);
        IEnumerable<string> filesOrFolders = dir.GetFiles("*", SearchOption.AllDirectories).Select(f => f.FullName);
        List<string>? entryPointSchemas =
            FileSystemUtilities.ResolvePossibleFileAndFolderPathsToProcessableSchemas(filesOrFolders);

        Assert.NotNull(entryPointSchemas);
        Assert.IsNotEmpty(entryPointSchemas);
    }

    [Test, TestCaseSource(nameof(GetPhysicalFolderPathsForGeneratedSchemaLibraries))]
    public void TestGenerateImportIncludeReport(string folderPath)
    {
        var dir = new DirectoryInfo(folderPath);

        string[] report = FileSystemUtilities.GenerateImportIncludeReport(dir.FullName);

        Assert.NotNull(report);
        Assert.IsNotEmpty(report);
    }

    [Test]
    public void TestGenerateImportIncludeReportGitHubIssue71()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory).AscendToFolder("XObjectsTests").AscendByLevel(1).DescendToFolder("GithubIssue71");

        string[] report = FileSystemUtilities.GenerateImportIncludeReport(dir.FullName);

        Assert.NotNull(report);
        Assert.AreEqual(6, report.Length); // 6 XSD files in the folder

        // Find each file's line (each line starts with the file name)
        string FindLine(string fileName) => report.Single(l => l.StartsWith(fileName, StringComparison.OrdinalIgnoreCase));

        // AppProtocol: imports nothing
        Assert.AreEqual("V2G_CI_AppProtocol.xsd <- (none)", FindLine("V2G_CI_AppProtocol.xsd"));

        // MsgBody: imports MsgDef and MsgDataTypes
        Assert.AreEqual("V2G_CI_MsgBody.xsd <- imp: V2G_CI_MsgDataTypes.xsd, imp: V2G_CI_MsgDef.xsd", FindLine("V2G_CI_MsgBody.xsd"));

        // MsgDataTypes: imports MsgBody
        Assert.AreEqual("V2G_CI_MsgDataTypes.xsd <- imp: V2G_CI_MsgBody.xsd", FindLine("V2G_CI_MsgDataTypes.xsd"));

        // MsgDef: imports MsgHeader
        Assert.AreEqual("V2G_CI_MsgDef.xsd <- imp: V2G_CI_MsgHeader.xsd", FindLine("V2G_CI_MsgDef.xsd"));

        // MsgHeader: imports MsgDef, MsgDataTypes, and xmldsig-core-schema
        Assert.AreEqual("V2G_CI_MsgHeader.xsd <- imp: V2G_CI_MsgDataTypes.xsd, imp: V2G_CI_MsgDef.xsd, imp: xmldsig-core-schema.xsd", FindLine("V2G_CI_MsgHeader.xsd"));

        // xmldsig: imports nothing
        Assert.AreEqual("xmldsig-core-schema.xsd <- (none)", FindLine("xmldsig-core-schema.xsd"));
    }

    public static IEnumerable<object[]> GetPhysicalFolderPathsForGeneratedSchemaLibraries()
    {
        var cwd = new DirectoryInfo(Environment.CurrentDirectory);
        DirectoryInfo linqToXsdSlnFolder = cwd.AscendToFolder("LinqToXsdCore");
        FileInfo testingSuiteFilter = linqToXsdSlnFolder.GetFiles("LinqToXsd-TestingSuite.slnf").Single();
        using FileStream fileStream = testingSuiteFilter.OpenRead();
        var generatedSchemaLibProjectsInSlnFilter = JsonDocument.Parse(fileStream).RootElement
            .GetProperty("solution").GetProperty("projects");

        DirectoryInfo baseFolder = linqToXsdSlnFolder.DescendToFolder("GeneratedSchemaLibraries");

        foreach (var project in generatedSchemaLibProjectsInSlnFilter.EnumerateArray())
        {
            var projectPath = project.GetString();
            if (projectPath.EndsWith("XObjectsCore")) continue;
            var containerDir = Path.GetDirectoryName(projectPath);
            var fullyQualifiedDir = Path.Combine(linqToXsdSlnFolder.FullName, containerDir);
            if (!fullyQualifiedDir.Contains(baseFolder.Name)) continue;
            yield return new object[] { fullyQualifiedDir };
        }
    }
}