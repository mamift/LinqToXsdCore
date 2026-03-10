#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Scriban;
using Scriban.Runtime;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.CodeGen.Model;
using Xml.Schema.Linq.CodeGen.Scriban;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq;

/// <summary>
/// Static methods to support multiple ways of generating code.
/// </summary>
public static class XObjectsCoreGenerator
{
    /// <summary>
    /// Creates a new instance of <see cref="LinqToXsdSettings"/>, optionally by loading from an XML file.
    /// </summary>
    /// <param name="fromXmlFile">Null, empty or non-existent file path value will simply return a default instance.</param>
    /// <returns></returns>
    public static LinqToXsdSettings LoadLinqToXsdSettings(string? fromXmlFile = null)
    {
        return fromXmlFile.IsNotEmpty() && File.Exists(fromXmlFile)
            ? new LinqToXsdSettings(fromXmlFile)
            : new LinqToXsdSettings();
    }

    /// <summary>
    /// Generates code for a sequence of file paths and an instance of a <see cref="LinqToXsdSettings"/>.
    /// </summary>
    /// <param name="xsdFilePaths"></param>
    /// <param name="settings"></param>
    /// <param name="programObserver"></param>
    /// <returns></returns>
    public static Dictionary<string, string> Generate(
        IEnumerable<string> xsdFilePaths,
        LinqToXsdSettings settings, 
        IWarnableObserver<string>? programObserver = null)
    {
        if (xsdFilePaths == null) throw new ArgumentNullException(nameof(xsdFilePaths));

        return xsdFilePaths
            .SelectMany(file => Generate(file, settings))
            // Multiple XSD files may import the same namespace, e.g. in case of a shared schema.
            // In this case we arbitrary keep the first occurence.
            .Distinct(new FileNameComparer())
            .ToDictionary(x => x.filename, x => x.code);
    }

    /// <summary>
    /// Generates code using a given <see cref="xsdFilePath"/>, and an optionally, the file path to a configuration file.
    /// </summary>
    /// <param name="xsdFilePath"></param>
    /// <param name="linqToXsdSettingsFilePath"></param>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
    public static IEnumerable<(string filename, string code)> Generate(string xsdFilePath, string? linqToXsdSettingsFilePath = null)
    {
        if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
        var settings = LoadLinqToXsdSettings(linqToXsdSettingsFilePath);
        return Generate(xsdFilePath, settings);
    }

    /// <summary>
    /// Generates code using a given <see cref="xsdFilePath"/> for a single file, and an optional <see cref="LinqToXsdSettings"/> instance.
    /// </summary>
    /// <param name="xsdFilePath"></param>
    /// <param name="settings">If null, uses default or </param>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="xsdFilePath"/> is <see langword="null"/></exception>
    public static IEnumerable<(string filename, string code)> Generate(string xsdFilePath, LinqToXsdSettings? settings = null)
    {
        if (xsdFilePath.IsEmpty()) throw new ArgumentNullException(nameof(xsdFilePath));
        settings ??= new LinqToXsdSettings();

        using var xmlReader = XmlReader.Create(xsdFilePath, Defaults.DefaultXmlReaderSettings);

        XmlSchemaSet schemaSet = xmlReader.ToXmlSchemaSet();

        string xsdFolder = Path.GetDirectoryName(xsdFilePath)!;

        return Generate(schemaSet, settings)
            .Select(x =>
            {
                // When SplitCodeFiles by Namespace is configured,
                // Generate() returns a tuple (clrNamespace, writer) per CLR namespace that we need to map to a file.
                // Otherwise, Generate() returns a single (null, writer) tuple.
                var filename = string.IsNullOrEmpty(x.clrNamespace)
                    ? xsdFilePath
                    : settings.NamespaceFileMap.TryGetValue(x.clrNamespace, out string? nsFile)
                        ? Path.Combine(xsdFolder, nsFile)
                        : throw new LinqToXsdException(
                            $"CLR namespace {x.clrNamespace} has no output file configured, which is required when using SplitCodeFiles by Namespace configuration.");

                return (filename, x.code);
            });
    }

    /// <summary>
    /// Generates code using a given <paramref name="schemaSet"/> of XSD's and a <see cref="LinqToXsdSettings"/> instance.
    /// </summary>
    /// <param name="schemaSet"></param>
    /// <param name="settings"></param>
    /// <returns>
    ///     A single (null, StringWriter) when configuration doesn't split files per namespace.
    ///     Otherwise, one StringWriter per CLR namespace.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="schemaSet"/> is <see langword="null"/></exception>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="settings"/> is <see langword="null"/></exception>
    public static IEnumerable<(string? clrNamespace, string code)> Generate(XmlSchemaSet schemaSet, LinqToXsdSettings? settings = null)
    {
        if (schemaSet == null) throw new ArgumentNullException(nameof(schemaSet));
        settings ??= new LinqToXsdSettings();

        var xsdConverter = new XsdToTypesConverter(settings);
        ClrMappingInfo mapping = xsdConverter.GenerateMapping(schemaSet);

        var codeGenerator = new CodeDomTypesGenerator(settings);
        var namespaces = codeGenerator.GenerateTypes(mapping);

        var template = TemplateLoader.Load("file.scriban-cs");
        
        return settings.SplitFilesByNamespace
            ? namespaces.GroupBy(ns => ns.Name).Select(g => ((string?)g.Key, RenderCodeUnit(g)))
            : [ (null, RenderCodeUnit(namespaces)) ];

        string RenderCodeUnit(IEnumerable<CNamespace> namespaces)
        {
            var nsArray = namespaces.ToArray();

            var globals = new ScriptObject();
            globals.Import(typeof(ScribanGlobals));
            globals.Import(
                new 
                { 
                    Settings = settings,
                    TypeManager = new 
                    {
                        // TODO: when RootElement is a POCO model class, it should provide CNamespace more easily
                        Namespace = nsArray.FirstOrDefault(ns => ns.Dom == codeGenerator.RootElement.ParentNamespace),
                        RootElement = codeGenerator.RootElement,
                        AllTypes = codeGenerator.AllTypes,
                        AllElements = codeGenerator.AllElements,
                        AllWrappers = codeGenerator.AllWrappers,
                    },
                    Namespaces = nsArray,
                }, 
                renamer: m => m.Name);

            var context = new TemplateContext()
            {
                MemberRenamer = m => m.Name,
                TemplateLoader = new TemplateLoader(),
            };
            context.PushGlobal(globals);

            return template.Render(context);
        }
    }

    /// <summary>
    /// Generates code by searching for an accompanying configuration file, whereby each configuration file is named the same as the XSD file, but with an
    /// .config extension (i.e. schemaFileName.xsd.config). Will skip over XSDs that have no accompanying .config file.
    /// </summary>
    /// <param name="schemaFiles"></param>
    /// <param name="observer"></param>
    /// <returns></returns>
    public static Dictionary<string, string> Generate(IEnumerable<string> schemaFiles, IWarnableObserver<string>? observer = null)
    {
        // xsd file paths are keys, the FileInfo's to their config files are values
        List<(FileInfo xsdFile, FileInfo configFile)> dictOfSchemasAndTheirConfigs = schemaFiles
            .Select(xsdFilePath => {
                var configFile = new FileInfo($"{xsdFilePath}.config");
                var xsdFile = new FileInfo(xsdFilePath);
                return (xsdFile, configFile);
            })
            .ToList();
        
        var excludeV11Xsds = dictOfSchemasAndTheirConfigs
            .Where(filePairs => filePairs.xsdFile.Exists && filePairs.xsdFile.GetXmlSchemaVersion() != XmlSchemaVersion.Version1_1)
            .ToList();

        if (excludeV11Xsds.Count != dictOfSchemasAndTheirConfigs.Count) {
            observer?.OnWarn("Found some XSD v1.1 schemas: this tool does not support XSD v1.1. and will ignore those.");
        }

        observer?.OnNext($"Schemas to process: {excludeV11Xsds.ToDelimitedString(e => Path.GetFileName(e.xsdFile.Name), ';')}");

        return excludeV11Xsds
            .SelectMany(pair => Generate(pair.xsdFile.FullName, pair.configFile.FullName))
            // Multiple XSD files may import the same namespace, e.g. in case of a shared schema.
            // In this case we arbitrary keep the first occurence.
            .Distinct(new FileNameComparer())
            .ToDictionary(x => x.filename, x => x.code);
    }

    class FileNameComparer : IEqualityComparer<(string filename, string code)>
    {
        public bool Equals((string filename, string code) x, (string filename, string code) y)
            => x.filename == y.filename;

        public int GetHashCode((string filename, string code) obj)
            => obj.filename?.GetHashCode() ?? 0;
    }
}