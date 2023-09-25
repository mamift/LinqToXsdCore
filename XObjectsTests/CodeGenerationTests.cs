using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    using SF = SyntaxFactory;

    public class CodeGenerationTests: BaseTester
    {
        private const string AtomXsdFilePath = @"Atom\atom.xsd";

        [Test]
        public void NamespaceCodeGenerationConventionTest()
        {
            const string simpleDocXsdFilepath = @"Toy schemas\Simple doc.xsd";
            var mockFileInfo = new MockFileInfo(AllTestFiles, simpleDocXsdFilepath);
            
            var simpleDocXsd = XmlReader.Create(mockFileInfo.OpenRead()).ToXmlSchema();

            var sourceText = Utilities.GenerateSourceText(simpleDocXsdFilepath, AllTestFiles);

            var tree = CSharpSyntaxTree.ParseText(sourceText);
            var namespaceNode = tree.GetNamespaceRoot();

            Assert.IsNotNull(namespaceNode);

            var xmlQualifiedNames = simpleDocXsd.Namespaces.ToArray();
            var nsName = Regex.Replace(xmlQualifiedNames.Last().Namespace, @"\W", "_");
            var cSharpNsName = Regex.Replace(namespaceNode.Name.ToString(), @"\W", "_");

            Assert.IsTrue(cSharpNsName == nsName);
        }

        /// <summary>
        /// Tests the the BuildWrapperDictionary() method of the LinqToXsdTypeManager class that's
        /// generated does not contain <c>typeof(void)</c> expressions, which are meaningless and break
        /// typed XElement conversion.
        /// </summary>
        [Test]
        public void AtomNoVoidTypeOfExpressionsInLinqToXsdTypeManagerBuildWrapperDictionaryMethodTest()
        {
            const string atomDir = @"Atom";
            var atomRssXsdFile = $"{atomDir}\\atom.xsd";
            var atomRssXsdFileInfo = new MockFileInfo(AllTestFiles, atomRssXsdFile);
            var tree = Utilities.GenerateSyntaxTree(atomRssXsdFileInfo, AllTestFiles);

            var linqToXsdTypeManagerClassDeclarationSyntax = tree.GetNamespaceRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                                                                 .FirstOrDefault(cds => cds.Identifier.ValueText == nameof(LinqToXsdTypeManager));

            Assert.IsNotNull(linqToXsdTypeManagerClassDeclarationSyntax);

            var buildWrapperDictionaryMethod = linqToXsdTypeManagerClassDeclarationSyntax
                                               .DescendantNodes().OfType<MethodDeclarationSyntax>()
                                               .FirstOrDefault(mds =>
                                                   mds.Identifier.ValueText == "BuildWrapperDictionary");

            Assert.IsNotNull(buildWrapperDictionaryMethod);

            var statements = buildWrapperDictionaryMethod.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

            Assert.IsTrue(statements.Length == 2);

            var typeOfExpressions = statements.SelectMany(ies => ies.ArgumentList.DescendantNodes()).OfType<TypeOfExpressionSyntax>().ToArray();

            Assert.IsNotEmpty(typeOfExpressions);
            Assert.IsTrue(typeOfExpressions.Length == 4);

            var typeOfVoid = SF.ParseExpression("typeof(void)");
            var nonVoidTypeOfExpressions = typeOfExpressions.Where(toe => !toe.IsEquivalentTo(typeOfVoid)).ToArray();
            var voidTypeOfExpressions = typeOfExpressions.Except(nonVoidTypeOfExpressions).ToArray();

            Assert.IsNotEmpty(nonVoidTypeOfExpressions);
            Assert.IsTrue(nonVoidTypeOfExpressions.Length == 4);

            // if this is not empty, then you have a problem...
            Assert.IsEmpty(voidTypeOfExpressions);
        }

        /// <summary>
        /// There shouldn't be <c>typeof(void)</c> expressions in any generated code.
        /// <para>See commit bc75ea0 which introduced this incorrect behaviour.</para>
        /// </summary>
        [Test]
        [TestCase("1707_ISYBAU_XML_Schema"), TestCase("3dps-1_0_0"), TestCase("AbstractTypeTest"), TestCase("AkomaNtoso"), TestCase("AkomaNtoso30-CSD13-D2f"), TestCase("AspNetSiteMaps"), TestCase("Atom"), TestCase("CityGML"), TestCase("ContentModelTest"), TestCase("EnumsTest"), TestCase("EnzymeML"), TestCase("GelML"), TestCase("GS1"), TestCase("HL-7"), TestCase("HR-XML"), TestCase("LegalRuleML"), TestCase("MetaLEX"), TestCase("Microsoft Search"), TestCase("Multi-namespaces"), TestCase("mzIdentML"), TestCase("mzML"), TestCase("mzQuantML"), TestCase("NameMangled"), TestCase("NHS CDS"), TestCase("OcmContracts"), TestCase("Office 2003 Reference schemas"), TestCase("OfficeOpenXML-XMLSchema-Strict"), TestCase("OfficeOpenXML-XMLSchema-Transitional"), TestCase("OFMX"), TestCase("OGC-misc"), TestCase("OPC"), TestCase("OpenPackagingConventions-XMLSchema"), TestCase("Opml"), TestCase("Pubmed"), TestCase("Rss"), TestCase("SBML"), TestCase("SharePoint2010"), TestCase("SWRL"), TestCase("ThermoML"), TestCase("Toy schemas"), TestCase("TraML"), TestCase("UK CabinetOffice"), TestCase("Windows"), TestCase("XMLSpec"), TestCase("XQueryX"), TestCase("XSD")]
        //[TestCase("NIEM"), TestCase("SBVR-XML"), TestCase("LandXML"), TestCase("FHIR"), TestCase("CellML"), TestCase("DTSX"), TestCase("Chem eStandards"), TestCase("AIXM")]
        // [TestCase("Microsoft Project 2007"), TestCase("MSBuild")]
        public void NoVoidTypeOfExpressionsInGeneratedCode(string assemblyName)
        {
            var xsdsToProcess = GetFileSystemForAssemblyName(assemblyName).AllFiles.Where(f => f.EndsWith(".xsd"));

            CheckTypeOfVoidExpressionsInGeneratedCode(xsdsToProcess);
        }

        private void CheckTypeOfVoidExpressionsInGeneratedCode(IEnumerable<string> xsdsToProcess, int randomSubset = -1)
        {
            var allProcessableXsds =
                Utilities.ResolvePossibleFileAndFolderPathsToProcessableSchemas(AllTestFiles, xsdsToProcess);

            var failingXsds = new List<(IFileInfo file, Exception exception)>(allProcessableXsds.Capacity);

            var toProcess = randomSubset > 0 ? allProcessableXsds.RandomSubset(100) : allProcessableXsds;

            foreach (var xsd in toProcess) {
                var generateResult = Utilities.GenerateSyntaxTreeOrError(xsd, AllTestFiles);

                if (generateResult.IsT1) {
                    failingXsds.Add((xsd, generateResult.AsT1));
                    continue;
                }

                var generatedCodeTree = generateResult.AsT0;
                var root = generatedCodeTree.GetRoot();

                var allDescendents = root.DescendantNodes().SelectMany(d => d.DescendantNodes()).ToList();

                if (!allDescendents.Any()) continue;

                var allStatements = allDescendents.OfType<StatementSyntax>();
                var allExpressions = allStatements.SelectMany(s => s.DescendantNodes()).OfType<ExpressionSyntax>();
                var typeOfExpressions = allExpressions.OfType<TypeOfExpressionSyntax>().Distinct().ToArray();

                Assert.IsNotEmpty(typeOfExpressions);

                var typeOfVoid = SF.ParseExpression("typeof(void)");
                var nonVoidTypeOfExpressions = typeOfExpressions.Where(toe => !toe.IsEquivalentTo(typeOfVoid)).ToArray();
                var voidTypeOfExpressions = typeOfExpressions.Where(toe => toe.IsEquivalentTo(typeOfVoid)).ToArray();

                Assert.IsNotEmpty(nonVoidTypeOfExpressions);

                if (voidTypeOfExpressions.Any()) {
                    Assert.Warn($"Some typeof(void) expressions found! Warning generated for XSD: " + xsd.FullName);
                }
            }

            if (failingXsds.Any()) {
                foreach (var pair in failingXsds) {
                    var file = pair.file.FullName;
                    TestContext.Out.WriteLine($"{file} failed to generated code.");

                    throw pair.exception;
                }
            }
        }

        /// <summary>
        /// There shouldn't be <c>typeof(void)</c> expressions in any generated code.
        /// <para>See commit bc75ea0 which introduced this incorrect behaviour.</para>
        /// </summary>
        // [Test]
        public void NoVoidTypeOfExpressionsInGeneratedCodeEver()
        {
            var dir = new MockDirectoryInfo(AllTestFiles, ".");
            var allXsds = dir.GetFiles("*.xsd", SearchOption.AllDirectories)
                // Microsoft.Build schemas will have typeof(void) expressions due to the existence of bugs that predate this .net core port
                .Where(f => !f.FullName.Contains("Microsoft.Build.") && !f.FullName.Contains("Microsoft Project 2007"))
                .Select(f => f.FullName).ToArray();

            // cant run all
            CheckTypeOfVoidExpressionsInGeneratedCode(allXsds, 100);
        }

        /// <summary>
        /// Tests that in all the properties generated, there are no <c>void.TypeDefinition</c> expressions.
        /// </summary>
        [Test]
        public void NoVoidTypeDefReferencesInAnyStatementsInClrPropertiesTest()
        {
            const string xsdSchema = @"XSD\W3C XMLSchema v1.xsd";
            var xsdCode = Utilities.GenerateSyntaxTree(new MockFileInfo(AllTestFiles, xsdSchema), AllTestFiles);

            var allClasses = xsdCode.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            var allProperties = allClasses.SelectMany(cds => cds.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                .Distinct();

            var readWriteable = (from prop in allProperties
                where prop.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration) ||
                                                           a.IsKind(SyntaxKind.SetAccessorDeclaration))
                      && prop.AccessorList.Accessors.Count >= 2
                select prop).Distinct();

            var virtualProps = readWriteable.Where(prop => prop.Modifiers.Any(SyntaxKind.VirtualKeyword)).ToList();
            var accessors = virtualProps.SelectMany(prop => prop.AccessorList.Accessors).ToList();
            var getters = accessors.Where(getter => getter.IsKind(SyntaxKind.GetAccessorDeclaration));
            var setters = accessors.Where(setter => setter.IsKind(SyntaxKind.SetAccessorDeclaration));

            var getterStatements = getters.SelectMany(getter => getter.DescendantNodes().OfType<StatementSyntax>());
            var setterStatements = setters.SelectMany(getter => getter.DescendantNodes().OfType<StatementSyntax>());

            var getterReturnStatements = getterStatements.OfType<ReturnStatementSyntax>();
            var getterTypeDefinitionReferences = getterReturnStatements.SelectMany(r => r.DescendantNodes()
                .OfType<PredefinedTypeSyntax>());
            var getterVoidTypeDefinitionReferences =
                getterTypeDefinitionReferences.Where(tdefr => tdefr.Keyword.Text == "void");

            var setterExpressionSyntaxStatements = setterStatements.OfType<ExpressionStatementSyntax>();
            var setterTypeDefinitionReferences = setterExpressionSyntaxStatements.SelectMany(s => s.DescendantNodes())
                .OfType<PredefinedTypeSyntax>();
            var setterVoidTypeDefinitionReferences =
                setterTypeDefinitionReferences.Where(tdefr => tdefr.Keyword.Text == "void");

            Assert.IsEmpty(setterVoidTypeDefinitionReferences);
            Assert.IsEmpty(getterVoidTypeDefinitionReferences);
        }

        /// <summary>
        /// Tests enums declared at the namespace level.
        /// </summary>
        [Test]
        public void EnumAtNamespaceLevelGenerationTest()
        {
            const string wssXsdFilePath = @"SharePoint2010\wss.xsd";
            var wssXsdFileInfo = new MockFileInfo(AllTestFiles, wssXsdFilePath);
            var tree = Utilities.GenerateSyntaxTree(wssXsdFileInfo, AllTestFiles);
            var root = tree.GetNamespaceRoot();

            var namespaceScopedEnums = root.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();

            Assert.IsNotEmpty(namespaceScopedEnums);
            const int expected = 46;
            var actual = namespaceScopedEnums.Count;
            var isExpected = actual == expected;
            if (!isExpected) Assert.Warn(Utilities.WarningMessage(expected, actual));
        }

        /// <summary>
        /// Tests that all invocations of <see cref="System.Xml.Linq.XName.Get(string)"/> are fully qualified.
        /// </summary>
        [Test]
        public void TestXNameGetInvocationsAreFullyQualified()
        {
            var atomXsdFileInfo = new MockFileInfo(AllTestFiles, AtomXsdFilePath);
            CSharpSyntaxTree tree = Utilities.GenerateSyntaxTree(atomXsdFileInfo, AllTestFiles);

            TestContext.CurrentContext.DumpDebugOutputToFile(debugStrings: new []{ tree.ToString() });

            NamespaceDeclarationSyntax root = tree.GetNamespaceRoot();
            var sourceCode = root.ToFullString();

            var xNameInvocationsUpToMethodName =
                Regex.Matches(sourceCode, "System\\.Xml\\.Linq\\.XName\\.Get\\(\"", RegexOptions.Multiline);

            Assert.IsNotEmpty(xNameInvocationsUpToMethodName);
            Assert.IsTrue(xNameInvocationsUpToMethodName.Count == 630);
        }

        /// <summary>
        /// Tests that all fields and properties that have attributes are decorated with the same attribute, <see cref="DebuggerBrowsableAttribute"/>.
        /// </summary>
        [Test]
        public void DebuggerBrowsableAttributesGeneratedTest()
        {
            var atomXsdFileInfo = new MockFileInfo(AllTestFiles, AtomXsdFilePath);

            var tree = Utilities.GenerateSyntaxTree(atomXsdFileInfo, AllTestFiles);
            var root = tree.GetNamespaceRoot();

            TestContext.CurrentContext.DumpDebugOutputToFile(debugStrings: new [] { root.ToFullString() });

            var allProperties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
            var allFields = root.DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();

            var allFieldsWithAttrs = (from field in allFields
                                      where field.AttributeLists.Select(al => al.Attributes).Any()
                                      select field).ToList();

            var allPropertiesWithAttrs = (from p in allProperties
                                          where p.AttributeLists.Select(al => al.Attributes).Any()
                                          select p).ToList();

            var allPropsAttributes = allPropertiesWithAttrs
                .SelectMany(p => p.AttributeLists.SelectMany(al => al.Attributes)).ToList();

            var allFieldAttributes = allFieldsWithAttrs
                .SelectMany(f => f.AttributeLists.SelectMany(al => al.Attributes)).ToList();

            var allPropAttributeNames = allPropsAttributes.Select(a => ((IdentifierNameSyntax) a.Name).Identifier.Text).ToList();
            var allFieldAttributeNames = allFieldAttributes.Select(a => ((IdentifierNameSyntax) a.Name).Identifier.Text).ToList();

            Assert.IsNotEmpty(allPropAttributeNames);
            Assert.IsNotEmpty(allFieldAttributeNames);

            const int expectedAllPropAttributeNamesCount = 52;
            var actualAllPropAttributeNamesCount = allPropAttributeNames.Count;
            Assert.IsTrue(actualAllPropAttributeNamesCount == expectedAllPropAttributeNamesCount);

            const int expectedAllFieldAttributeNamesCount = 234;
            var actualAllFieldAttributeNamesCount = allFieldAttributeNames.Count;
            Assert.IsTrue(actualAllFieldAttributeNamesCount == expectedAllFieldAttributeNamesCount);

            const string debuggerBrowsableName = "DebuggerBrowsable";
            const string editorBrowsableName = "EditorBrowsable";
            List<string> propAndFieldAttrNames = allPropAttributeNames.Concat(allFieldAttributeNames).Distinct().ToList();
            var allNamesAreTheSame = propAndFieldAttrNames.All(s => s == debuggerBrowsableName || s == editorBrowsableName);

            Assert.IsTrue(allNamesAreTheSame);
        }
    }
}