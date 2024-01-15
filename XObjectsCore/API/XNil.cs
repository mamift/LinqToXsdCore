using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public static class XNil
    {
        public static readonly XName Name = XName.Get("nil", XmlSchema.InstanceNamespace);

        // Well-known value that can be passed to AddElementToParent() to set
        // <Element xsi:nil="true">, regardless of actual datatype.
        public static readonly object Value = new();

        public static XElement Element(XName elementName) => new XElement(elementName, new XAttribute(Name, "true"));

        public static bool IsXsiNil(this XElement element) => element?.Attribute(Name)?.Value == "true";

        public static void SetXsiNil(this XElement element)
        {
            element.RemoveAll();
            element.Add(new XAttribute(Name, "true"));
        }

        public static void RemoveXsiNil(this XElement element) => element.Attribute(Name)?.Remove();
    }
}
