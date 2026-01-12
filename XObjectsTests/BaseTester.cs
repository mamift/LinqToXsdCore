#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Xml.Schema.Linq.Tests.Extensions;

namespace Xml.Schema.Linq.Tests;

public class BaseTester
{
    public List<Assembly> TestAssembliesLoaded { get; protected set; } = null!;
    public MockFileSystem AllTestFiles { get; protected set; } = null!;
    public MockXmlUrlResolver MockXmlFileResolver { get; protected set; } = null!;

    /// <summary>
    /// This setup method will tee-up some helpful reference data that are used in testing the code gen output.
    /// <para>It will filter out assemblies that are not relevant for <see cref="TestAssembliesLoaded"/>.</para>
    /// </summary>
    [SetUp]
    public void Setup()
    {
        var current = Assembly.GetExecutingAssembly();
        var location = new DirectoryInfo(Path.GetDirectoryName(current.Location)!);
        var allDlls = location.GetFileSystemInfos("*.dll", SearchOption.AllDirectories);
        var testDlls = allDlls.Where(a => 
            !(
                a.Name.Contains("System.") || a.Name.Contains("Microsoft.") || a.Name.Contains("MoreLinq") || a.Name.Contains("nunit") || a.Name.Contains("Fasterflect") ||
                a.Name.Equals("LinqToXsd.dll") || a.Name.Contains("XObjects")
            )
        ).ToList();
        var referencedAssemblies = testDlls.OrderBy(a => a.FullName).ToList();

        TestAssembliesLoaded = referencedAssemblies.Select(name => Assembly.LoadFile(name.FullName)).ToList();
            
        AllTestFiles = Utilities.GetAggregateMockFileSystem(TestAssembliesLoaded);
        MockXmlFileResolver = new MockXmlUrlResolver(AllTestFiles);
    }

    public CSharpSyntaxTree GenerateSyntaxTree(MockFileInfo mfi)
    {
        return Utilities.GenerateSyntaxTree(mfi, AllTestFiles);
    }

    public IEnumerable<MockFileSystem> GetFileSystemForAssemblyNames(IEnumerable<string> assemblyNames)
    {
        foreach (var assemblyName in assemblyNames) {
            yield return GetFileSystemForAssemblyName(assemblyName);
        }
    }

    public MockFileSystem GetFileSystemForAssemblyName(string assemblyName)
    {
        Assembly fileSystemForAssemblyName = TestAssembliesLoaded.Single(a => a.GetName().Name == assemblyName);
        return Utilities.GetAssemblyFileSystem(fileSystemForAssemblyName);
    }

    public StreamReader GetFileStreamReader(string nonRootedPath)
    {
        return GetFile(nonRootedPath).ToStreamReader();
    }

    public IFileInfo GetFile(string nonRootedPath)
    {
        return AllTestFiles.AllFiles.Where(f => f.EndsWith(nonRootedPath)).Select(f => AllTestFiles.FileInfo.New(f)).First();
    }

    public XmlSchemaSet GetTestFileAsXmlSchemaSet(string endsWithFilePattern)
    {
        if (endsWithFilePattern == null) throw new ArgumentNullException(nameof(endsWithFilePattern));

        var file = AllTestFiles.AllFiles.SingleOrDefault(f => f.EndsWith(endsWithFilePattern));
        Assert.NotNull(file);
        return AllTestFiles.FileInfo.New(file).ReadAsXmlSchemaSet(MockXmlFileResolver);
    }
    
    public XmlSchema GetTestFileAsXmlSchema(string endsWithFilePattern)
    {
        if (endsWithFilePattern == null) throw new ArgumentNullException(nameof(endsWithFilePattern));

        var file = AllTestFiles.AllFiles.SingleOrDefault(f => f.EndsWith(endsWithFilePattern));
        Assert.NotNull(file);
        return AllTestFiles.FileInfo.New(file).ReadAsXmlSchema();
    }
}