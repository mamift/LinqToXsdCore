using System;
using System.IO;
using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class FileExtensions
{
    public static StreamReader ToStreamReader(this IFileInfo fileInfo)
    {
        return new StreamReader(fileInfo.OpenRead());
    }

    public static XDocument ToXDocument(this IFileInfo fileInfo)
    {
        return XDocument.Load(fileInfo.ToStreamReader());
    }
    
    public static XmlSchemaSet ReadAsXmlSchemaSet(this IFileInfo fileInfo, XmlResolver resolver)
    {
        if (resolver == null) throw new ArgumentNullException(nameof(resolver));
        
        using var sr = new StreamReader(fileInfo.OpenRead());
        XmlReaderSettings defaultXmlReaderSettings = Defaults.DefaultXmlReaderSettings;
        defaultXmlReaderSettings.XmlResolver = resolver;
        var reader = XmlReader.Create(sr, defaultXmlReaderSettings);
        var xsd = reader.ToXmlSchemaSet(resolver);

        return xsd;
    }

    public static XmlSchema ReadAsXmlSchema(this IFileInfo fileInfo)
    {
        using var sr = new StreamReader(fileInfo.OpenRead());
        var reader = XmlReader.Create(sr, Defaults.DefaultXmlReaderSettings);
        var xsd = reader.ToXmlSchema();

        return xsd;
    }
}
