using System.IO;

namespace LinqToXsd
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
