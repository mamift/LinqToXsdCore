using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Resolvers;

namespace Xml.Schema.Linq.Tests;

internal class MockXmlUrlResolver : XmlResolver
{
    private readonly IMockFileDataAccessor fs;

    public MockXmlUrlResolver(IMockFileDataAccessor fs)
    {
        this.fs = fs;
    }

    public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        if (absoluteUri == null) throw new ArgumentNullException(nameof (absoluteUri));

        return new MockFileInfo(this.fs, absoluteUri.OriginalString).OpenRead();
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
        var search = fs.AllFiles.Where(f => f.EndsWith(justTheFileName, StringComparison.CurrentCultureIgnoreCase));
        var theFile = search.FirstOrDefault();

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

    }
}