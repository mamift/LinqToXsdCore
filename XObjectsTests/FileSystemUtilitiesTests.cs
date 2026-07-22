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
    [Test,TestCaseSource(nameof(GetPhysicalFolderPathsForGeneratedSchemaLibraries))]
    public void TestResolvePossibleFileAndFolderPathsToProcessableSchemas(object folderPath)
    {
        var dir = new DirectoryInfo((string)folderPath);
        IEnumerable<string> filesOrFolders = dir.GetFiles().Select(f => f.FullName);
        List<string>? entryPointSchemas =
            FileSystemUtilities.ResolvePossibleFileAndFolderPathsToProcessableSchemas(filesOrFolders);

        Assert.NotNull(entryPointSchemas);
    }

    public void TestResolvePossibleFileAndFolderPathsToProcessableSchemasGithubIssue71()
    {
        string folderPath = "";
        var dir = new DirectoryInfo((string)folderPath);
        IEnumerable<string> filesOrFolders = dir.GetFiles().Select(f => f.FullName);
        List<string>? entryPointSchemas =
            FileSystemUtilities.ResolvePossibleFileAndFolderPathsToProcessableSchemas(filesOrFolders);

        Assert.NotNull(entryPointSchemas);
    }

    private static IEnumerable<object[]> GetPhysicalFolderPathsForGeneratedSchemaLibraries()
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
            var containerDir = Path.GetDirectoryName(projectPath);
            var fullyQualifiedDir = Path.Combine(linqToXsdSlnFolder.FullName, containerDir);
            yield return new object[] { fullyQualifiedDir };
        }
    }
}