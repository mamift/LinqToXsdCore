using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xml.Schema.Linq;

namespace LinqToXsd
{
    public static class XDocumentExtensions
    {
        private const string W3CXmlSchemaNamespaceUri = "http://www.w3.org/2001/XMLSchema";
        private static readonly XName IncludeXName = XName.Get("include", W3CXmlSchemaNamespaceUri);
        private static readonly XName ImportXName = XName.Get("import", W3CXmlSchemaNamespaceUri);

        /// <summary>
        /// Determines if the current <see cref="XDocument"/> is a W3C Xml Schema by checking for the presence of the
        /// W3C namespace URI in the root element.
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        public static bool IsAnXmlSchema(this XDocument xDoc)
        {
            if (xDoc?.Root == null) throw new ArgumentNullException(nameof(xDoc));

            return xDoc.Root.Name.LocalName == "schema" && xDoc.Root.Name.Namespace == W3CXmlSchemaNamespaceUri;
        }

        /// <summary>
        /// From an existing collection of <see cref="XDocument"/>s, filter out the ones that are themselves referenced in xs:include or xs:import
        /// directives from within other XML documents in the same collection. This ensures that they are not referenced twice.
        /// </summary>
        /// <remarks>This extension operates on a dictionary collection to ensure that the file name remains associated with it's relevant XDocument
        /// instance, as XDocuments do not contain any information about the file name or where the XML document was/is stored.</remarks>
        /// <param name="xDocs"></param>
        /// <returns></returns>
        public static Dictionary<string, XDocument> FilterOutSchemasThatAreIncludedOrImported(this Dictionary<string, XDocument> xDocs)
        {
            var actualSchemas = xDocs.Where(kvp => kvp.Value.IsAnXmlSchema()).ToList();
            var allImportReferences = actualSchemas.SelectMany(kvp => kvp.Value.Descendants(ImportXName));
            var allIncludeReferences = actualSchemas.SelectMany(kvp => kvp.Value.Descendants(IncludeXName));

            var importAndIncludeElements = allIncludeReferences.Union(allImportReferences).ToList();
            var schemaLocationXName = XName.Get("schemaLocation");

            var filesReferredToInImportAndIncludeElements = importAndIncludeElements
                                                            .SelectMany(iie => iie.Attributes(schemaLocationXName))
                                                            .Distinct(new XAttributeValueEqualityComparer())
                                                            .Select(attr => attr.Value);

            var theXDocsReferencedByImportOrInclude = from xDoc in xDocs
                                                      where filesReferredToInImportAndIncludeElements.Any(f =>
                                                          string.Equals(f, Path.GetFileName(xDoc.Key), StringComparison.InvariantCultureIgnoreCase))
                                                      select xDoc;

            return theXDocsReferencedByImportOrInclude.ToDictionary(key => key.Key, kvp => kvp.Value);
        }
    }
}
