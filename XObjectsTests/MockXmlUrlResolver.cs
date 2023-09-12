using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Resolvers;
using add = LinqToXsd.Schemas.NameMangledTest.add;

namespace Xml.Schema.Linq.Tests;

internal class MockXmlUrlResolver : XmlResolver
{
    private readonly IMockFileDataAccessor fs;
    private readonly Dictionary<Uri, XDocument> _mappings = new Dictionary<Uri, XDocument>();

    public MockXmlUrlResolver(IMockFileDataAccessor fs)
    {
        this.fs = fs;
    }

    public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        if (absoluteUri == null) throw new ArgumentNullException(nameof (absoluteUri));

        var mockFileInfo = new MockFileInfo(this.fs, absoluteUri.OriginalString);
        var fileSystemStream = mockFileInfo.OpenRead();
        return fileSystemStream;
    }

    public override Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        return base.GetEntityAsync(absoluteUri, role, ofObjectToReturn);
    }

    public override Uri ResolveUri(Uri baseUri, string relativeUri)
    {
        var oBaseUri = base.ResolveUri(baseUri, relativeUri);
        var str = baseUri.ToString();
        var justTheFileName = Path.GetFileName(relativeUri);
        var fsSearch = fs.AllFiles.Where(f => f.EndsWith(justTheFileName, StringComparison.CurrentCultureIgnoreCase));
        var mappingsSearch = _mappings.Where(k => k.Key == baseUri || k.Key.Segments.Contains(relativeUri));
        var theFile = fsSearch.FirstOrDefault();

        var exists = theFile != null;
                 
        if (exists) {
            return new Uri(theFile);
        }

        throw new FileNotFoundException();
    }

    public override bool SupportsType(Uri absoluteUri, Type type)
    {
        return base.SupportsType(absoluteUri, type);
    }

    public void Add(Uri uri, Stream stream)
    {
        using (stream) {
            var streamAsXdoc = XDocument.Load(stream);
            this.Add(uri, streamAsXdoc);
        }
    }

    private void Add(Uri uri, XDocument data)
    {
        if (this._mappings.ContainsKey(uri))
            this._mappings[uri] = data;
        else
            this._mappings.Add(uri, data);
    }
}