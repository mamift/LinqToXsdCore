using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using NUnit.Framework;
using OneOf;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    public static class Utilities
    {
        /// <summary>
        /// Assuming that other XSDs exist in the same directory as the given <paramref name="fileName"/>, this will pre-load those
        /// additional XSDs into an <see cref="XmlPreloadedResolver"/> and use them if they are referenced by the file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="mfs"></param>
        /// <returns>Returns a compiled <see cref="XmlSchemaSet"/></returns>
        public static XmlSchemaSet PreLoadXmlSchemas(string fileName, MockFileSystem mfs)
        {
            if (fileName.IsEmpty()) throw new ArgumentNullException(nameof(fileName));

            var xsdFile = mfs.FileInfo.New(fileName);
            var directoryInfo = mfs.DirectoryInfo.New(xsdFile.DirectoryName!);
            var additionalXsds = directoryInfo.GetFiles("*.xsd")
                .Where(f => f.FullName != xsdFile.FullName);

            var xmlPreloadedResolver = new MockXmlUrlResolver(mfs);

            foreach (var xsd in additionalXsds) {
                var pathRoot = Path.GetPathRoot(xsd.FullName) ?? "";
                var unrooted = xsd.FullName.Replace(pathRoot, "");

                var xsdText = new StreamReader(xsd.OpenRead()).ReadToEnd();
                Assert.IsNotNull(xsdText);
                Assert.IsFalse(string.IsNullOrWhiteSpace(xsdText));
                xmlPreloadedResolver.Add(new Uri($"file://{xsd.FullName}", UriKind.Absolute), xsd);
            }

            var xmlReaderSettings = new XmlReaderSettings() {
                DtdProcessing = DtdProcessing.Ignore,
                CloseInput = true
            };

            var xmlReader = XmlReader.Create(xsdFile.OpenRead(), xmlReaderSettings);
            XmlSchemaSet xmlSchemaSet = xmlReader.ToXmlSchemaSet(xmlPreloadedResolver);

            return xmlSchemaSet;
        }

        public static Dictionary<IFileInfo, XDocument> FilterOutSchemasThatAreIncludedOrImported(this Dictionary<IFileInfo, XDocument> xDocs)
        {
            var actualSchemas = xDocs.Where(kvp => kvp.Value.IsAnXmlSchema()).ToList();
            var allImportReferences = actualSchemas.SelectMany(kvp => kvp.Value.Descendants(XDocumentExtensions.ImportXName));
            var allIncludeReferences = actualSchemas.SelectMany(kvp => kvp.Value.Descendants(XDocumentExtensions.IncludeXName));

            var importAndIncludeElements = allIncludeReferences.Union(allImportReferences).ToList();
            var schemaLocationXName = XName.Get("schemaLocation");

            var filesReferredToInImportAndIncludeElements = importAndIncludeElements
                .SelectMany(iie => iie.Attributes(schemaLocationXName))
                .Distinct(XAttributeValueEqualityComparer.Default)
                .Select(attr => attr.Value.Replace("/", ".").Replace("\\", "."));

            var theXDocsReferencedByImportOrInclude = from xDoc in xDocs
                where filesReferredToInImportAndIncludeElements.Any(file => {
                    return string.Equals(file, xDoc.Key.Name, StringComparison.CurrentCultureIgnoreCase);
                })
                select xDoc;

            return theXDocsReferencedByImportOrInclude.ToDictionary(key => key.Key, kvp => kvp.Value);
        }


        public static List<IFileInfo> ResolveFileAndFolderPathsToMockFileInfos(MockFileSystem mfs, 
            IEnumerable<string> sequenceOfFileAndOrFolderPaths, string filter = "*.*")
        {
            if (sequenceOfFileAndOrFolderPaths == null) throw new ArgumentNullException(nameof(sequenceOfFileAndOrFolderPaths));

            var enumeratedFileAndOrFolderPaths = sequenceOfFileAndOrFolderPaths.ToList();

            if (!enumeratedFileAndOrFolderPaths.Any())
                throw new InvalidOperationException("There are no file or folder paths present in the enumerable!");

            var dirs = enumeratedFileAndOrFolderPaths.Where(sf => mfs.GetFile(sf).Attributes.HasFlag(FileAttributes.Directory)).ToArray();
            var files = enumeratedFileAndOrFolderPaths.Except(dirs).Select(f => mfs.FileInfo.New(f)).ToList();
            var filteredFiles = dirs.SelectMany(d =>
                new MockDirectoryInfo(mfs, d).GetFiles(filter, SearchOption.AllDirectories).Select(f => f)).ToList();
            files.AddRange(filteredFiles);
            return files;
        }


        public static List<IFileInfo> ResolvePossibleFileAndFolderPathsToProcessableSchemas(MockFileSystem mfs,
            IEnumerable<string> filesOrFolders)
        {
            var files = ResolveFileAndFolderPathsToMockFileInfos(mfs, filesOrFolders, "*.xsd");
            var filesComparisonList = files.Select(f => f.FullName);

            // convert files to XDocuments and check if they are proper W3C schemas
            var pairs = files.Select(f => (file: f, schema: XDocument.Parse(mfs.GetFile(f).TextContents)));
            var xDocs = pairs.Where(kvp => kvp.schema.IsAnXmlSchema())
                .ToDictionary(kvp => kvp.file, kvp => kvp.schema);

            var filteredIncludeAndImportRefs = xDocs.FilterOutSchemasThatAreIncludedOrImported().Select(kvp => kvp.Key).ToList();
            var filteredIncludeAndImportRefsComparisonList = filteredIncludeAndImportRefs.Select(f => f.FullName);
            
            var resolvedSchemaFilesFilteredList = filesComparisonList.Except(filteredIncludeAndImportRefsComparisonList).Distinct().ToList();
            var resolvedSchemaFiles = resolvedSchemaFilesFilteredList.Select(fn => mfs.FileInfo.New(fn)).ToList();

            if (filteredIncludeAndImportRefs.Count == files.Count && !resolvedSchemaFilesFilteredList.Any()) {
                throw new LinqToXsdException("Cannot decide which XSD files to process as the specified " +
                                             "XSD files or folder of XSD files recursively import and/or " +
                                             "include each other! In this case you must explicitly provide" +
                                             "a file path and not a folder path.");
            }

            return resolvedSchemaFiles;
        }
        
        public static MockFileSystem GetAggregateMockFileSystem(IEnumerable<Assembly> assemblies)
        {
            var mockFs = new MockFileSystem();
            foreach (var assembly in assemblies) {
                if (assembly.GetName().Name!.Contains("GelML")) {
                    //Debugger.Break();
                }
                var fileData = GetAssemblyTextFilesDictionary(assembly);
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

        public static Dictionary<string, MockFileData> GetAssemblyTextFilesDictionary(Assembly assembly)
        {
            var names = assembly.GetManifestResourceNames();

            var rootName = assembly.GetName().Name + ".";
            var replacementRegex = new Regex(rootName);

            var streams = names.Select(n => {
                var rootNameReplaced = replacementRegex.Replace(n, rootName.Replace(".", "\\"), 1);
                return (
                    name: rootNameReplaced,
                    stream: assembly.GetManifestResourceStream(n));
            }).ToList();

            var contents = streams.Select(tu => (tu.name, data: new MockFileData(tu.stream.ReadAsString(dispose: true))))
                .ToDictionary(k => k.name, v => v.data);
            
            return contents;
        }

        public static string WarningMessage(object expected, object actual, [CallerMemberName] string caller = "")
        {
            return caller + "() failed; expected " + expected + ", got " + actual;
        }
        
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

            return SourceText.From(writer.ToString());
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
        /// <returns></returns>
        public static SourceText GenerateSourceText(XmlSchemaSet xmlSchemaSet, string xsdFileName,
            IMockFileDataAccessor mfs)
        {
            var possibleSettingsFile = $"{xsdFileName}.config";
            var fileExists = mfs.FileExists(possibleSettingsFile);
            Configuration config = fileExists
                ? Configuration.Load(new StringReader(mfs.GetFile(possibleSettingsFile).TextContents))
                : Configuration.LoadForSchema(XDocument.Parse(mfs.GetFile(xsdFileName).TextContents));

            var ns = config.Namespaces.Untyped;

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
        public static CSharpSyntaxTree GenerateSyntaxTree(string xsdFile, IMockFileDataAccessor fs)
        {
            return GenerateSyntaxTree(new MockFileInfo(fs, xsdFile), fs);
        }
        
        public static XmlSchemaSet GetXmlSchemaSet(IFileInfo xsdFile, IMockFileDataAccessor fs)
        {
            if (xsdFile == null) throw new ArgumentNullException(nameof(xsdFile));

            var folderWithAdditionalXsdFiles = xsdFile.DirectoryName;
            var directoryInfo = new MockDirectoryInfo(fs, folderWithAdditionalXsdFiles);
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

            return tree as CSharpSyntaxTree;
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

                var appCtxData = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
                
                #if !NET6_0
                if (appCtxData == null) {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    appCtxData = assemblies.Select(a => a.Location).ToDelimitedString(Path.PathSeparator);
                }
                #endif

                var referencePaths = appCtxData
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