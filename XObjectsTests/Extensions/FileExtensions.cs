using System.IO;
using System.IO.Abstractions;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class FileExtensions
{
    public static StreamReader ToStreamReader(this IFileInfo fileInfo)
    {
        return new StreamReader(fileInfo.OpenRead());
    }
}