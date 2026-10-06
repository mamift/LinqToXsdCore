#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace Xml.Schema.Linq.CodeGen;

public class PreloadedXsdsResolver: XmlUrlResolver
{
    private readonly Assembly _assembly = Assembly.GetAssembly(typeof(PreloadedXsdsResolver))!;

    public static class XmlXsdUrls
    {
        public static readonly string XmlXsd2009_01 = "http://www.w3.org/2009/01/xml.xsd";
        public static readonly string XmlXsd2007_08 = "http://www.w3.org/2007/08/xml.xsd";
        public static readonly string XmlXsd2004_10 = "http://www.w3.org/2004/10/xml.xsd";
        public static readonly string XmlXsd2001_03 = "http://www.w3.org/2001/03/xml.xsd";
        public static readonly string XmlXsd2001 = "http://www.w3.org/2001/xml.xsd";
        public static readonly string XmlXsdDtd2001 = "http://www.w3.org/2001/03/XMLSchema.dtd";
        public static readonly string XmlXsdDataTypesDtd2001 = "http://www.w3.org/2001/03/datatypes";

        /// <summary>
        /// From: <see cref="https://www.w3.org/2001/xml.xsd"/> (view in a web browser).
        /// </summary>
        public static readonly string[] All = [
            XmlXsd2001, XmlXsdDtd2001, XmlXsdDataTypesDtd2001,
            XmlXsd2001, XmlXsd2001_03, XmlXsd2004_10, XmlXsd2007_08, XmlXsd2009_01
        ];
    }

    public static class EmbeddedXsdFileNames
    {
        public static readonly string _2001XmlSchemaDataTypesDtd = "2001.datatypes.dtd";
        public static readonly string _2001XmlSchemaDtd = "2001.XMLSchema.dtd";
        public static readonly string _2001XmlXsd = "2001-03.xml.xsd";
        public static readonly string _2004XmlXsd = "2004-10.xml.xsd";
        public static readonly string _2007XmlXsd = "2007-08.xml.xsd";
        public static readonly string _2009XmlXsd = "2009-01.xml.xsd";

        public static readonly string XmlXsdConfig = "xml.xsd.config";

        public static readonly string[] All = [
            _2001XmlSchemaDataTypesDtd, _2001XmlSchemaDtd, _2001XmlXsd, _2004XmlXsd, _2007XmlXsd, _2009XmlXsd
        ];
    }

    protected Stream? GetStreamForFileName(string filename)
    {
        string[] names = _assembly.GetManifestResourceNames();
        if (names.Length == 0) return null;

        string? xmlXsd = Array.Find(names, x => x.EndsWith(filename));
        if (xmlXsd == null) return null;
        PreloadedXsdFileNames.Add(filename);
        Stream? stream = _assembly.GetManifestResourceStream(xmlXsd);
        return stream;
    }

    public override object? GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        var originalString = absoluteUri.OriginalString;
        Stream? stream = null;
        if (originalString == XmlXsdUrls.XmlXsd2009_01)
        {
            stream = GetStreamForFileName("2009-01.xml.xsd");
        }
        else if (originalString == XmlXsdUrls.XmlXsd2007_08)
        {
            stream = GetStreamForFileName("2007-08.xml.xsd");
        }
        else if (originalString == XmlXsdUrls.XmlXsd2004_10)
        {
            stream = GetStreamForFileName("2004-10.xml.xsd");
        }
        else if (originalString == XmlXsdUrls.XmlXsd2001_03)
        {
            stream = GetStreamForFileName("2001-03.xml.xsd");
        }
        else if (originalString == XmlXsdUrls.XmlXsdDtd2001)
        {
            stream = GetStreamForFileName("2001.XMLSchema.dtd");
        }
        else if (originalString.Contains(XmlXsdUrls.XmlXsdDataTypesDtd2001))
        {
            stream = GetStreamForFileName("2001.datatypes.dtd");
        }

        if (stream != null)
        {
            return stream;
        }

        return base.GetEntity(absoluteUri, role, ofObjectToReturn);
    }

    public List<string> PreloadedXsdFileNames { get; private set; } = new();

    public override Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        return base.GetEntityAsync(absoluteUri, role, ofObjectToReturn);
    }

    public override Uri ResolveUri(Uri baseUri, string relativeUri)
    {
        return base.ResolveUri(baseUri, relativeUri);
    }

    public override bool SupportsType(Uri absoluteUri, Type type)
    {
        return base.SupportsType(absoluteUri, type);
    }
}