using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Resolvers;
using System.Xml.Schema;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OneOf;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    public static class Utilities
    {
        public static Dictionary<IFileInfo, XDocument> FilterOutSchemasThatAreIncludedOrImported(this Dictionary<IFileInfo, XDocument> xDocs)
        {
            var actualSchemas = xDocs.Where(kvp => kvp.Value.IsAnXmlSchema()).ToList();
            var allImportReferences = actualSchemas.SelectMany(kvp => kvp.Value.Descendants(XDocumentExtensions.ImportXName));
            var allIncludeReferences = actualSchemas.SelectMany(kvp => kvp.Value.Descendants(XDocumentExtensions.IncludeXName));

            var importAndIncludeElements = allIncludeReferences.Union(allImportReferences).ToList();
            var schemaLocationXName = XName.Get("schemaLocation");

            var filesReferredToInImportAndIncludeElements = importAndIncludeElements
                .SelectMany(iie => iie.Attributes(schemaLocationXName))
                .Distinct(new XAttributeValueEqualityComparer())
                .Select(attr => attr.Value.Replace("/", ".").Replace("\\", "."));

            var theXDocsReferencedByImportOrInclude = from xDoc in xDocs
                where filesReferredToInImportAndIncludeElements.Any(file => {
                    return string.Equals(file, xDoc.Key.Name, StringComparison.CurrentCultureIgnoreCase);
                })
                select xDoc;

            return theXDocsReferencedByImportOrInclude.ToDictionary(key => key.Key, kvp => kvp.Value);
        }
        
        public static MockFileSystem GetAggregateMockFileSystem(IEnumerable<Assembly> assemblies)
        {
            var mockFs = new MockFileSystem();
            foreach (var assembly in assemblies) {
                string? name = assembly.GetName().Name;
                if (name!.Contains("GelML")) {
                    //Debugger.Break();
                }
                Dictionary<string, MockFileData> fileData;
                // the assembly name doens't match the namespace for this one 
                if (assembly.FullName!.Contains("LinqToXsd.Schemas")) {
                    fileData = GetAssemblyTextFilesDictionary(assembly, "Xml.Schema.Linq");
                }
                else {
                    fileData = GetAssemblyTextFilesDictionary(assembly);
                }
                    
                foreach (var kvp in fileData) {
                    var possibleExistingPath = kvp.Key;

                    if (mockFs.FileExists(possibleExistingPath)) {
                        throw new InvalidOperationException($"Possibly existing file in test data: [{possibleExistingPath}]");
                    }
                    mockFs.AddFile(kvp.Key, kvp.Value);
                }
            }

            return mockFs;
        }

        /// <summary>
        /// Load all the embedded resources of a given assembly and load it into a <see cref="MockFileSystem"/>.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static MockFileSystem GetAssemblyFileSystem(Assembly assembly)
        {
            return new MockFileSystem(GetAssemblyTextFilesDictionary(assembly));
        }

        public static Dictionary<string, MockFileData> GetAssemblyTextFilesDictionary(Assembly assembly, string? customRootName = null)
        {
            var names = assembly.GetManifestResourceNames();
            var info = names.Select(n => assembly.GetManifestResourceInfo(n)).ToList();

            var rootName = (customRootName ?? assembly.GetName().Name) + ".";
            var replacementRegex = new Regex(rootName);

            var streams = names.Select(n => {
                var rootNameReplaced = replacementRegex.Replace(n, rootName.Replace(".", "\\"), 1);
                return (
                    name: rootNameReplaced,
                    stream: assembly.GetManifestResourceStream(n)
                );
            }).ToList();

            var contents = streams.Select(tu => (tu.name, data: new MockFileData(tu.stream.ReadAsString(dispose: true))))
                .ToDictionary(k => k.name, v => v.data);
            
            return contents;
        }

        public static string WarningMessage(object expected, object actual, [CallerMemberName] string caller = "")
        {
            return caller + "() failed; expected " + expected + ", got " + actual;
        }
        
        /// <summary>
        /// Runs the <see cref="XObjectsCoreGenerator"/> to generates C# code, as <see cref="SourceText"/> from a given XSD file name.
        /// </summary>
        /// <param name="xsdFileName"></param>
        /// <param name="fs"></param>
        /// <returns></returns>
        public static SourceText GenerateSourceText(string xsdFileName, IMockFileDataAccessor fs)
        {
            var possibleSettingsFilePath = $"{xsdFileName}.config";
            
            var xsdFile = new MockFileInfo(fs, xsdFileName);
            var possibleSettings = new MockFileInfo(fs, possibleSettingsFilePath);
            var schemaSet = GetXmlSchemaSet(xsdFile, fs);

            IEnumerable<(string filename, TextWriter writer)> codeWriters;
            if (possibleSettings.Exists) {
                LinqToXsdSettings settings = XObjectsCoreGenerator.LoadLinqToXsdSettings(XDocument.Load(possibleSettings.OpenRead()));
                codeWriters = XObjectsCoreGenerator.Generate(schemaSet, settings);
            } else {
                codeWriters = XObjectsCoreGenerator.Generate(schemaSet);
            }

            // This method assumes SplitCodeFile is not used, so there's only a single writer per file.
            var writer = codeWriters.Single().writer;
            return SourceText.From(writer.ToString()!);
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
        /// Used specifically for unit testing, invokes the
        /// <see cref="XObjectsCoreGenerator.Generate(XmlSchemaSet,LinqToXsdSettings)"/>
        /// method for generating C# code.
        /// </summary>
        /// <param name="xmlSchemaSet"></param>
        /// <param name="xsdFileName">Required for loading any configuration files. Accepts relative and absolute.</param>
        /// <param name="mfs"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static SourceText GenerateSourceText(XmlSchemaSet xmlSchemaSet, string xsdFileName, IMockFileDataAccessor mfs, LinqToXsdSettings? settings = null)
        {
            var possibleSettingsFile = $"{xsdFileName}.config";
            var fileExists = mfs.FileExists(possibleSettingsFile);
            Configuration config = fileExists
                ? Configuration.Load(new StringReader(mfs.GetFile(possibleSettingsFile).TextContents))
                : Configuration.LoadForSchema(XDocument.Parse(mfs.GetFile(xsdFileName).TextContents));

            var ns = config.Namespaces.Untyped;

            settings ??= config.ToLinqToXsdSettings();
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
        public static CSharpSyntaxTree GenerateSyntaxTree(string xsdFile, IMockFileDataAccessor fs)
        {
            return GenerateSyntaxTree(new MockFileInfo(fs, xsdFile), fs);
        }
        
        public static XmlSchemaSet GetXmlSchemaSet(IFileInfo xsdFile, IMockFileDataAccessor fs)
        {
            if (xsdFile == null) throw new ArgumentNullException(nameof(xsdFile));
            if (fs == null) throw new ArgumentNullException(nameof(fs));

            var folderWithAdditionalXsdFiles = xsdFile.DirectoryName;
            MockDirectoryInfo directoryInfo = new MockDirectoryInfo(fs, folderWithAdditionalXsdFiles);
            var additionalXsds = directoryInfo.GetFiles("*.xsd").Where(f => f.FullName != xsdFile.FullName).ToArray();

            var xmlPreloadedResolver = new MockXmlUrlResolver(fs);

            foreach (var xsd in additionalXsds) {
                var uri = new Uri($"{xsd.Name}", UriKind.Relative);
                xmlPreloadedResolver.Add(uri, xsd);
            }

            var xmlReaderSettings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true
            };
            var schemaSet = XmlReader.Create(xsdFile.OpenRead(), xmlReaderSettings)
                .ToXmlSchemaSet(xmlPreloadedResolver);

            return schemaSet;
        }

        /// <summary>
        /// Generates C# code from a given <paramref name="xsdFile"/> and then returns the <see cref="CSharpSyntaxTree"/> of
        /// the generated code.
        /// </summary>
        public static CSharpSyntaxTree GenerateSyntaxTree(IFileInfo xsdFile, IMockFileDataAccessor mfs)
        {
            var schemaSet = GetXmlSchemaSet(xsdFile, mfs);

            var sourceText = GenerateSourceText(schemaSet, xsdFile.FullName, mfs);
            var stringBuilder = new StringBuilder();
            using var writer = new StringWriter(stringBuilder);
            sourceText.Write(writer);

            var tree = CSharpSyntaxTree.ParseText(sourceText, CSharpParseOptions.Default);

            return (CSharpSyntaxTree)tree;
        }

        public static OneOf<CSharpSyntaxTree, Exception> GenerateSyntaxTreeOrError(IFileInfo xsdFile, IMockFileDataAccessor mfs)
        {
            try {
                return GenerateSyntaxTree(xsdFile, mfs);
            }
            catch (Exception ex) {
                return ex;
            }
        }

        /// <summary>
        /// Generates C# code from a given <paramref name="xsdFile"/> and then returns the <see cref="CSharpSyntaxTree"/> of
        /// the generated code.
        /// </summary>
        public static CSharpSyntaxTree GenerateSyntaxTree(FileInfo xsdFile)
        {
            var schemaSet = GetXmlSchemaSet(xsdFile);

            var sourceText = GenerateSourceText(schemaSet, xsdFile.FullName);
            using var writer = new StreamWriter(xsdFile.FullName + ".cs");
            sourceText.Write(writer);

            var tree = CSharpSyntaxTree.ParseText(sourceText, CSharpParseOptions.Default);

            return tree as CSharpSyntaxTree;
        }

        /// <summary>
        /// Expects a real file from the physical file system.
        /// </summary>
        /// <param name="xsdFile"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static XmlSchemaSet GetXmlSchemaSet(FileInfo xsdFile)
        {
            if (xsdFile == null) throw new ArgumentNullException(nameof(xsdFile));

            var directoryInfo = new DirectoryInfo(xsdFile.DirectoryName ?? throw new InvalidOperationException("Invalid dir for XSD file!"));
            var additionalXsds = directoryInfo.GetFiles("*.xsd");

            var xmlPreloadedResolver = new XmlPreloadedResolver();

            foreach (var xsd in additionalXsds) {
                xmlPreloadedResolver.Add(new Uri($"file://{xsd.FullName}"), File.OpenRead(xsd.FullName));
            }

            var xmlReaderSettings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true
            };
            var schemaSet = XmlReader.Create(xsdFile.FullName, xmlReaderSettings)
                .ToXmlSchemaSet(xmlPreloadedResolver);
            return schemaSet;
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

        public static string GetDirectoryNameOfFolderAbove(string startFromDir, string folderName)
        {
            var currentDir = GetParentDirectoryName(startFromDir);
            while (currentDir != null)
            {
                var name = Directory
                    .EnumerateDirectories(currentDir, folderName)
                    .SingleOrDefault();

                if (name != null)
                {
                    return name;
                }

                currentDir = GetParentDirectoryName(currentDir);
            }
            return null;
        }

        public static string GetParentDirectoryName(string path)
        {
            return Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        private static readonly Lazy<CSharpCompilation> Compilation = new(() =>
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var references = GetReferencePaths();
            return CSharpCompilation.Create(Guid.NewGuid().ToString("N"), options: options)
                .AddReferences(references.Select(path => MetadataReference.CreateFromFile(path)));

            static IEnumerable<string> GetReferencePaths()
            {
                // do not reference 'GeneratedSchemaLibraries' as they potentially contains the types we are currently compiling.
                // assume that the assemblies have the same name as the folder they are in.
                var generatedSchemasRootDir = GetDirectoryNameOfFolderAbove(AppContext.BaseDirectory, "GeneratedSchemaLibraries");
                var excludedFileNames = Directory
                    .EnumerateDirectories(generatedSchemasRootDir, "*", SearchOption.TopDirectoryOnly)
                    .Select(path => Path.GetFileName(path))
                    .ToArray();

                var appCtxData = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
                
                #if !NET6_0
                if (appCtxData == null) {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    appCtxData = assemblies.Select(a => a.Location).ToDelimitedString(Path.PathSeparator);
                }
                #endif

                var referencePaths = appCtxData
                    .Split(Path.PathSeparator)
                    .Where(path => !excludedFileNames.Contains(Path.GetFileNameWithoutExtension(path)))
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