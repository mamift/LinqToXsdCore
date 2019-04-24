using System.IO;

namespace Xml.Schema.Linq.Extensions
{
    public static class StreamExtensionMethods
    {
        public static string ReadAsString(this Stream stream)
        {
            var reader = new StreamReader(stream, true);

            return reader.ReadToEnd();
        }
    }
}
