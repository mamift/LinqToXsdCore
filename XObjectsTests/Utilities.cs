using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

            return SourceText.From(code.ToString());
        }

        /// <summary>
        /// Generates C# code from a given <paramref name="xsdFilePath"/> and then returns the <see cref="CSharpSyntaxTree"/> of
        /// </summary>
        public static CSharpSyntaxTree GenerateSyntaxTree(string xsdFilePath) => GenerateSyntaxTree(new FileInfo(xsdFilePath));
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