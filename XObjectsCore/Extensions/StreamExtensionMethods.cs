using System.IO;
using System.Text;

namespace Xml.Schema.Linq.Extensions
{
    public static class StreamExtensionMethods
    {
        /// <summary>
        /// Reads the current <see cref="Stream"/> as a <see cref="string"/>, optionally using a given <paramref name="encoding"/>.
        /// <para>Will also reset the Stream's internal position to 0 so it can be re-read again.</para>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ReadAsString(this Stream stream, Encoding encoding = null)
        {
            var streamReader = encoding == null
                ? new StreamReader(stream, detectEncodingFromByteOrderMarks: true)
                : new StreamReader(stream, encoding);

            var @string = streamReader.ReadToEnd();
            stream.Position = 0; // reset position
            return @string;
        }
    }
}
