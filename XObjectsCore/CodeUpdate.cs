using System;
using System.IO;
using System.Text;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq
{
    public class CodeUpdate: IDisposable
    {
        private FileStream FileStream { get; }

        private Encoding Encoding { get; }

        public TextWriter TextWriter { get; private set; }

        public CodeUpdate(TextWriter writer, string outputFile, Encoding encoding = null)
        {
            TextWriter = writer ?? throw new ArgumentNullException(nameof(writer));
            if (outputFile.IsEmpty()) throw new ArgumentNullException(nameof(outputFile));
            if (encoding == null) encoding = Encoding.UTF8;
            Encoding = encoding;

            FileStream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        public void Update()
        {
            var memoryStream = new MemoryStream();
            FileStream.CopyTo(memoryStream);
            var memoryString = memoryStream.ReadAsString();

            var textWriterString = TextWriter.ToString();

            if (memoryString == textWriterString) return; // don't bother updating
            
            
        }

        public void Update(TextWriter textWriter)
        {
            TextWriter = textWriter;
            Update();
        }

        public void Dispose()
        {
            FileStream.Dispose();
            TextWriter.Dispose();
        }
    }
}