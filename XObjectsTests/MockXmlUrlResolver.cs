using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests;

internal class ReflexiveXmlSchemaSet : XmlSchemaSet
{

}

internal class MockXmlUrlResolver : XmlResolver
{
    private readonly IMockFileDataAccessor fs;
    private readonly Dictionary<Uri, IFileInfo> mappings = new();

    public MockXmlUrlResolver(IMockFileDataAccessor fs)
    {
        this.fs = fs;
    }

    public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        if (absoluteUri == null) throw new ArgumentNullException(nameof (absoluteUri));

        var mapping = mappings.ValueForKey(absoluteUri);

        if (mapping == null) {
            var mockFileInfo = new MockFileInfo(this.fs, absoluteUri.OriginalString);
            var fileSystemStream = mockFileInfo.OpenRead();
            return fileSystemStream;
        }

        return mapping.OpenRead();
    }

    public override Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        return base.GetEntityAsync(absoluteUri, role, ofObjectToReturn);
    }

    public override Uri ResolveUri(Uri baseUri, string relativeUri)
    {
        var str = baseUri.ToString();
        var justTheFileName = Path.GetFileName(relativeUri);
        var fsSearch = fs.AllFiles.Where(f => f.EndsWith(justTheFileName, StringComparison.CurrentCultureIgnoreCase));
        var mappingsSearch = mappings.Where(k => k.Key.OriginalString.EndsWith(relativeUri));
        var possibleMappingResult = mappingsSearch.FirstOrDefault();
        var theFile = EqualityComparer<KeyValuePair<Uri, IFileInfo>>.Default.Equals(possibleMappingResult, default) 
            ? null : possibleMappingResult.Key;

        if (theFile == null) {
            var fsResult = fsSearch.FirstOrDefault();
            if (fsResult != null) {
                theFile = new Uri(fsResult);
            }
        }

        if (theFile != null) {
            return theFile;
        }

        throw new FileNotFoundException();
    }

    public override bool SupportsType(Uri absoluteUri, Type type)
    {
        return base.SupportsType(absoluteUri, type);
    }

    public void Add(Uri uri, IFileInfo file) => mappings[uri] = file;
}