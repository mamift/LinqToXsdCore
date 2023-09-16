using System.Xml;

namespace Xml.Schema.Linq;

public static class Defaults
{
    public static readonly XmlReaderSettings DefaultXmlReaderSettings = new XmlReaderSettings() {
        DtdProcessing = DtdProcessing.Parse,
        CloseInput = true
    };
}