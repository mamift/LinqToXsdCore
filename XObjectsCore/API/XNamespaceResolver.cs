using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
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
}