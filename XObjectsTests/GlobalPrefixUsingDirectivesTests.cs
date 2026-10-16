using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    /// <summary>
    /// Tests that the using directives generated for a schema set with more than one namespace are qualified with
    /// global:: either always, when <see cref="LinqToXsdSettings.AlwaysPrefixGlobalInUsingDirectives"/> is set, or
    /// when an import shares a component with the namespace it is declared in.
    /// </summary>
    public class GlobalPrefixUsingDirectivesTests
    {
        private const string RootXsdPath = @"C:\schemas\root.xsd";
        private const string OtherXsdPath = @"C:\schemas\other.xsd";

        private const string RootXsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
           xmlns=""urn:test:namespace1""
           xmlns:ns2=""urn:test:namespace2""
           targetNamespace=""urn:test:namespace1""
           elementFormDefault=""qualified"">
    <xs:import namespace=""urn:test:namespace2"" schemaLocation=""other.xsd"" />
    <xs:element name=""Combined"">
        <xs:complexType>
            <xs:sequence>
                <xs:element ref=""ns2:Other"" minOccurs=""0"" />
            </xs:sequence>
        </xs:complexType>
    </xs:element>
</xs:schema>";

        private const string OtherXsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
           xmlns=""urn:test:namespace2""
           targetNamespace=""urn:test:namespace2""
           elementFormDefault=""qualified"">
    <xs:element name=""Other"" type=""xs:string"" />
</xs:schema>";

        private MockFileSystem CreateTwoNamespaceSchemaFileSystem()
        {
            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { RootXsdPath, new MockFileData(RootXsd) },
                { OtherXsdPath, new MockFileData(OtherXsd) },
            });
        }

        private List<UsingDirectiveSyntax> GenerateUsingDirectives(LinqToXsdSettings settings)
        {
            var fs = CreateTwoNamespaceSchemaFileSystem();
            var schemaSet = Utilities.GetXmlSchemaSetAndPreload(new MockFileInfo(fs, RootXsdPath), fs);
            var sourceText = Utilities.GenerateSourceText(schemaSet, RootXsdPath, fs, settings);
            var tree = CSharpSyntaxTree.ParseText(sourceText, CSharpParseOptions.Default);

            return tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
        }

        private static bool IsCrossNamespaceImport(UsingDirectiveSyntax usingDirective)
        {
            var name = usingDirective.Name.ToString();
            return name.EndsWith("urn.test.namespace1") || name.EndsWith("urn.test.namespace2");
        }

        [Test]
        public void WhenAlwaysPrefixGlobalThenAllUsingDirectivesAreGlobalQualified()
        {
            var settings = new LinqToXsdSettings { AlwaysPrefixGlobalInUsingDirectives = true };

            var usingDirectives = GenerateUsingDirectives(settings);

            Assert.IsNotEmpty(usingDirectives);
            Assert.IsTrue(usingDirectives.All(u => u.Name.ToString().StartsWith("global::")),
                "Expected every using directive to be qualified with global:: when AlwaysPrefixGlobal is set.");
        }

        [Test]
        public void WhenImportSharesComponentWithImportingNamespaceThenImportIsGlobalQualified()
        {
            var usingDirectives = GenerateUsingDirectives(new LinqToXsdSettings());

            var crossNamespaceImports = usingDirectives.Where(IsCrossNamespaceImport).ToList();

            Assert.IsNotEmpty(crossNamespaceImports);
            Assert.IsTrue(crossNamespaceImports.All(u => u.Name.ToString().StartsWith("global::")),
                "Expected cross-namespace imports to be qualified with global:: when they share a component with the namespace they are declared in.");
            Assert.IsTrue(usingDirectives.Any(u => !u.Name.ToString().StartsWith("global::")),
                "Expected the default imports to remain unqualified when they share no component with the enclosing namespace.");
        }
    }
}
