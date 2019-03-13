//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading;

namespace Xml.Schema.Linq
{
    public interface ILinqToXsdTypeManager
    {
        Dictionary<XName, Type> GlobalTypeDictionary { get; }
        Dictionary<XName, Type> GlobalElementDictionary { get; }
        Dictionary<Type, Type> RootContentTypeMapping { get; }
        XmlSchemaSet Schemas { get; set; }
    }

    public interface IXMetaData
    {
        XName SchemaName { get; }
        ILinqToXsdTypeManager TypeManager { get; }
        SchemaOrigin TypeOrigin { get; }
        Dictionary<XName, System.Type> LocalElementsDictionary { get; }
        XTypedElement Content { get; }
        ContentModelEntity GetContentModel();
        FSM GetValidationStates();
    }

    public interface IXTyped
    {
        IEnumerable<T> Descendants<T>() where T : XTypedElement, new();
        IEnumerable<T> Ancestors<T>() where T : XTypedElement;
        IEnumerable<T> SelfAndDescendants<T>() where T : XTypedElement, new();
        IEnumerable<T> SelfAndAncestors<T>() where T : XTypedElement;
    }


    public class XNamespaceResolver : IXmlNamespaceResolver
    {
        XElement element;

        public XNamespaceResolver(XElement element)
        {
            this.element = element;
        }

        public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            switch (scope)
            {
                case XmlNamespaceScope.All:
                    return GetNamespaceDecls(null, false);

                case XmlNamespaceScope.Local:
                    return GetNamespaceDecls(element.Parent, false);

                case XmlNamespaceScope.ExcludeXml:
                    return GetNamespaceDecls(null, true);
            }

            return null;
        }

        public string LookupNamespace(string prefix)
        {
            Debug.Assert(prefix != null);
            if (prefix.Length == 0)
            {
                return element.GetDefaultNamespace().NamespaceName;
            }

            return element.GetNamespaceOfPrefix(prefix).NamespaceName;
        }

        public string LookupPrefix(string namespaceName)
        {
            Debug.Assert(namespaceName != null);
            return element.GetPrefixOfNamespace(namespaceName);
        }

        Dictionary<string, string> GetNamespaceDecls(XElement outOfScope, bool excludeXml)
        {
            Dictionary<string, string> namespaceDecls = new Dictionary<string, string>();
            do
            {
                IEnumerable<XAttribute> attributes = element.Attributes();
                foreach (XAttribute att in attributes)
                {
                    if (att.IsNamespaceDeclaration)
                    {
                        string prefix = att.Name.LocalName;
                        if (prefix == "xmlns")
                        {
                            prefix = string.Empty;
                        }

                        if (!namespaceDecls.ContainsKey(prefix))
                        {
                            namespaceDecls.Add(prefix, att.Value);
                        }
                    }
                }

                element = element.Parent;
            } while (element != outOfScope);

            if (!excludeXml)
            {
                if (!namespaceDecls.ContainsKey("xml"))
                {
                    namespaceDecls.Add("xml", XNamespace.Xml.NamespaceName);
                }
            }

            return namespaceDecls;
        }
    }

    internal class TypeManager : ILinqToXsdTypeManager
    {
        private static XmlSchemaSet defaultSchemaSet = null;
        internal static TypeManager Default = new TypeManager();

        Dictionary<XName, Type> ILinqToXsdTypeManager.GlobalTypeDictionary
        {
            get { return XTypedServices.EmptyDictionary; }
        }

        Dictionary<XName, Type> ILinqToXsdTypeManager.GlobalElementDictionary
        {
            get { return XTypedServices.EmptyDictionary; }
        }

        Dictionary<Type, Type> ILinqToXsdTypeManager.RootContentTypeMapping
        {
            get { return XTypedServices.EmptyTypeMappingDictionary; }
        }

        XmlSchemaSet ILinqToXsdTypeManager.Schemas
        {
            get
            {
                if (defaultSchemaSet == null)
                {
                    XmlSchemaSet tempSet = new XmlSchemaSet();
                    Interlocked.CompareExchange<XmlSchemaSet>(ref defaultSchemaSet, tempSet, null);
                }

                return defaultSchemaSet;
            }
            set
            {
                defaultSchemaSet = value; //This operation should be atomic    
            }
        }
    }
}