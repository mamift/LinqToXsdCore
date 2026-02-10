using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq;
using NUnit.Framework;
using ObjectsComparer;
using Xml.Schema.Linq.Extensions;
using Xml.Schema.Linq.Tests.Extensions;

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
        [TestCase("1707_ISYBAU_XML_Schema"), TestCase("AbstractTypeTest"), TestCase("AkomaNtoso"), TestCase("AkomaNtoso30-CSD13-D2f"), TestCase("AspNetSiteMaps"), TestCase("Atom"), TestCase("ContentModelTest"), TestCase("EnumsTest"), TestCase("EnzymeML"), TestCase("MetaLEX"), TestCase("Microsoft Search"), TestCase("Multi-namespaces"), TestCase("mzIdentML"), TestCase("mzML"), TestCase("mzQuantML"), TestCase("NameMangled"), TestCase("NHS CDS"), TestCase("OcmContracts"), TestCase("OfficeOpenXML-XMLSchema-Strict"), TestCase("OfficeOpenXML-XMLSchema-Transitional"), TestCase("OFMX"), TestCase("Opml"), TestCase("Pubmed"), TestCase("Rss"), TestCase("SharePoint2010"), TestCase("ThermoML"), TestCase("Toy schemas"), TestCase("TraML"), TestCase("Windows"), TestCase("W3C.XML"), TestCase("XMLSpec"), TestCase("XQueryX")]
        // these are failing tests due reasons besides typeof(void)
        /* [TestCase("CityGML"), TestCase("GelML"), TestCase("GS1"), TestCase("HL-7"), TestCase("HR-XML"), TestCase("LegalRuleML"), TestCase("Office 2003 Reference schemas"), TestCase("OPC"),
        TestCase("SWRL"), TestCase("UK CabinetOffice"), TestCase("OpenPackagingConventions-XMLSchema"), TestCase("XSD"), TestCase("OGC-misc"), TestCase("SBML")] */
        // also these fail:
        // [TestCase("NIEM"), TestCase("SBVR-XML"), TestCase("LandXML"), TestCase("FHIR"), TestCase("CellML"), TestCase("DTSX"), TestCase("Chem eStandards"), TestCase("AIXM")]
        // [TestCase("MSBuild"), TestCase("3dps-1_0_0")]
        // this is an abominable schema and causes out of memory exceptions: NEVER USE! it's cursed!
        // [TestCase("Microsoft Project 2007")
        // causes stackoverflow error:
        // [TestCase("BITS-2.0-XSD")]
        public void NoVoidTypeOfExpressionsInGeneratedCode(string assemblyName)
        {
            var xsdsToProcess = GetFileSystemForAssemblyName(assemblyName).AllFiles.Where(f => f.EndsWith(".xsd"));

            CheckTypeOfVoidExpressionsInGeneratedCode(xsdsToProcess);
        }

        private void CheckTypeOfVoidExpressionsInGeneratedCode(IEnumerable<string> xsdsToProcess, int randomSubset = -1)
        {
            var allProcessableXsds =
                AllTestFiles.ResolvePossibleFileAndFolderPathsToProcessableSchemas(xsdsToProcess);

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
                    var message = $"{file} failed to generated code.";
                    TestContext.Out.WriteLine(message);

                    throw new LinqToXsdException(message, pair.exception);
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
            var ns = tree.GetNamespaceRoot();

            var fullString = ns.ToFullString();
            TestContext.CurrentContext.DumpDebugOutputToFile(debugStrings: new [] { fullString });

            var allProperties = ns.GetAllPropertyDescendantAttributes();
            var allFields = ns.GetAllFieldDescendantAttributes();

            var allPropAttributeNames = allProperties.Select(a => ((IdentifierNameSyntax) a.Name).Identifier.Text).ToList();
            var allFieldAttributeNames = allFields.Select(a => ((IdentifierNameSyntax) a.Name).Identifier.Text).ToList();

            Assert.IsNotEmpty(allPropAttributeNames);
            Assert.IsNotEmpty(allFieldAttributeNames);

            const int expectedAllPropAttributeNamesCount = 52;
            var actualAllPropAttributeNamesCount = allPropAttributeNames.Count;
            Assert.IsTrue(actualAllPropAttributeNamesCount == expectedAllPropAttributeNamesCount);

            const int expectedAllFieldAttributeNamesCount = 235;
            var actualAllFieldAttributeNamesCount = allFieldAttributeNames.Count;
            Assert.IsTrue(actualAllFieldAttributeNamesCount == expectedAllFieldAttributeNamesCount);

            const string debuggerBrowsableName = "DebuggerBrowsable";
            const string editorBrowsableName = "EditorBrowsable";
            List<string> propAndFieldAttrNames = allPropAttributeNames.Concat(allFieldAttributeNames).Distinct().ToList();
            var allNamesAreTheSame = propAndFieldAttrNames.All(s => s == debuggerBrowsableName || s == editorBrowsableName);

            Assert.IsTrue(allNamesAreTheSame);
        }

        [Test]
        public void CompareGeneratedAttributesCurrentAndSavedCSharpSourceCode()
        {
            var atomXsdFileInfo = new MockFileInfo(AllTestFiles, AtomXsdFilePath);

            CSharpSyntaxTree tree1 = Utilities.GenerateSyntaxTree(atomXsdFileInfo, AllTestFiles);
            NamespaceDeclarationSyntax ns1 = tree1.GetNamespaceRoot().CleanForComparison();

            CSharpSyntaxTree tree2 = new FileInfo("..\\..\\..\\..\\GeneratedSchemaLibraries\\Atom\\atom.xsd.cs").ToSyntaxTree();
            NamespaceDeclarationSyntax ns2 = tree2.GetNamespaceRoot().CleanForComparison();
            
            Assert.IsEmpty(ns1.CompareProperties(ns2));
            Assert.IsEmpty(ns1.CompareFields(ns2));

            List<AttributeSyntax> allFields1 = ns1.GetAllFieldDescendantAttributes();
            List<AttributeSyntax> allFields2 = ns2.GetAllFieldDescendantAttributes();

            Assert.AreEqual(allFields1.Count, allFields2.Count);

            var fieldWithAttrString1 = allFields1.Select(f => f.Parent.Parent.ToString().Trim()).ToList();
            var fieldWithAttrString2 = allFields2.Select(f => f.Parent.Parent.ToString().Trim()).ToList();

            var stringComparison = fieldWithAttrString1.CompareObjects(fieldWithAttrString2);

            Assert.IsEmpty(stringComparison);
        }

        [Test]
        public void CompareFieldSummariesForEntireNamespaces()
        {
            var atomXsdFileInfo = new MockFileInfo(AllTestFiles, AtomXsdFilePath);

            var tree1 = Utilities.GenerateSyntaxTree(atomXsdFileInfo, AllTestFiles);
            var ns1 = tree1.GetNamespaceRoot();
            ns1 = ns1.CleanForComparison();

            var existingCodeFile = "atom.xsd.cs";
            var existingAtomCode = Environment.CurrentDirectory
                .AscendToFolder("GeneratedSchemaLibraries").DescendToFolder("Atom").FindFileRecursively(existingCodeFile);

            var tree2 = existingAtomCode.ToSyntaxTree();
            var ns2 = tree2.GetNamespaceRoot();
            ns2 = ns2.CleanForComparison();
            
            {
                var directoryName = Path.GetDirectoryName(existingAtomCode.FullName);
                var fileName = Path.GetFileNameWithoutExtension(existingAtomCode.FullName);
                // save the new one for comparison; file ext is csv to prevent hot reload from triggering during test debug
                var comparisonFilePath = Path.Combine(directoryName!, fileName + ".2.csx");
                ns1.WriteToFile(comparisonFilePath);
                ns2.WriteToFile(existingAtomCode.FullName);
            }
            
            Assert.IsEmpty(ns1.CompareProperties(ns2));
            Assert.IsEmpty(ns1.CompareFields(ns2));

            var allFieldAttrs1 = ns1.GetAllFieldDescendantAttributes();
            var allFieldAttrs2 = ns2.GetAllFieldDescendantAttributes();

            var attrsAndFields1 = SummariseFields(allFieldAttrs1);
            var attrsAndFields2 = SummariseFields(allFieldAttrs2);

            var groupedFields1 = (from af in attrsAndFields1
                group af by af.FieldName into afGroup
                orderby afGroup.Key
                select afGroup).ToList();
            
            var groupedFields2 = (from af in attrsAndFields2
                group af by af.FieldName into afGroup
                orderby afGroup.Key
                select afGroup).ToList();

            var fieldCounts1 = groupedFields1.Select(g => new { g.Key, Count = g.Count() }).ToList();
            var fieldCounts2 = groupedFields2.Select(g => new { g.Key, Count = g.Count() }).ToList();

            var regrouped1 = from af in groupedFields1.Where(g => g.Key == "DebuggerBrowsable")
                    .SelectMany(g => g)
                group af by af.FieldName into faf
                select faf;

            var regrouped2 = from af in groupedFields1.Where(g => g.Key == "DebuggerBrowsable")
                    .SelectMany(g => g)
                group af by af.FieldName into faf
                select faf;

            var compareCounts = fieldCounts1.CompareObjects(fieldCounts2);
            // test failure, but we need intelligent error messages at this point
            if (compareCounts.Any()) {
                var sb = new StringBuilder();
                foreach (var compare in compareCounts) {
                    var memberIndex = compare.MemberPath.StripToDigits().ParseInt();
                    var theMember1 = fieldCounts1[memberIndex!.Value];
                    var theMember2 = fieldCounts2[memberIndex!.Value!];
                    Assert.AreEqual(theMember2.Key, theMember1.Key);
                    sb.AppendLine($"FieldName: {theMember1.Key}, Count1: {theMember1.Count}, Count2: {theMember2.Count}");
                }
                
                Assert.Fail("Mismatch between field counts! " + Environment.NewLine + sb + Environment.NewLine);
            }
            
            Assert.IsEmpty(compareCounts);

            var compareFields = groupedFields1.CompareObjects(groupedFields2);

            Assert.IsEmpty(compareFields);
            
            return;

            static List<(string Attr, string FieldName)> SummariseFields(List<AttributeSyntax> attrs)
            {
                return attrs.Select(a =>
                    (
                        Attr: a.Name.ToFullString(), 
                        FieldName: ((a.Parent as AttributeListSyntax)?.Parent as FieldDeclarationSyntax)?.Declaration.Variables.Select(v => v.Identifier.ValueText).Single()
                    )
                ).OrderByDescending(e => e.FieldName)
                .ToList();
            }
        }

        [Test]
        public void ValidatorSubTypesExist()
        {
            var atomXsdFileInfo = new MockFileInfo(AllTestFiles, AtomXsdFilePath);

            var newTree = Utilities.GenerateSyntaxTree(atomXsdFileInfo, AllTestFiles);
            var newNs = newTree.GetNamespaceRoot();
            newNs = newNs.CleanForComparison();

            var existingCodeFile = "atom.xsd.cs";
            var existingAtomCsFilePath = new DirectoryInfo(Environment.CurrentDirectory)
                .AscendToFolder("GeneratedSchemaLibraries").DescendToFolder("Atom").FindFileRecursively(existingCodeFile);

            Assert.True(existingAtomCsFilePath.Exists, $"Can't find existing code generated for file: '{existingCodeFile}'");

            var treeFromExisting = existingAtomCsFilePath.ToSyntaxTree();
            var existingNs = treeFromExisting.GetNamespaceRoot();
            existingNs = existingNs.CleanForComparison();

            var newTypes = newNs.Members.OfType<TypeDeclarationSyntax>().ToList();
            var newSubTypes = (from t1 in newTypes
                from tt1 in t1.Members.OfType<TypeDeclarationSyntax>()
                select tt1).ToList();

            var existingTypes = existingNs.Members.OfType<TypeDeclarationSyntax>().ToList();
            var existingSubtypes = (from t2 in existingTypes
                from tt2 in t2.Members.OfType<TypeDeclarationSyntax>()
                select tt2).ToList();

            Assert.AreEqual(newTypes.Count, existingTypes.Count,
                $"Type count mismatch between new code and existing code! New count: {newTypes.Count}, Existing count: {existingTypes.Count}");

            Assert.IsNotEmpty(newSubTypes);
            Assert.IsNotNull(newSubTypes.SingleOrDefault());

            Assert.IsNotEmpty(existingSubtypes);
            Assert.IsNotNull(existingSubtypes.SingleOrDefault());
        }
    }
}