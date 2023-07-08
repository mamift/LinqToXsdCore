using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Resolvers;
using System.Xml.Schema;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    public static class Utilities
    {
        public static MockFileSystem GetAggregateMockFileSystem(IEnumerable<Assembly> assemblies)
        {
            var mockFs = new MockFileSystem();
            foreach (var assembly in assemblies) {
                var fileData = GetAssemblyTextFilesDictionary(assembly);
                foreach (var kvp in fileData) {
                    mockFs.AddFile(kvp.Key, kvp.Value);
                }
            }

            return mockFs;
        }

        public static MockFileSystem GetAssemblyFileSystem(Assembly assembly)
        {
            var mockFs = new MockFileSystem(GetAssemblyTextFilesDictionary(assembly));
            return mockFs;
        }

        public static Dictionary<string, MockFileData> GetAssemblyTextFilesDictionary(Assembly assembly)
        {
            var names = assembly.GetManifestResourceNames();

            var rootName = assembly.GetName().Name + ".";

            var streams = names.Select(n => (
                name: n.Replace(rootName, rootName.Replace(".", "\\")),
                stream: assembly.GetManifestResourceStream(n))
            ).ToList();

            var contents = streams.Select(tu => (tu.name, data: new MockFileData(tu.stream.ReadAsString(dispose: true))))
                .ToDictionary(k => k.name, v => v.data);
            
            return contents;
        }

        public static string WarningMessage(object expected, object actual, [CallerMemberName] string caller = "")
        {
            return caller + "() failed; expected " + expected + ", got " + actual;
        }

        /// <summary>
        /// Used specifically for unit testing, invokes the
        /// <see cref="XObjectsCoreGenerator.Generate(IEnumerable{string},LinqToXsdSettings)"/>
        /// method for generating C# code.
        /// </summary>
        /// <param name="xsdFileName"></param>
        /// <returns></returns>
        public static SourceText GenerateSourceText(string xsdFileName)
        {
            var possibleSettingsFile = $"{xsdFileName}.config";
            var codeWriters = File.Exists(possibleSettingsFile)
                ? XObjectsCoreGenerator.Generate(xsdFileName, possibleSettingsFile)
                : XObjectsCoreGenerator.Generate(xsdFileName, default(string));

            // This method assumes SplitCodeFile is not used, so there's only a single writer per file.
            var writer = codeWriters.Single().writer;

            return SourceText.From(writer.ToString());
        }

        /// <summary>
        /// Used specifically for unit testing, invokes the
        /// <see cref="XObjectsCoreGenerator.Generate(XmlSchemaSet,LinqToXsdSettings)"/>
        /// method for generating C# code.
        /// </summary>
        /// <param name="xmlSchemaSet"></param>
        /// <param name="xsdFileName">Required for loading any configuration files. Accepts relative and absolute.</param>
        /// <returns></returns>
        public static SourceText GenerateSourceText(XmlSchemaSet xmlSchemaSet, string xsdFileName)
        {
            var possibleSettingsFile = $"{xsdFileName}.config";
            Configuration config = File.Exists(possibleSettingsFile)
                ? Configuration.Load(possibleSettingsFile)
                : Configuration.GetBlankConfigurationInstance();
            var settings = config.ToLinqToXsdSettings();

            var code = XObjectsCoreGenerator.Generate(xmlSchemaSet, settings);

            var writerText = code.Select(t => t.writer.ToString());

            var delimitedByNewLines = writerText.ToDelimitedString(Environment.NewLine);

            return SourceText.From(delimitedByNewLines);
        }

        /// <summary>
        /// Generates C# code from a given <paramref name="xsdFilePath"/> and then returns the <see cref="CSharpSyntaxTree"/> of
        /// </summary>
        public static CSharpSyntaxTree GenerateSyntaxTree(string xsdFilePath)
        {
            return GenerateSyntaxTree(new FileInfo(xsdFilePath));
        }

        /// <summary>
        /// Generates C# code from a given <paramref name="xsdFile"/> and then returns the <see cref="CSharpSyntaxTree"/> of
        /// the generated code.
        /// </summary>
        public static CSharpSyntaxTree GenerateSyntaxTree(IFileInfo xsdFile, IMockFileDataAccessor fs)
        {
            if (xsdFile == null) throw new ArgumentNullException(nameof(xsdFile));

            var folderWithAdditionalXsdFiles = xsdFile.DirectoryName;
            var directoryInfo = new MockDirectoryInfo(fs, folderWithAdditionalXsdFiles);
            var additionalXsds = directoryInfo.GetFiles("*.xsd").Where(f => f.FullName != xsdFile.FullName).ToArray();

            var xmlPreloadedResolver = new MockXmlUrlResolver(fs);

            foreach (var xsd in additionalXsds) {
                var fileStream = xsd.OpenRead();
                var uri = new Uri($"{xsd.Name}", UriKind.Relative);
                xmlPreloadedResolver.Add(uri, fileStream);
            }

            var xmlReaderSettings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true
            };
            var schemaSet = XmlReader.Create(xsdFile.OpenRead(), xmlReaderSettings)
                .ToXmlSchemaSet(xmlPreloadedResolver);

            var sourceText = GenerateSourceText(schemaSet, xsdFile.FullName);
            var stringBuilder = new StringBuilder();
            using var writer = new StringWriter(stringBuilder);
            sourceText.Write(writer);

            var tree = CSharpSyntaxTree.ParseText(sourceText, CSharpParseOptions.Default);

            return tree as CSharpSyntaxTree;
        }

        /// <summary>
        /// Generates C# code from a given <paramref name="xsdFile"/> and then returns the <see cref="CSharpSyntaxTree"/> of
        /// the generated code.
        /// </summary>
        public static CSharpSyntaxTree GenerateSyntaxTree(FileInfo xsdFile)
        {
            if (xsdFile == null) throw new ArgumentNullException(nameof(xsdFile));

            var folderWithAdditionalXsdFiles = xsdFile.DirectoryName;
            var directoryInfo = new DirectoryInfo(folderWithAdditionalXsdFiles);
            var additionalXsds = directoryInfo.GetFiles("*.xsd");

            var xmlPreloadedResolver = new XmlPreloadedResolver();

            foreach (var xsd in additionalXsds) {
                xmlPreloadedResolver.Add(new Uri($"file://{xsd.FullName}"), File.OpenRead(xsd.FullName));
            }

            var xmlReaderSettings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true
            };
            var atomXsdSchemaSet = XmlReader.Create(xsdFile.FullName, xmlReaderSettings)
                                            .ToXmlSchemaSet(xmlPreloadedResolver);

            var sourceText = GenerateSourceText(atomXsdSchemaSet, xsdFile.FullName);
            using var writer = new StreamWriter(xsdFile.FullName + ".cs");
            sourceText.Write(writer);

            var tree = CSharpSyntaxTree.ParseText(sourceText, CSharpParseOptions.Default);

            return tree as CSharpSyntaxTree;
        }

        /// <summary>
        /// Compile a syntax tree and returns the number of syntax and compilation diagnostics
        /// </summary>
        /// <param name="tree"></param>
        /// <returns>The number of syntax and compilation diagnostics. Zero means success.</returns>
        /// <remarks>
        /// Can be used in unit tests like this:<code><![CDATA[
        ///    var diags = Utilities.GetSyntaxAndCompilationDiagnostics(syntaxTree);
        ///    Assert.AreEqual(0, diags.Length);
        /// ]]></code></remarks>
        public static Diagnostic[] GetSyntaxAndCompilationDiagnostics(SyntaxTree tree) => GetSyntaxAndCompilationDiagnostics(tree, out _, out _);
        /// <summary>
        /// Compile a syntax tree and returns syntax and compilation diagnostics
        /// </summary>
        /// <param name="tree">The syntax tree to compile.</param>
        /// <param name="syntaxDiagnostics">Syntax diagnostics.</param>
        /// <param name="compilationDiagnostics">Compilation diagnostics.</param>
        /// <returns>The number of syntax and compilation diagnostics. Zero means success.</returns>
        /// <remarks>
        /// Can be used in unit tests like this:<code><![CDATA[
        ///    var diags = Utilities.GetSyntaxAndCompilationDiagnostics(syntaxTree);
        ///    Assert.AreEqual(0, diags.Length);
        /// ]]></code></remarks>
        public static Diagnostic[] GetSyntaxAndCompilationDiagnostics(SyntaxTree tree, out Diagnostic[] syntaxDiagnostics, out Diagnostic[] compilationDiagnostics)
        {
            syntaxDiagnostics = tree.GetDiagnostics().ToArray();

            var compilation = Compilation.Value.AddSyntaxTrees(tree);
            compilationDiagnostics = compilation.GetDiagnostics().Where(diag => !DiagnosticAccepted(diag)).ToArray();

            return syntaxDiagnostics.Concat(compilationDiagnostics).ToArray();

            // Consider compilation as success for following warnings
            static bool DiagnosticAccepted(Diagnostic diagnostic)
            {
                return diagnostic.Id == "CS8019"; // warning CS8019: Unnecessary using directive.
            }
        }

        private static readonly Lazy<CSharpCompilation> Compilation = new(() =>
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var references = GetReferencePaths();
            return CSharpCompilation.Create(Guid.NewGuid().ToString("N"), options: options)
                .AddReferences(references.Select(path => MetadataReference.CreateFromFile(path)));

            static IEnumerable<string> GetReferencePaths()
            {
                // do not reference LinqToXsd.Schemas.dll as this assembly already contains the types we are currently compiling.
                var excludedFileNames = new string[] { "LinqToXsd.Schemas.dll" };

                var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                    .Split(Path.PathSeparator)
                    .Where(path => !excludedFileNames.Contains(Path.GetFileName(path)))
                    .OrderBy(_ => _)
                    .ToArray();

                return GetRuntimeReferences(referencePaths);

                static IEnumerable<string> GetRuntimeReferences(params string[] fileNames)
                {
                    var runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                    return fileNames.Select(fileName => Path.Combine(runtimeDirectory, fileName));
                }
            }
        });
    }
}