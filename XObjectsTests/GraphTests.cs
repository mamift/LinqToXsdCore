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
                           <IncludedBy>CamlView.xsd</IncludedBy>
                         </Schema>
                         <Schema Name="CamlView.xsd">
                           <Includes>
                             <Schema Name="coredefinitions.xsd" />
                             <Schema Name="CamlQuery.xsd" />
                           </Includes>
                           <IncludedBy>wss.xsd</IncludedBy>
                         </Schema>
                         <Schema Name="CoreDefinitions.xsd">
                           <IncludedBy>CamlQuery.xsd;CamlView.xsd</IncludedBy>
                         </Schema>
                         <Schema Name="cui.xsd">
                           <IncludedBy>wss.xsd</IncludedBy>
                         </Schema>
                         <Schema Name="WorkflowActions.xsd">
                           <IncludedBy>wss.xsd</IncludedBy>
                         </Schema>
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
    public void TestGetEntrypointSchemasFromMicrosoftSearchXsd()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("Microsoft Search");
        
        Graph graph = Graph.BuildFromFolder(dir.FullName);
        
        Assert.IsNotNull(graph);

        var entryPoint = graph.GetEntryPointSchemas();
        
        Assert.IsNotEmpty(entryPoint);
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

    [Test]
    public void TestBuildFromFolderPopulatesIncludedBySharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var wss = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("wss.xsd"));
        var camlView = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("CamlView.xsd"));
        var camlQuery = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("CamlQuery.xsd"));
        var coreDefs = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("CoreDefinitions.xsd"));
        var cui = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("cui.xsd"));
        var workflowActions = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("WorkflowActions.xsd"));

        Assert.IsNull(wss.IncludedBy);

        Assert.IsNotNull(camlView.IncludedBy);
        Assert.That(camlView.IncludedByList, Is.EquivalentTo(new[] { "wss.xsd" }));

        Assert.IsNotNull(camlQuery.IncludedBy);
        Assert.That(camlQuery.IncludedByList, Is.EquivalentTo(new[] { "CamlView.xsd" }));

        Assert.IsNotNull(coreDefs.IncludedBy);
        Assert.That(coreDefs.IncludedByList, Is.EquivalentTo(new[] { "CamlQuery.xsd", "CamlView.xsd" }));

        Assert.IsNotNull(cui.IncludedBy);
        Assert.That(cui.IncludedByList, Is.EquivalentTo(new[] { "wss.xsd" }));

        Assert.IsNotNull(workflowActions.IncludedBy);
        Assert.That(workflowActions.IncludedByList, Is.EquivalentTo(new[] { "wss.xsd" }));
    }

    [Test]
    public void TestBuildFromFolderPopulatesImportedByOfficeOpenXMLStrict()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("OfficeOpenXML-XMLSchema-Strict");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        var importedSchemas = graph.Schema.Where(s => s.ImportedBy != null && s.ImportedBy.Any()).ToList();
        Assert.IsNotEmpty(importedSchemas);

        foreach (var imported in importedSchemas)
        {
            Assert.IsNotNull(imported.ImportedByList);
            Assert.IsNotEmpty(imported.ImportedByList);
            foreach (var importerName in imported.ImportedByList)
            {
                var importer = graph.Schema.FirstOrDefault(s => s.Name.EqualsIgnoreCase(importerName));
                Assert.IsNotNull(importer);
                Assert.IsNotNull(importer.Imports?.Schema);
                Assert.IsTrue(importer.Imports.Schema.Any(i => Path.GetFileName(i.Name).EqualsIgnoreCase(imported.Name)));
            }
        }
    }

    [Test]
    public void TestBuildFromFolderPopulatesIncludedByAndImportedByInTempFolder()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "LinqToXsdGraphTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            string baseSchema = """
                                <?xml version="1.0" encoding="utf-8"?>
                                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="http://example.com/base">
                                  <xs:element name="BaseElement" type="xs:string"/>
                                </xs:schema>
                                """;

            string includedSchema = """
                                    <?xml version="1.0" encoding="utf-8"?>
                                    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="http://example.com/main">
                                      <xs:element name="IncElement" type="xs:string"/>
                                    </xs:schema>
                                    """;

            string mainSchema = """
                                <?xml version="1.0" encoding="utf-8"?>
                                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="http://example.com/main" xmlns:b="http://example.com/base">
                                  <xs:include schemaLocation="included.xsd"/>
                                  <xs:import namespace="http://example.com/base" schemaLocation="base.xsd"/>
                                  <xs:element name="MainElement" type="xs:string"/>
                                </xs:schema>
                                """;

            string standaloneSchema = """
                                      <?xml version="1.0" encoding="utf-8"?>
                                      <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="http://example.com/standalone">
                                        <xs:element name="StandaloneElement" type="xs:string"/>
                                      </xs:schema>
                                      """;

            File.WriteAllText(Path.Combine(tempDir, "base.xsd"), baseSchema);
            File.WriteAllText(Path.Combine(tempDir, "included.xsd"), includedSchema);
            File.WriteAllText(Path.Combine(tempDir, "main.xsd"), mainSchema);
            File.WriteAllText(Path.Combine(tempDir, "standalone.xsd"), standaloneSchema);

            Graph graph = Graph.BuildFromFolder(tempDir);

            var main = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("main.xsd"));
            var included = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("included.xsd"));
            var @base = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("base.xsd"));
            var standalone = graph.Schema.Single(s => s.Name.EqualsIgnoreCase("standalone.xsd"));

            Assert.IsNull(main.IncludedBy);
            Assert.IsNull(main.ImportedBy);

            Assert.IsNotNull(included.IncludedBy);
            Assert.That(included.IncludedByList, Is.EquivalentTo(new[] { "main.xsd" }));
            Assert.IsNull(included.ImportedBy);

            Assert.IsNotNull(@base.ImportedBy);
            Assert.That(@base.ImportedByList, Is.EquivalentTo(new[] { "main.xsd" }));
            Assert.IsNull(@base.IncludedBy);

            Assert.IsNull(standalone.IncludedBy);
            Assert.IsNull(standalone.ImportedBy);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void TestGetRootSchemasSharePoint2010()
    {
        DirectoryInfo dir = GetGeneratedSchemaLibraryFolder("SharePoint2010");
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        List<Linq.CodeGen.Schema> rootSchemas = graph.GetRootSchemas();

        Assert.IsNotNull(rootSchemas);
        Assert.AreEqual(1, rootSchemas.Count);
        Assert.That(rootSchemas.Single().Name, Is.EqualTo("wss.xsd").IgnoreCase);
    }

    [Test]
    public void TestGetRootSchemasWithImportsAndIncludes()
    {
        var xmlString = """
                        <Graph xmlns="https://github.com/mamift/LinqToXsdCore">
                         <Schema Name="root1.xsd">
                           <Imports>
                             <Schema Name="child1.xsd" />
                           </Imports>
                         </Schema>
                         <Schema Name="root2.xsd">
                           <Includes>
                             <Schema Name="child2.xsd" />
                           </Includes>
                         </Schema>
                         <Schema Name="child1.xsd">
                           <Includes>
                             <Schema Name="subchild.xsd" />
                           </Includes>
                         </Schema>
                         <Schema Name="child2.xsd" />
                         <Schema Name="subchild.xsd" />
                         <Schema Name="standalone.xsd" />
                        </Graph>
                        """;

        var graph = Graph.Parse(xmlString);

        var rootSchemas = graph.GetRootSchemas();

        Assert.IsNotNull(rootSchemas);
        Assert.AreEqual(2, rootSchemas.Count);
        var rootNames = rootSchemas.Select(s => s.Name).ToList();
        Assert.That(rootNames, Is.EquivalentTo(new[] { "root1.xsd", "root2.xsd" }));
    }

    [Test]
    public void TestGetRootSchemasIsolatedOnly()
    {
        var xmlString = """
                        <Graph xmlns="https://github.com/mamift/LinqToXsdCore">
                         <Schema Name="standalone1.xsd" />
                         <Schema Name="standalone2.xsd" />
                        </Graph>
                        """;

        var graph = Graph.Parse(xmlString);

        var rootSchemas = graph.GetRootSchemas();

        Assert.IsNotNull(rootSchemas);
        Assert.IsEmpty(rootSchemas);
    }

    [Test]
    public void TestGetRootSchemasCyclic()
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

        var rootSchemas = graph.GetRootSchemas();

        Assert.IsNotNull(rootSchemas);
        Assert.IsEmpty(rootSchemas);
    }

    [Test]
    public void TestGetRootSchemasEmpty()
    {
        var graph = new Graph();

        var rootSchemas = graph.GetRootSchemas();

        Assert.IsNotNull(rootSchemas);
        Assert.IsEmpty(rootSchemas);
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