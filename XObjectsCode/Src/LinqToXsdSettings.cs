//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml.Linq;
using System.Collections.Generic;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq
{
    public enum GeneratedTypesVisibility
    {
        Public,
        Internal
    }

    public class LinqToXsdSettings
    {
        private Dictionary<string, string> namespaceMapping;
        public Dictionary<string, GeneratedTypesVisibility> NamespaceTypesVisibilityMap { get; } = new();
        public Dictionary<string, string> NamespaceFileMap { get; } = new();
        internal XElement trafo;
        private bool verifyRequired = false;
        private bool enableServiceReference = false;
        private readonly bool NameMangler2;

        public LinqToXsdSettings(bool nameMangler2 = false)
        {
            this.NameMangler2 = nameMangler2;
            namespaceMapping = new Dictionary<string, string>();
        }

        public LinqToXsdSettings(string filePath, bool nameMangler2 = false)
        {
            this.NameMangler2 = nameMangler2;
            namespaceMapping = new Dictionary<string, string>();
            Load(filePath);
        }

        public void Load(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
                throw new ArgumentException("Argument configFile should be non-null and non-empty.");

            Load(XDocument.Load(configFile));
        }

        public void Load(XDocument configDocument)
        {
            if (configDocument?.Root == null) throw new ArgumentNullException(nameof(configDocument));

            var rootElement = configDocument.Root;

            var namespacesElement = rootElement.Element(XName.Get("Namespaces", Constants.TypedXLinqNs));
            GenerateNamespaceMapping(namespacesElement);
            GenerateNamespaceVisibilityMapping(namespacesElement);
            GenerateNamespaceFileMapping(namespacesElement);

            var codegenElement = rootElement.Element(XName.Get("CodeGeneration", Constants.TypedXLinqNs));

            UseDateOnly = codegenElement?.Element(XName.Get("UseDateOnly", Constants.TypedXLinqNs))?.Value == "true";
            UseTimeOnly = codegenElement?.Element(XName.Get("UseTimeOnly", Constants.TypedXLinqNs))?.Value == "true";
            UseDateTimeOffset = codegenElement?.Element(XName.Get("UseDateTimeOffset", Constants.TypedXLinqNs))?.Value == "true";

            var splitFilesElement = codegenElement?.Element(XName.Get("SplitCodeFiles", Constants.TypedXLinqNs));
            SplitFilesByNamespace = splitFilesElement?.Attribute("By")?.Value == "Namespace";


            var nullableRefsName = XName.Get("NullableReferences", Constants.TypedXLinqNs);
            NullableReferences =
                (codegenElement?.Element(nullableRefsName) ?? rootElement.Element(nullableRefsName))?.Value == "true";

            trafo = rootElement.Element(XName.Get("Transformation", Constants.FxtNs));

            XElement validationSettings = rootElement.Element(XName.Get("Validation", Constants.TypedXLinqNs));
            if (validationSettings != null)
            {
                verifyRequired =
                    (string)validationSettings.Element(XName.Get("VerifyRequired", Constants.TypedXLinqNs)) == "true";
            }
        }

        public string GetClrNamespace(string xmlNamespace)
        {
            string clrNamespace = string.Empty;
            if (xmlNamespace == null)
            {
                return clrNamespace;
            }

            if (namespaceMapping.TryGetValue(xmlNamespace, out clrNamespace))
            {
                return clrNamespace;
            }

            clrNamespace = NameGenerator.MakeValidCLRNamespace(
                xmlNamespace, this.NameMangler2);
            namespaceMapping.Add(xmlNamespace, clrNamespace);
            return clrNamespace;
        }

        public bool SplitFilesByNamespace { get; set; }

        public bool VerifyRequired
        {
            get { return verifyRequired; }
        }

        public bool EnableServiceReference
        {
            get { return enableServiceReference; }
            set { enableServiceReference = value; }
        }

        public bool UseDateOnly { get; set; }
        public bool UseTimeOnly { get; set; }
        public bool UseDateTimeOffset { get; set; }

        public bool NullableReferences { get; set; }

        private void GenerateNamespaceMapping(XElement namespaces)
        {
            if (namespaces == null) return;
            foreach (XElement ns in namespaces.Elements(XName.Get("Namespace", Constants.TypedXLinqNs))) {
                var schemaXName = XName.Get("Schema");
                var schema = (string)ns.Attribute(schemaXName);
                if (schema == null) {
                    ns.SetAttributeValue(schemaXName, string.Empty);
                    schema = string.Empty;
                }
                var clr = (string) ns.Attribute(XName.Get("Clr"));
                namespaceMapping.Add(schema, clr);
            }
        }

        private void GenerateNamespaceVisibilityMapping(XElement namespaces)
        {
            if (namespaces == null) return;
            foreach (var ns in namespaces.Elements(XName.Get("Namespace", Constants.TypedXLinqNs))) {
                var clrNs = (string) ns.Attribute(XName.Get("Clr"));
                var visibilityValue = (string) ns.Attribute(XName.Get("DefaultVisibility"));
                var visibility = visibilityValue == "internal"
                    ? GeneratedTypesVisibility.Internal
                    : GeneratedTypesVisibility.Public;

                NamespaceTypesVisibilityMap.Add(clrNs, visibility);
            }
        }

        private void GenerateNamespaceFileMapping(XElement namespaces)
        {
            if (namespaces == null) return;
            foreach (var ns in namespaces.Elements(XName.Get("Namespace", Constants.TypedXLinqNs)))
            {
                var file = ns.Attribute(XName.Get("File"))?.Value;
                if (file == null) continue;
                var clrNs = ns.Attribute(XName.Get("Clr"))?.Value;
                NamespaceFileMap.Add(clrNs, file);
            }
        }
    }
}