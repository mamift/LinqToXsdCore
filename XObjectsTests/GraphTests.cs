using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Tests.Extensions;

namespace Xml.Schema.Linq.Tests;

[TestFixture]
public class GraphTests
{
    [Test]
    public void TestBuildFromFolder()
    {
        DirectoryInfo dir = new DirectoryInfo(Environment.CurrentDirectory).AscendToFolder("XObjectsTests").AscendByLevel(1).DescendToFolder("SharePoint2010");
        
        Graph graph = Graph.BuildFromFolder(dir.FullName);
        /* as XML for the SharePoint 2010 folder it looks like
         
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
        */
    }

    [Test]
    public void TestFindEntryPointSchemasFromSharePoint2010()
    {
        DirectoryInfo dir = GetSharePoint2010Folder();
        Graph graph = Graph.BuildFromFolder(dir.FullName);

        List<string> entryPoints = graph.FindEntryPointSchemas();

        Assert.NotNull(entryPoints);
        Assert.AreEqual(1, entryPoints.Count);
        Assert.That(entryPoints.Single(), Is.EqualTo("wss.xsd").IgnoreCase);
    }

    [Test]
    public void TestFindEntryPointSchemasCanCompileXmlSchemaSet()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"LinqToXsdCore.GraphTests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            string rootSchemaPath = Path.Combine(tempFolder, "Root.xsd");
            File.WriteAllText(rootSchemaPath,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"urn:test\" elementFormDefault=\"qualified\">" +
                "  <xs:element name=\"Root\" type=\"xs:string\" />" +
                "</xs:schema>");

            Graph graph = Graph.BuildFromFolder(tempFolder);
            List<string> entryPoints = graph.FindEntryPointSchemas();

            Assert.That(entryPoints, Is.Not.Empty);
            Assert.That(entryPoints.Count, Is.EqualTo(1));
            Assert.That(entryPoints.Single(), Is.EqualTo("Root.xsd").IgnoreCase);

            var filesByName = new DirectoryInfo(tempFolder).GetFiles("*.xsd", SearchOption.TopDirectoryOnly)
                .ToDictionary(f => f.Name, f => f.FullName, StringComparer.OrdinalIgnoreCase);

            var errors = new List<string>();
            var schemaSet = new XmlSchemaSet();
            schemaSet.ValidationEventHandler += (_, args) =>
            {
                if (args.Severity == XmlSeverityType.Error)
                    errors.Add(args.Message);
            };

            foreach (string entryPoint in entryPoints)
            {
                Assert.That(filesByName.ContainsKey(entryPoint), $"Entry point file not found: {entryPoint}");
                schemaSet.Add(null, filesByName[entryPoint]);
            }

            schemaSet.Compile();

            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
        }
    }

    private static DirectoryInfo GetSharePoint2010Folder()
    {
        return new DirectoryInfo(Environment.CurrentDirectory)
            .AscendToFolder("XObjectsTests")
            .AscendByLevel(1)
            .DescendToFolder("SharePoint2010");
    }
}