using System;
using System.Xml;
using System.Xml.Schema;

namespace Xml.Schema.Linq.CodeGen;

public static class XmlReaderExtensions
{
    public static XmlSchemaSet CompileXmlSchemaSetWithPreloadedXsds(this XmlReader reader)
    {
        var xmlResolver = new PreloadedXsdsResolver();
        var newXmlSet = new XmlSchemaSet {
            XmlResolver = xmlResolver
        };

        newXmlSet.Add(null, reader);
        newXmlSet.Compile();

        return newXmlSet;
    }

}