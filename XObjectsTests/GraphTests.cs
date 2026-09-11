using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;
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

        // just for comparison purposes, we don't care about the folder path or relative paths
        graph.Folder = null;
        foreach (var element in graph.Untyped.Descendants().Where(a => a.Attributes("RelativePath").Any()))
        {
            var attr = element.Attribute("RelativePath");
            if (attr != null) attr.Remove();
        }

        var xmlString = """
                        <Graph xmlns="https://github.com/mamift/LinqToXsdCore">
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

        List<string> entryPoints = graph.GetEntryPointSchemaNames();

        Assert.NotNull(entryPoints);
        Assert.AreEqual(1, entryPoints.Count);
        Assert.That(entryPoints.Single(), Is.EqualTo("wss.xsd").IgnoreCase);
    }

    [Test]
    public void TestFindEntryPointSchemasFromSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var entryPoints = graph.GetEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.AreEqual(1, entryPoints.Count);
    }

    [Test]
    public void TestSchemaEntryPointDependenciesForSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var entryPoints = graph.GetEntryPointSchemas();

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

        List<Linq.CodeGen.Schema> entryPoints = graph.GetEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.AreEqual(1, entryPoints.Count);
        
        Linq.CodeGen.Schema theEntryPointSchema = entryPoints.Single();
        
        Assert.That(theEntryPointSchema.Name, Is.EqualTo("wss.xsd").IgnoreCase);

        List<string> allSchemaNames = graph.GetAllSchemaNames();
        
        Assert.IsNotEmpty(allSchemaNames);
        Assert.True(allSchemaNames.Count == dir.GetFiles("*.xsd", SearchOption.AllDirectories).Length);

        List<Linq.CodeGen.Schema> linkedRecursively = theEntryPointSchema.GetDependenciesRecursively().ToList();
        Assert.NotNull(linkedRecursively);
        Assert.IsNotEmpty(linkedRecursively);

        Assert.True(linkedRecursively.All(s => allSchemaNames.Contains(s.Name)));
        Assert.True(linkedRecursively.Count == allSchemaNames.Count-1);
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

        List<string> entryPoints = graph.GetEntryPointSchemaNames();
        List<Linq.CodeGen.Schema>? entryPointSchemas = graph.GetEntryPointSchemas();

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

        List<string> entryPoints = graph.GetEntryPointSchemaNames();
        List<Linq.CodeGen.Schema>? entryPointSchemas = graph.GetEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.IsNotEmpty(entryPoints);

        Assert.AreEqual(entryPointSchemas.Count, entryPoints.Count);
    }

    [Test, TestCase("SharePoint2010")]
    public void TestFindEntryPointSchemasCanCompileXmlSchemaSet(string folderName)
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder(folderName);
        Graph graph = Graph.BuildFromFolder(dir.FullName);
        
        List<Linq.CodeGen.Schema>? entryPointSchemas = graph.GetEntryPointSchemas();
        Assert.IsNotEmpty(entryPointSchemas);
    }

    [Test]
    public void TestTraverseSchemasSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var entryPoints = graph.GetEntryPointSchemas();
        var traversed = entryPoints.TraverseSchemas(graph);

        Assert.IsNotNull(traversed);
        Assert.AreEqual(graph.Schema.Count, traversed.Count);
        Assert.That(traversed.First().Name, Is.EqualTo("wss.xsd").IgnoreCase);

        // Traversing all schemas in graph filters to valid entry points and traverses them
        var traversedAll = graph.Schema.TraverseSchemas(graph);
        Assert.IsNotNull(traversedAll);
        Assert.AreEqual(graph.Schema.Count, traversedAll.Count);

        // Passing non-entrypoint schemas results in empty reachable list because none are entry points
        var nonEntryPoints = graph.Schema.Where(s => !s.Name.EqualsIgnoreCase("wss.xsd")).ToList();
        var traversedNonEntry = nonEntryPoints.TraverseSchemas(graph);
        Assert.IsNotNull(traversedNonEntry);
        Assert.IsEmpty(traversedNonEntry);
    }

    [Test]
    public void TestTraverseSchemasOfficeOpenXMLStrict()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("OfficeOpenXML-XMLSchema-Strict");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var entryPoints = graph.GetEntryPointSchemas();
        var traversed = entryPoints.TraverseSchemas(graph);

        Assert.IsNotNull(traversed);
        Assert.IsNotEmpty(traversed);
        Assert.IsTrue(traversed.Count >= entryPoints.Count);
    }

    [Test]
    public void TestTraverseSchemasWithCycles()
    {
        var xmlString = """
                        <Graph xmlns="https://github.com/mamift/LinqToXsdCore">
                         <Schema Name="a.xsd">
                           <Includes>
                             <Schema Name="b.xsd" />
                           </Includes>
                         </Schema>
                         <Schema Name="b.xsd">
                           <Includes>
                             <Schema Name="a.xsd" />
                           </Includes>
                         </Schema>
                        </Graph>
                        """;

        var graph = Graph.Parse(xmlString);
        var entryPoints = graph.GetEntryPointSchemas();
        Assert.AreEqual(1, entryPoints.Count);

        var traversed = entryPoints.TraverseSchemas(graph);
        Assert.IsNotNull(traversed);
        Assert.AreEqual(2, traversed.Count);
    }

    [Test]
    public void TestTraverseSchemasNullAndEmpty()
    {
        var graph = new Graph();
        Assert.Throws<ArgumentNullException>(() => ((List<Linq.CodeGen.Schema>)null!).TraverseSchemas(graph));
        Assert.Throws<ArgumentNullException>(() => new List<Linq.CodeGen.Schema>().TraverseSchemas(null!));

        var result = new List<Linq.CodeGen.Schema>().TraverseSchemas(graph);
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [Test]
    public void TestBuildFromMicrosoftSearchXsd()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("Microsoft Search");
        
        Graph graph = Graph.BuildFromFolder(dir.FullName);
        
        Assert.IsNotNull(graph);

        var connected = graph.GetConnectedSchemaNames();
        var standalone = graph.GetDisconnectedSchemaNames();
        
        Assert.IsNotEmpty(connected);
        Assert.IsNotEmpty(standalone);
    }

    [Test]
    public void TestGetConnectedSchemasSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var connected = graph.GetConnectedSchemas();
        var connectedNames = graph.GetConnectedSchemaNames();
        var disconnected = graph.GetDisconnectedSchemas();
        var disconnectedNames = graph.GetDisconnectedSchemaNames();

        Assert.IsNotNull(connected);
        Assert.AreEqual(graph.Schema.Count, connected.Count);
        Assert.AreEqual(graph.Schema.Count, connectedNames.Count);
        Assert.IsEmpty(disconnected);
        Assert.IsEmpty(disconnectedNames);
    }

    [Test]
    public void TestGetConnectedAndDisconnectedSchemasWithIsolated()
    {
        var xmlString = """
                        <Graph xmlns="https://github.com/mamift/LinqToXsdCore">
                         <Schema Name="a.xsd">
                           <Includes>
                             <Schema Name="b.xsd" />
                           </Includes>
                         </Schema>
                         <Schema Name="b.xsd" />
                         <Schema Name="standalone.xsd" />
                        </Graph>
                        """;

        var graph = Graph.Parse(xmlString);

        var connected = graph.GetConnectedSchemas();
        var connectedNames = graph.GetConnectedSchemaNames();
        var disconnected = graph.GetDisconnectedSchemas();
        var disconnectedNames = graph.GetDisconnectedSchemaNames();

        Assert.AreEqual(2, connected.Count);
        Assert.That(connectedNames, Does.Contain("a.xsd"));
        Assert.That(connectedNames, Does.Contain("b.xsd"));
        Assert.That(connectedNames, Does.Not.Contain("standalone.xsd"));

        Assert.AreEqual(1, disconnected.Count);
        Assert.AreEqual("standalone.xsd", disconnected.Single().Name);
        Assert.AreEqual("standalone.xsd", disconnectedNames.Single());
    }

    [Test]
    public void TestGetConnectedSchemasWithImportsAndIncludes()
    {
        var xmlString = """
                        <Graph xmlns="https://github.com/mamift/LinqToXsdCore">
                         <Schema Name="importer.xsd">
                           <Imports>
                             <Schema Name="imported.xsd" />
                           </Imports>
                         </Schema>
                         <Schema Name="imported.xsd" />
                         <Schema Name="includer.xsd">
                           <Includes>
                             <Schema Name="included.xsd" />
                           </Includes>
                         </Schema>
                         <Schema Name="included.xsd" />
                         <Schema Name="isolated1.xsd" />
                         <Schema Name="isolated2.xsd" />
                        </Graph>
                        """;

        var graph = Graph.Parse(xmlString);

        var connected = graph.GetConnectedSchemas();
        var connectedNames = graph.GetConnectedSchemaNames();
        var disconnected = graph.GetDisconnectedSchemas();
        var disconnectedNames = graph.GetDisconnectedSchemaNames();

        Assert.AreEqual(4, connected.Count);
        Assert.That(connectedNames, Is.EquivalentTo(new[] { "importer.xsd", "imported.xsd", "includer.xsd", "included.xsd" }));

        Assert.AreEqual(2, disconnected.Count);
        Assert.That(disconnectedNames, Is.EquivalentTo(new[] { "isolated1.xsd", "isolated2.xsd" }));
    }

    [Test]
    public void TestGetConnectedSchemasEmpty()
    {
        var graph = new Graph();

        var connected = graph.GetConnectedSchemas();
        var connectedNames = graph.GetConnectedSchemaNames();
        var disconnected = graph.GetDisconnectedSchemas();
        var disconnectedNames = graph.GetDisconnectedSchemaNames();

        Assert.IsEmpty(connected);
        Assert.IsEmpty(connectedNames);
        Assert.IsEmpty(disconnected);
        Assert.IsEmpty(disconnectedNames);
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