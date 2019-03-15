//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml.Linq;
using System.Collections.Generic;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq
{
    public class LinqToXsdSettings
    {
        Dictionary<string, string> namespaceMapping;
        internal XElement trafo;
        bool verifyRequired = false;
        bool enableServiceReference = false;
        readonly bool NameMangler2;
        
        public LinqToXsdSettings(bool nameMangler2 = false)
        {
            this.NameMangler2 = nameMangler2;
            namespaceMapping = new Dictionary<string, string>();
        }

        public void Load(string configFile)
        {
            if (configFile == null || configFile.Length == 0)
            {
                throw new ArgumentException("Argument configFile should be non-null and non-empty.");
            }

            XDocument configDoc = XDocument.Load(configFile);
            XElement rootElement = configDoc.Root;
            GenerateNamespaceMapping(rootElement.Element(XName.Get("Namespaces", Constants.TypedXLinqNs)));
            trafo = rootElement.Element(XName.Get("Transformation", Constants.FxtNs));
            XElement validationSettings = rootElement.Element(XName.Get("Validation", Constants.TypedXLinqNs));
            if (validationSettings != null)
            {
                verifyRequired =
                    (string) validationSettings.Element(XName.Get("VerifyRequired", Constants.TypedXLinqNs)) == "true"
                        ? true
                        : false;
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

        public bool VerifyRequired
        {
            get { return verifyRequired; }
        }

        public bool EnableServiceReference
        {
            get { return enableServiceReference; }
            set { enableServiceReference = value; }
        }

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
    }
}