using System.IO;
using System.IO.Abstractions;
using System.Xml.Linq;

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
}