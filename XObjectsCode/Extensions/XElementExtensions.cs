using System.Linq;
using System.Xml.Linq;

namespace Xml.Schema.Linq.CodeGen;

public static class XElementExtensions
{
    public static XComment ToXComment(this XElement element, bool includeNamespace = false)
    {
        if (includeNamespace) {
            return new XComment(element.ToString(SaveOptions.DisableFormatting));
        }

        var strippedOfNamespace = RemoveAllNamespaces(element);

        return new XComment(strippedOfNamespace.ToString(SaveOptions.DisableFormatting));
    }

    /// <summary>
    /// https://stackoverflow.com/a/988325/1376318
    /// </summary>
    /// <param name="xmlDocument"></param>
    /// <returns></returns>
    public static string RemoveAllNamespaces(string xmlDocument)
    {
        var xElement = XElement.Parse(xmlDocument);
        XElement xmlDocumentWithoutNs = RemoveAllNamespaces(xElement);

        return xmlDocumentWithoutNs.ToString();
    }

    /// <summary>
    /// https://stackoverflow.com/a/988325/1376318
    /// </summary>
    /// <param name="xmlDocument"></param>
    /// <returns></returns>
    public static XElement RemoveAllNamespaces(XElement xmlDocument)
    {
        if (!xmlDocument.HasElements) {
            XElement xElement = new XElement(xmlDocument.Name.LocalName);
            xElement.Value = xmlDocument.Value;

            foreach (XAttribute attribute in xmlDocument.Attributes())
                xElement.Add(attribute);

            return xElement;
        }

        return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
    }
}