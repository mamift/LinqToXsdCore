#nullable enable
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq
{
    /// <summary>
    /// Static methods to support multiple ways of generating code.
    /// </summary>
    public static class XObjectsCoreGenerator
    {
        /// <summary>
        /// Creates a new instance of <see cref="LinqToXsdSettings"/>, optionally by loading from an XML file.
        /// </summary>
        /// <param name="fromXmlFile">Null, empty or non-existent file path value will simply return a default instance.</param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static LinqToXsdSettings LoadLinqToXsdSettings(string? fromXmlFile = null, IWarnableObserver<string>? observer = null)
        {
            var settings = new LinqToXsdSettings();
            if (fromXmlFile.IsNotEmpty() && File.Exists(fromXmlFile))
            {
                settings.Load(fromXmlFile);
            }
            else if (observer != null) 
            {
                if (fromXmlFile.IsEmpty())
                {
                    observer?.OnWarn("Config file not provided; using default values");
                }
                else
                {
                    observer.OnWarn($"Config file '{fromXmlFile}' did not exist; using default values");
                }
            }

            return settings;
        }

        /// <summary>
        /// Creates a new instance of <see cref="LinqToXsdSettings"/> from an already parsed <see cref="XDocument"/>.
        /// </summary>
        /// <returns></returns>
        public static LinqToXsdSettings LoadLinqToXsdSettings(XDocument xdoc)
        {
            if (xdoc == null) throw new ArgumentNullException(nameof(xdoc));
            var settings = new LinqToXsdSettings();
            settings.Load(xdoc);

            return settings;
        }

        /// <summary>
        /// Generates code for a sequence of file paths and an instance of a <see cref="LinqToXsdSettings"/>.
        /// </summary>
        /// <param name="xsdFilePaths"></param>
        /// <param name="settings"></param>
        /// <param name="programObserver"></param>
        /// <returns></returns>
        public static Dictionary<string, TextWriter> Generate(IEnumerable<string> xsdFilePaths,
            LinqToXsdSettings settings, IWarnableObserver<string>? programObserver = null)
        {
            if (xsdFilePaths == null) throw new ArgumentNullException(nameof(xsdFilePaths));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            List<string> files = xsdFilePaths
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // When several schemas are generated together under one shared configuration, load them
            // all into a single XmlSchemaSet and emit each schema's types exactly once. Without this,
            // a schema that is xs:include/xs:import'd by more than one entry point is regenerated into
            // every importing file (see GitHub issue #14).
            // This is skipped when code is split by namespace or class, because each schema is then
            // responsible for producing its own set of split files.
            if (files.Count > 1 && settings.SplitCodeFilesBy == null)
            {
                if (TryGenerateMerged(files, settings, out var mergedWriters))
                    return mergedWriters;
            }

            var generated = files
                .SelectMany(file => Generate(file, settings)
                    .Select(generatedFile => (sourceSchema: file, filename: generatedFile.filename, writer: generatedFile.writer)));

            return DisambiguateClashingSplitFileNames(generated)
                // Multiple XSD files may import the same namespace, e.g. in case of a shared schema.
                // In this case we arbitrary keep the first occurence.
                .Distinct(new FileNameComparer())
                .ToDictionary(x => x.filename, x => x.writer);
        }

        /// <summary>
        /// Adds the source schema's file name to the name of any split output file that a different schema
        /// already produced with different content, e.g. the <c>LinqToXsdTypeManager</c> that every schema emits
        /// gets a file of the same name. Without this, the types of one schema would be silently dropped.
        /// <para>Files whose content is identical are left alone, as they are the same types generated from a
        /// schema that several entry points share. When generated code isn't split, no two files can clash and
        /// this method changes nothing.</para>
        /// </summary>
        private static IEnumerable<(string filename, TextWriter writer)> DisambiguateClashingSplitFileNames(
            IEnumerable<(string sourceSchema, string filename, TextWriter writer)> generatedFiles)
        {
            List<(string sourceSchema, string filename, TextWriter writer)> files = generatedFiles.ToList();

            var clashingFileNames = files
                .GroupBy(f => f.filename, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Select(f => f.writer.ToString()).Distinct(StringComparer.Ordinal).Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                if (!clashingFileNames.Contains(file.filename))
                {
                    yield return (file.filename, file.writer);
                    continue;
                }

                var directory = Path.GetDirectoryName(file.filename) ?? string.Empty;
                var sourceSchemaName = Path.GetFileNameWithoutExtension(file.sourceSchema);
                var disambiguatedFileName = $"{sourceSchemaName}.{Path.GetFileName(file.filename)}";

                yield return (Path.Combine(directory, disambiguatedFileName), file.writer);
            }
        }

        /// <summary>
        /// Key used to tag a generated <see cref="CodeTypeDeclaration"/> (via <see cref="CodeObject.UserData"/>)
        /// with the source URI of the schema that defines it, so that a merged generation pass can be split
        /// back into one output file per source schema.
        /// </summary>
        internal const string SourceSchemaUserDataKey = "LinqToXsd.SourceSchemaUri";

        /// <summary>
        /// Generates code for several schema files by loading them all into a single <see cref="XmlSchemaSet"/>,
        /// generating the CLR type model once, and then splitting the resulting types back into one output file
        /// per source schema. Falls back to <c>false</c> (so the caller uses per-file generation) when the schemas
        /// cannot be merged, for example when they cannot be compiled together.
        /// </summary>
        private static bool TryGenerateMerged(List<string> files, LinqToXsdSettings settings, out Dictionary<string, TextWriter> writers)
        {
            writers = null!;

            XmlSchemaSet schemaSet;
            try
            {
                schemaSet = LoadMergedSchemaSet(files);
            }
            catch (XmlSchemaException)
            {
                return false;
            }

            var xsdConverter = new XsdToTypesConverter(settings);
            ClrMappingInfo mapping = xsdConverter.GenerateMapping(schemaSet);
            if (mapping == null)
                return false;

            var codeGenerator = new CodeDomTypesGenerator(settings);
            List<CodeNamespace> namespaces = codeGenerator.GenerateTypes(mapping).ToList();

            writers = SplitNamespacesBySourceSchema(namespaces, files, settings);
            return writers.Count > 0;
        }

        private static XmlSchemaSet LoadMergedSchemaSet(IEnumerable<string> files)
        {
            var resolver = new XmlUrlResolver();
            var schemaSet = new XmlSchemaSet {
                XmlResolver = resolver
            };

            foreach (string file in files)
            {
                if (string.IsNullOrWhiteSpace(file)) continue;

                // Mirror the reader settings used by the single-file path.
                var readerSettings = new XmlReaderSettings {
                    DtdProcessing = Defaults.DefaultXmlReaderSettings.DtdProcessing,
                    CloseInput = Defaults.DefaultXmlReaderSettings.CloseInput
                };
                readerSettings.XmlResolver = resolver;

                using var reader = XmlReader.Create(file, readerSettings);
                schemaSet.Add(null, reader);
            }

            schemaSet.Compile();
            return schemaSet;
        }

        private static Dictionary<string, TextWriter> SplitNamespacesBySourceSchema(
            List<CodeNamespace> namespaces, List<string> inputFiles, LinqToXsdSettings settings)
        {
            // The TypeManager and XRoot(s) are synthetic and don't belong to a single source schema; they are
            // hosted in the first input file so the whole set still exposes one entry point for the typed-XML
            // infrastructure.
            string rootBucketPath = NormalizeSourcePath(Path.GetFullPath(inputFiles[0])) ?? Path.GetFullPath(inputFiles[0]);

            var buckets = new Dictionary<string, List<(CodeNamespace originalNamespace, CodeTypeDeclaration declaration)>>(StringComparer.OrdinalIgnoreCase);

            foreach (CodeNamespace ns in namespaces)
            {
                foreach (CodeTypeDeclaration declaration in ns.Types.Cast<CodeTypeDeclaration>().ToList())
                {
                    string sourcePath = NormalizeSourcePath(declaration.UserData[SourceSchemaUserDataKey] as string) ?? rootBucketPath;

                    if (!buckets.TryGetValue(sourcePath, out var list))
                    {
                        list = new List<(CodeNamespace, CodeTypeDeclaration)>();
                        buckets[sourcePath] = list;
                    }

                    list.Add((ns, declaration));
                }
            }

            List<string> inputFilePaths = inputFiles
                .Select(f => NormalizeSourcePath(Path.GetFullPath(f)) ?? Path.GetFullPath(f))
                .ToList();

            // Deterministic ordering: entry points first (in the order the user supplied them), then any
            // additionally discovered shared schemas sorted by path.
            List<string> orderedKeys = inputFilePaths
                .Where(buckets.ContainsKey)
                .Concat(buckets.Keys.Except(inputFilePaths, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var result = new Dictionary<string, TextWriter>(StringComparer.OrdinalIgnoreCase);

            foreach (string sourcePath in orderedKeys)
            {
                if (!buckets.TryGetValue(sourcePath, out var entries)) continue;

                var unit = new CodeCompileUnit();

                // A single schema can contribute types to several CLR namespaces, so preserve one CodeNamespace
                // per original namespace that has declarations in this bucket.
                foreach (var nsGroup in entries.GroupBy(e => e.originalNamespace.Name ?? string.Empty))
                {
                    var newNamespace = new CodeNamespace(nsGroup.Key);
                    CodeNamespace originalNamespace = nsGroup.First().originalNamespace;
                    foreach (CodeNamespaceImport import in originalNamespace.Imports)
                        newNamespace.Imports.Add(new CodeNamespaceImport(import.Namespace));

                    foreach (var (_, declaration) in nsGroup)
                        newNamespace.Types.Add(declaration);

                    unit.Namespaces.Add(newNamespace);
                }

                var writer = unit.ToStringWriter();
                writer.UpdateAutoGeneratedComment(assembly: typeof(XObjectsCoreGenerator).Assembly);
                if (settings.NullableReferences)
                    writer.InsertFilePragma("#nullable enable annotations");

                result[sourcePath] = writer;
            }

            return result;
        }

        private static string? NormalizeSourcePath(string? sourceUri)
        {
            if (string.IsNullOrWhiteSpace(sourceUri)) return null;

            string path = sourceUri;
            if (sourceUri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                try { path = new Uri(sourceUri).LocalPath; }
                catch (UriFormatException) { /* keep the raw value */ }
            }

            try { return Path.GetFullPath(path); }
            catch { return path; }
        }

        /// <summary>
        /// Generates code using a given <see cref="xsdFilePath"/>, and an optionally, the file path to a configuration file.
        /// </summary>
        /// <param name="xsdFilePath"></param>
        /// <param name="linqToXsdSettingsFilePath"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
        public static IEnumerable<(string filename, TextWriter writer)> Generate(string xsdFilePath, string? linqToXsdSettingsFilePath = null,
            IWarnableObserver<string>? observer = null)
        {
            if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
            LinqToXsdSettings settings = LoadLinqToXsdSettings(linqToXsdSettingsFilePath, observer);

            return Generate(xsdFilePath, settings);
        }

        /// <summary>
        /// Generates code using a given <see cref="xsdFilePath"/> for a single file, and an optional <see cref="LinqToXsdSettings"/> instance.
        /// </summary>
        /// <param name="xsdFilePath"></param>
        /// <param name="settings">If null, uses default or </param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
        public static IEnumerable<(string filename, TextWriter writer)> Generate(string xsdFilePath, LinqToXsdSettings? settings = null)
        {
            if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
            if (settings == null) settings = new LinqToXsdSettings();

            // Create a fresh XmlReaderSettings per invocation: Defaults.DefaultXmlReaderSettings is a
            // process-wide static that callers (e.g. unit tests) can mutate, which would change the
            // BaseURI resolution of schemas read here.
            var resolver = new XmlUrlResolver();
            var readerSettings = new XmlReaderSettings() {
                DtdProcessing = Defaults.DefaultXmlReaderSettings.DtdProcessing,
                CloseInput = Defaults.DefaultXmlReaderSettings.CloseInput
            };
            readerSettings.XmlResolver = resolver;
            var xmlReader = XmlReader.Create(xsdFilePath, readerSettings);
            List<string> preloadedFileNames = new();

            using (xmlReader) {
                XmlSchemaSet? schemaSet = null;
                // Pass the resolver explicitly so xs:import/xs:include resolution uses the reader's
                // resolver and BaseURI rather than anything inherited from shared/global state.
                try
                {
                    schemaSet = xmlReader.ToXmlSchemaSet(resolver);
                }
                catch (XmlSchemaException xse) when (xse.IsXmlNamespaceAttributeError())
                {
                    readerSettings.XmlResolver = null;
                    using var xmlReader2 = XmlReader.Create(xsdFilePath, readerSettings);
                    schemaSet = xmlReader2.CompileXmlSchemaSetWithPreloadedXsds(out preloadedFileNames);
                }

                // the compilation of XSDs resulted in using preloaded schemas, then use preloaded XSD configuration so
                // generated code is emitted in a user-friendly namespace
                if (preloadedFileNames.Any()) {
                    var assembly = typeof(XmlReaderExtensions).Assembly;
                    var names = assembly.GetManifestResourceNames();
                    bool isAnEmbeddedXmlSchema = false;
                    foreach (var fileName in preloadedFileNames) {
                        int findIndex = Array.FindIndex(names, n => n.EndsWith(fileName));
                        if (findIndex == -1)
                            throw new InvalidOperationException($"File name not found embedded in assembly: {fileName}");
                        isAnEmbeddedXmlSchema = true;
                        break;
                    }

                    if (isAnEmbeddedXmlSchema) {
                        var configFileName = Array.Find(names, n => n.EndsWith(PreloadedXsdsResolver.EmbeddedXsdFileNames.XmlXsdConfig));
                        var streamForFilename = assembly.GetManifestResourceStream(configFileName ?? throw new InvalidOperationException("Unable to find xml.xsd.config"));
                        settings.AppendFromAdditionalConfigFile(streamForFilename);
                    }
                }

                string? xsdFolder = Path.GetDirectoryName(xsdFilePath);

                return Generate(schemaSet, settings, Path.ChangeExtension(xsdFilePath, ".xsd-g.cs"))
                    .Select(x => {
                        // When SplitCodeFiles is configured Generate() returns one tuple per output file, whose
                        // name is relative to the folder of the XSD. Otherwise a single (null, writer) tuple is
                        // returned and the generated code goes to the XSD's own file name.
                        var filename = string.IsNullOrEmpty(x.outputFileName)
                            ? xsdFilePath
                            : Path.Combine(xsdFolder ?? string.Empty, x.outputFileName);

                        return (filename, x.writer);
                    });
            }
        }

        /// <summary>
        /// Generates code using a given <paramref name="schemaSet"/> of XSDs.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <param name="originalXsdFileName"></param>
        /// <returns>
        ///     A single (null, StringWriter) when the configuration doesn't split generated code across files.
        ///     Otherwise, one StringWriter per output file, keyed by the name of that file.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
        public static IEnumerable<(string outputFileName, TextWriter writer)> Generate(XmlSchemaSet schemaSet, string originalXsdFileName)
        {
            var settings = new LinqToXsdSettings();
            return Generate(schemaSet, settings, originalXsdFileName);
        }

        /// <summary>
        /// Generates code using a given <paramref name="schemaSet"/> of XSD's and a <see cref="LinqToXsdSettings"/> instance.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <param name="settings"></param>
        /// <param name="originalXsdFileName">Used when <see cref="LinqToXsdSettings.SplitCodeFilesBy"/> is <see cref="CodeGeneration.SplitCodeFilesLocalType.ByEnum.None"/> or not specified.</param>
        /// <returns>
        ///     A single (null, StringWriter) when the configuration doesn't split generated code across files.
        ///     Otherwise, one StringWriter per output file, keyed by the name of that file.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="settings"/> is <see langword="null"/></exception>
        public static IEnumerable<(string outputFileName, TextWriter writer)> Generate(XmlSchemaSet schemaSet, LinqToXsdSettings settings, string originalXsdFileName)
        {
            return GenerateCodeCompileUnits(schemaSet, settings, originalXsdFileName)
                .Select(x =>
                {
                    var writer = x.unit.ToStringWriter();

                    writer.UpdateAutoGeneratedComment(assembly: typeof(XObjectsCoreGenerator).Assembly);

                    if (settings.NullableReferences)
                    {
                        // HACK: CodeDom doesn't allow us to add #pragmas.
                        // In <auto-generated> code, CS mandates a "#nullable enable" pragma.
                        // So we add the pragma inside the generated text directly
                        writer.InsertFilePragma("#nullable enable annotations");
                    }

                    return (x.outputFileName, (TextWriter)writer);
                });
        }

        /// <summary>
        /// Creates <see cref="CodeCompileUnit"/>s from a given <see cref="XmlSchemaSet"/> and <see cref="LinqToXsdSettings"/>.
        /// </summary>
        /// <param name="schemaSet"></param>
        /// <param name="settings"></param>
        /// <param name="originalXsdFileName"></param>
        /// <returns>
        ///     A single (null, CodeCompileUnit) when configuration doesn't split generated code across files.
        ///     Otherwise, one CodeCompileUnit per output file, keyed by the name of that file.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="settings"/> is <see langword="null"/></exception>
        public static IEnumerable<(string outputFileName, CodeCompileUnit unit)> GenerateCodeCompileUnits(XmlSchemaSet schemaSet, LinqToXsdSettings settings, string originalXsdFileName)
        {
            if (schemaSet == null) throw new ArgumentNullException(nameof(schemaSet));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (originalXsdFileName == null) throw new ArgumentNullException(nameof(originalXsdFileName));
            var xsdConverter = new XsdToTypesConverter(settings);
            ClrMappingInfo mapping = xsdConverter.GenerateMapping(schemaSet);

            var codeGenerator = new CodeDomTypesGenerator(settings);
            var namespaces = codeGenerator.GenerateTypes(mapping);

            return settings.SplitCodeFilesBy switch
            {
                CodeGeneration.SplitCodeFilesLocalType.ByEnum.Namespace => SplitByNamespace(namespaces, settings),
                CodeGeneration.SplitCodeFilesLocalType.ByEnum.Class => SplitByClass(namespaces),
                _ => new[] { (Path.ChangeExtension(originalXsdFileName, ".xsd-g.cs"), BuildUnit(namespaces)) }
            };
        }

        /// <summary>
        /// The suffix given to each file produced when generated code is split across multiple files. Mirrors the
        /// file name the CLI gives to the single output file it writes by default, e.g. <c>schema.xsd-g.cs</c>.
        /// </summary>
        internal const string SplitCodeFileSuffix = ".xsd-g.cs";

        /// <summary>
        /// Puts each generated <see cref="CodeNamespace"/> into its own <see cref="CodeCompileUnit"/>.
        /// </summary>
        private static IEnumerable<(string outputFileName, CodeCompileUnit unit)> SplitByNamespace(
            IEnumerable<CodeNamespace> namespaces, LinqToXsdSettings settings)
        {
            var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var namespaceGroup in namespaces.GroupBy(ns => ns.Name ?? string.Empty)) {
                var fileName = ConfiguredNamespaceFileName(namespaceGroup.Key, settings);
                if (fileName == null)
                    fileName = UniqueOutputFileName(namespaceGroup.Key.IsEmpty() ? "GlobalNamespace" : namespaceGroup.Key, usedFileNames);
                else
                    usedFileNames.Add(fileName);

                yield return (fileName, BuildUnit(namespaceGroup));
            }
        }

        /// <summary>
        /// Returns the file name the <c>@File</c> attribute of a <c>Namespace</c> configuration element maps a CLR namespace
        /// to, or <c>null</c> when the configuration doesn't name one.
        /// </summary>
        private static string ConfiguredNamespaceFileName(string clrNamespace, LinqToXsdSettings settings)
        {
            if (string.IsNullOrEmpty(clrNamespace)) return null;

            return settings.NamespaceFileMap.TryGetValue(clrNamespace, out string configuredFileName) &&
                   !string.IsNullOrWhiteSpace(configuredFileName)
                ? configuredFileName
                : null;
        }

        /// <summary>
        /// Puts each top-level type declaration into its own <see cref="CodeCompileUnit"/>, along with the namespace
        /// (and its using directives) the type is declared in.
        /// </summary>
        private static IEnumerable<(string outputFileName, CodeCompileUnit unit)> SplitByClass(IEnumerable<CodeNamespace> namespaces)
        {
            var declarations = (from ns in namespaces
                                from CodeTypeDeclaration type in ns.Types.Cast<CodeTypeDeclaration>()
                                select (clrNamespace: ns.Name ?? string.Empty, containingNamespace: ns, type)).ToList();

            // The same type name can be generated into different CLR namespaces (e.g. types from an imported schema
            // are emitted alongside the importing schema's types), so those files are named with their CLR namespace.
            var clashingTypeNames = declarations
                .GroupBy(d => d.type.Name, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.Ordinal);

            var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var declaration in declarations) {
                var baseName = clashingTypeNames.Contains(declaration.type.Name) && !string.IsNullOrEmpty(declaration.clrNamespace)
                    ? $"{declaration.clrNamespace}.{declaration.type.Name}"
                    : declaration.type.Name;

                var unit = new CodeCompileUnit();
                unit.Namespaces.Add(NamespaceWithType(declaration.containingNamespace, declaration.type));

                yield return (UniqueOutputFileName(baseName, usedFileNames), unit);
            }
        }

        /// <summary>
        /// Appends <see cref="SplitCodeFileSuffix"/> to a file name, adding a counter if another output file has
        /// already taken that name.
        /// </summary>
        private static string UniqueOutputFileName(string baseName, HashSet<string> usedFileNames)
        {
            var fileName = $"{baseName}{SplitCodeFileSuffix}";
            for (var counter = 1; !usedFileNames.Add(fileName); counter++)
                fileName = $"{baseName}.{counter}{SplitCodeFileSuffix}";

            return fileName;
        }

        /// <summary>
        /// Creates a copy of a <see cref="CodeNamespace"/> that holds only the given <paramref name="type"/>, so that
        /// each split file retains the using directives its type needs.
        /// </summary>
        private static CodeNamespace NamespaceWithType(CodeNamespace containingNamespace, CodeTypeDeclaration type)
        {
            var codeNamespace = new CodeNamespace(containingNamespace.Name);
            foreach (CodeNamespaceImport import in containingNamespace.Imports)
                codeNamespace.Imports.Add(new CodeNamespaceImport(import.Namespace));
            codeNamespace.Types.Add(type);

            return codeNamespace;
        }

        private static CodeCompileUnit BuildUnit(IEnumerable<CodeNamespace> namespaces)
        {
            var ccu = new CodeCompileUnit();
            foreach (var ns in namespaces)
                ccu.Namespaces.Add(ns);
            return ccu;
        }

        /// <summary>
        /// Generates code by searching for an accompanying configuration file, whereby each configuration file is named the same as the XSD file, but with an
        /// .config extension (i.e. schemaFileName.xsd.config). Will skip v1.1 XSDs; XSDs that have no config file will be generated with default settings.
        /// </summary>
        /// <param name="schemaFiles"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static Dictionary<string, TextWriter> Generate(IEnumerable<string> schemaFiles,
            IWarnableObserver<string>? observer = null)
        {
            // xsd file paths are keys, the FileInfo's to their config files are values
            List<(FileInfo xsdFile, FileInfo configFile)> dictOfSchemasAndTheirConfigs = schemaFiles
                .Select(xsdFilePath => {
                    var configFile = new FileInfo($"{xsdFilePath}.config");
                    var xsdFile = new FileInfo(xsdFilePath);
                    return (xsdFile, configFile);
                })
                .ToList();
            
            List<(FileInfo xsdFile, FileInfo configFile)> excludeV11Xsds = dictOfSchemasAndTheirConfigs
                .Where(filePairs => filePairs.xsdFile.Exists && filePairs.xsdFile.GetXmlSchemaVersion() == XmlSchemaVersion.Version1_1)
                .ToList();

            if (excludeV11Xsds.Count > 0) {
                observer?.OnWarn("Found some XSD v1.1 schemas: this tool does not support XSD v1.1. and will ignore those.");
            }

            List<(FileInfo xsdFile, FileInfo configFile)> pairsToProcess = dictOfSchemasAndTheirConfigs.Except(excludeV11Xsds).ToList();
            
            observer?.OnNext($"Schemas to process: {pairsToProcess.ToDelimitedString(e => Path.GetFileName(e.xsdFile.Name), ';')}");

            // When several schemas are generated without any per-file configuration they all share the default
            // settings, so merge them into a single pass to avoid duplicating shared schemas (see GitHub issue #14).
            if (pairsToProcess.Count > 1 && pairsToProcess.All(pair => !pair.configFile.Exists))
            {
                List<string> files = pairsToProcess.Select(pair => pair.xsdFile.FullName).ToList();
                if (TryGenerateMerged(files, new LinqToXsdSettings(), out var mergedWriters))
                    return mergedWriters;
            }

            return DisambiguateClashingSplitFileNames(pairsToProcess
                    .SelectMany(pair => Generate(pair.xsdFile.FullName, pair.configFile.FullName, observer)
                        .Select(generatedFile => (sourceSchema: pair.xsdFile.FullName, filename: generatedFile.filename, writer: generatedFile.writer))))
                // Multiple XSD files may import the same namespace, e.g. in case of a shared schema.
                // In this case we arbitrary keep the first occurence.
                .Distinct(new FileNameComparer())
                .ToDictionary(x => x.filename, x => x.writer);
        }

        class FileNameComparer : IEqualityComparer<(string filename, TextWriter writer)>
        {
            public bool Equals((string filename, TextWriter writer) x, (string filename, TextWriter writer) y)
                => x.filename == y.filename;

            public int GetHashCode((string filename, TextWriter writer) obj)
                => obj.filename?.GetHashCode() ?? 0;
        }
    }
}