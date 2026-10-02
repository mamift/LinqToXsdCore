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
    /// <summary>
    /// From: <see cref="https://www.w3.org/2001/xml.xsd"/> (view in a web browser).
    /// </summary>
    public static readonly string[] XmlXsdUrls = [
        "http://www.w3.org/2009/01/xml.xsd", "http://www.w3.org/2007/08/xml.xsd", "http://www.w3.org/2004/10/xml.xsd",
        "http://www.w3.org/2001/03/xml.xsd", "http://www.w3.org/2001/xml.xsd"
    ];

    public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        if (Array.IndexOf(XmlXsdUrls, absoluteUri.OriginalString) > 0)
        {
            var assembly = Assembly.GetAssembly(typeof(PreloadedXsdsResolver));
            Debug.Assert(assembly != null);

            string[] names = assembly.GetManifestResourceNames();
            if (names.Length == 0) goto fallback;

            string xmlXsd = Array.Find(names, x => x.EndsWith("xml.xsd"));
            if (xmlXsd == null) goto fallback;
            PreloadedXsdFileNames.Add("xml.xsd");
            Stream stream = assembly.GetManifestResourceStream(xmlXsd);
            return stream;
        }

        fallback:
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