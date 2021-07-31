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
        public Dictionary<string, GeneratedTypesVisibility> NamespaceTypesVisibilityMap { get; } = new Dictionary<string, GeneratedTypesVisibility>();
        internal XElement trafo;
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
            GenerateCodeGenMapping(rootElement.Element(XName.Get(nameof(CodeGeneration))));
            XElement nullableSettings = rootElement.Element(XName.Get("NullableReferences", Constants.TypedXLinqNs));
            NullableReferences = nullableSettings?.Value == "true";
            trafo = rootElement.Element(XName.Get("Transformation", Constants.FxtNs));
            XElement validationSettings = rootElement.Element(XName.Get("Validation", Constants.TypedXLinqNs));
            if (validationSettings != null)
            {
                VerifyRequired =
                    (string)validationSettings.Element(XName.Get("VerifyRequired", Constants.TypedXLinqNs)) == "true";
            }
        }

        private void GenerateCodeGenMapping(XElement codeGenElement)
        {
            if (codeGenElement == null) return;
            var splitCodeFilesElement = codeGenElement.Element(XName.Get(nameof(SplitCodeFiles)));
            if (splitCodeFilesElement == null) return;
            var byAttr = splitCodeFilesElement.Attribute(XName.Get(nameof(SplitCodeFiles.By)));

            if (byAttr?.Value == "Class") SplitCodeGenByClasses = true;
            if (byAttr?.Value == "Namespace") SplitCodeGenByClasses = true;

            if (SplitCodeGenByClasses && SplitCodeGenByNamespaces)
                throw new LinqToXsdException("You cannot split generated code files by both namespaces and classes!");
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

        public bool VerifyRequired { get; private set; } = false;

        public bool EnableServiceReference { get; set; } = false;

        public bool NullableReferences { get; set; }

        public bool SplitCodeGenByNamespaces { get; set; }
        
        public bool SplitCodeGenByClasses { get; set; }

        private void GenerateNamespaceMapping(XElement namespaces)
        {
            if (namespaces == null)
                return;
            foreach (XElement ns in namespaces.Elements(XName.Get("Namespace", Constants.TypedXLinqNs)))
            {
                namespaceMapping.Add((string) ns.Attribute(XName.Get("Schema")),
                    (string) ns.Attribute(XName.Get("Clr")));
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
    }
}