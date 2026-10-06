using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace Xml.Schema.Linq.CodeGen;

public static class XmlReaderExtensions
{
    public static XmlSchemaSet CompileXmlSchemaSetWithPreloadedXsds(this XmlReader reader, out List<string> preloadedFileNames)
    {
        var xmlResolver = new PreloadedXsdsResolver();
        var newXmlSet = new XmlSchemaSet {
            XmlResolver = xmlResolver
        };

        newXmlSet.Add(null, reader);
        newXmlSet.Compile();

        preloadedFileNames = xmlResolver.PreloadedXsdFileNames;

        return newXmlSet;
    }

}