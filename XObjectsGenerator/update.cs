namespace XObjectsGenerator
{
    using S = global::System;
    using IO = global::System.IO;
    using T = global::System.Text;

    internal class Update: S.IDisposable
    {
        private readonly IO.MemoryStream stream = new IO.MemoryStream();

        private readonly string filename;

        private readonly T.Encoding encoding;

        public readonly IO.TextWriter Writer;

        public Update(string filename, T.Encoding encoding)
        {
            this.filename = filename;
            this.encoding = encoding;
            this.Writer = new IO.StreamWriter(this.stream, encoding);
        }

        public bool Close()
        {
            this.Writer.Close();
            var memoryString = new IO.StreamReader(
                    new IO.MemoryStream(this.stream.ToArray()), this.encoding).
                ReadToEnd();
            var fileString = "";
            using (var file = new IO.FileStream(
                this.filename, IO.FileMode.OpenOrCreate))
            {
                using (var fileReader = new IO.StreamReader(file))
                {
                    fileString = fileReader.ReadToEnd();
                }
            }
            if (memoryString != fileString)
            {
                using (
                    var file = 
                        new IO.FileStream(this.filename, IO.FileMode.Create))
                {
                    using (
                        var fileWriter = 
                            new IO.StreamWriter(file, this.encoding))
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
