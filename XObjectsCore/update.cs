using System.IO;
using System.Text;
using System;

namespace XObjectsGenerator
{
    public class Update: IDisposable
    {
        private readonly MemoryStream stream = new MemoryStream();

        private readonly string filename;

        private readonly Encoding encoding;

        public readonly TextWriter Writer;

        public Update(string filename, Encoding encoding)
        {
            this.filename = filename;
            this.encoding = encoding;
            this.Writer = new StreamWriter(this.stream, encoding);
        }

        public bool Close()
        {
            this.Writer.Close();
            var memoryString = new StreamReader(
                    new MemoryStream(this.stream.ToArray()), this.encoding).
                ReadToEnd();
            var fileString = "";
            using (var file = new FileStream(
                this.filename, FileMode.OpenOrCreate))
            {
                using (var fileReader = new StreamReader(file))
                {
                    fileString = fileReader.ReadToEnd();
                }
            }
            if (memoryString != fileString)
            {
                using (
                    var file = 
                        new FileStream(this.filename, FileMode.Create))
                {
                    using (
                        var fileWriter = 
                            new StreamWriter(file, this.encoding))
                    {
                        fileWriter.Write(memoryString);
                        file.SetLength(file.Position);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            this.Close();
        }

        #endregion
    }
}
