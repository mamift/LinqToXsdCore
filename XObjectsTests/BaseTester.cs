#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Xml.Schema.Linq.Tests.Extensions;

namespace Xml.Schema.Linq.Tests;

public class BaseTester
{
    protected List<Assembly> TestAssembliesLoaded { get; private set; } = null!;
    public MockFileSystem AllTestFiles { get; protected set; } = null!;

    [SetUp]
    public void Setup()
    {
        var current = Assembly.GetExecutingAssembly();
        var location = new DirectoryInfo(Path.GetDirectoryName(current.Location)!);
        var allDlls = location.GetFileSystemInfos("*.dll", SearchOption.AllDirectories);
        var testDlls = allDlls.Where(a => !(a.Name.Contains("System.") || a.Name.Contains("Microsoft.") || a.Name.Contains("MoreLinq") || a.Name.Contains("LinqToXsd") || 
                                            a.Name.Contains("nunit") || a.Name.Contains("Fasterflect"))).ToList();
        var referencedAssemblies = testDlls.OrderBy(a => a.FullName).ToList();

        TestAssembliesLoaded = referencedAssemblies.Select(name => Assembly.LoadFile(name.FullName)).ToList();
            
        AllTestFiles = Utilities.GetAggregateMockFileSystem(TestAssembliesLoaded);
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
}