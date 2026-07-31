using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Tests.Extensions;

namespace Xml.Schema.Linq.Tests;

[TestFixture]
public class GraphTests
{
    [Test]
    public void TestBuildFromFolderSharePoint2010_XmlCompared()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var xmlString = """
                        <Graph xmlns="urn:LinqToXsdCore:Xml.Schema.Linq.CodeGen">
                         <Schema Name="CamlQuery.xsd">
                           <Includes>
                             <Schema Name="coredefinitions.xsd" />
                           </Includes>
                         </Schema>
                         <Schema Name="CamlView.xsd">
                           <Includes>
                             <Schema Name="coredefinitions.xsd" />
                             <Schema Name="CamlQuery.xsd" />
                           </Includes>
                         </Schema>
                         <Schema Name="CoreDefinitions.xsd" />
                         <Schema Name="cui.xsd" />
                         <Schema Name="WorkflowActions.xsd" />
                         <Schema Name="wss.xsd">
                           <Includes>
                             <Schema Name="camlview.xsd" />
                             <Schema Name="cui.xsd" />
                             <Schema Name="workflowActions.xsd" />
                           </Includes>
                         </Schema>
                        </Graph>
                        """;

        var xmlDoc = Graph.Parse(xmlString);

        Assert.AreEqual(graph.Schema.Count, xmlDoc.Schema.Count);

        var originalStr = graph.Untyped.ToString(SaveOptions.DisableFormatting);
        var parsedStr = xmlDoc.Untyped.ToString(SaveOptions.DisableFormatting);

        Assert.AreEqual(originalStr, parsedStr);
    }

    [Test]
    public void TestFindEntryPointSchemaNamesFromSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        List<string> entryPoints = graph.FindEntryPointSchemaNames();

        Assert.NotNull(entryPoints);
        Assert.AreEqual(1, entryPoints.Count);
        Assert.That(entryPoints.Single(), Is.EqualTo("wss.xsd").IgnoreCase);
    }

    [Test]
    public void TestFindEntryPointSchemasFromSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var entryPoints = graph.FindEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.AreEqual(1, entryPoints.Count);
    }

    [Test]
    public void TestSchemaEntryPointDependenciesForSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var entryPoints = graph.FindEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.AreEqual(1, entryPoints.Count);
        Assert.That(entryPoints.Single().Name, Is.EqualTo("wss.xsd").IgnoreCase);

        var allSchemaNames = graph.GetAllSchemaNames();
        
        Assert.IsNotEmpty(allSchemaNames);
        Assert.True(allSchemaNames.Count == dir.GetFiles("*.xsd", SearchOption.AllDirectories).Length);

        var linked = entryPoints.Single().GetDependencies().ToList();

        Assert.NotNull(linked);
    }

    [Test]
    public void TestSchemaEntryPointDependenciesRecursivelyForSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var entryPoints = graph.FindEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.AreEqual(1, entryPoints.Count);
        
        Linq.CodeGen.Schema theEntryPointSchema = entryPoints.Single();
        
        Assert.That(theEntryPointSchema.Name, Is.EqualTo("wss.xsd").IgnoreCase);

        var allSchemaNames = graph.GetAllSchemaNames();
        
        Assert.IsNotEmpty(allSchemaNames);
        Assert.True(allSchemaNames.Count == dir.GetFiles("*.xsd", SearchOption.AllDirectories).Length);

        var linkedRecursively = theEntryPointSchema.GetDependenciesRecursively().ToList();
        Assert.NotNull(linkedRecursively);
        
        Assert.True(linkedRecursively.Count == allSchemaNames.Count);
    }

    [Test]
    public void TestFindEntryPointSchemasFromOfficeOpenXMLXMLSchemaStrict()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("OfficeOpenXML-XMLSchema-Strict");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        List<Linq.CodeGen.Schema>? withIncludesImports = graph.GetSchemasThatImportsOrIncludeOthers();
        Assert.IsNotEmpty(withIncludesImports);

        List<Linq.CodeGen.Schema>? withoutIncludesImports = graph.GetSchemasThatDoNotImportAndIncludeOthers();
        Assert.IsNotEmpty(withoutIncludesImports);

        List<Linq.CodeGen.Schema>? includedByOthers = graph.GetSchemasThatAreIncludedByOthers();
        Assert.IsEmpty(includedByOthers);

        List<Linq.CodeGen.Schema>? importedByOthers = graph.GetSchemasThatAreImportedByOthers();
        Assert.IsNotEmpty(importedByOthers);

        List<string> entryPoints = graph.FindEntryPointSchemaNames();
        List<Linq.CodeGen.Schema>? entryPointSchemas = graph.FindEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.IsNotEmpty(entryPoints);

        Assert.AreEqual(entryPointSchemas.Count, entryPoints.Count);
    }

    [Test]
    public void TestFindEntryPointSchemasFromOfficeOpenXMLXMLSchemaTransitional()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("OfficeOpenXML-XMLSchema-Transitional");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        List<Linq.CodeGen.Schema>? withIncludesImports = graph.GetSchemasThatImportsOrIncludeOthers();
        Assert.IsNotEmpty(withIncludesImports);

        List<Linq.CodeGen.Schema>? withoutIncludesImports = graph.GetSchemasThatDoNotImportAndIncludeOthers();
        Assert.IsNotEmpty(withoutIncludesImports);

        List<Linq.CodeGen.Schema>? includedByOthers = graph.GetSchemasThatAreIncludedByOthers();
        Assert.IsEmpty(includedByOthers);

        List<Linq.CodeGen.Schema>? importedByOthers = graph.GetSchemasThatAreImportedByOthers();
        Assert.IsNotEmpty(importedByOthers);

        List<string> entryPoints = graph.FindEntryPointSchemaNames();
        List<Linq.CodeGen.Schema>? entryPointSchemas = graph.FindEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.IsNotEmpty(entryPoints);

        Assert.AreEqual(entryPointSchemas.Count, entryPoints.Count);
    }

    [Test, TestCase("SharePoint2010")]
    public void TestFindEntryPointSchemasCanCompileXmlSchemaSet(string folderName)
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder(folderName);
        Graph graph = Graph.BuildFromFolder(dir.FullName);
        
        List<Linq.CodeGen.Schema>? entryPointSchemas = graph.FindEntryPointSchemas();
        Assert.IsNotEmpty(entryPointSchemas);
    }

    public static DirectoryInfo GetGeneratedSchemaLibraryFolder(string folder)
    {
        if (folder == null) throw new ArgumentNullException(nameof(folder));

        return new DirectoryInfo(Environment.CurrentDirectory)
            .AscendToFolder("XObjectsTests")
            .AscendByLevel(1)
            .DescendToFolder(folder);
    }
}