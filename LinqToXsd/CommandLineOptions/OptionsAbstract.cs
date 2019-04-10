using System.Collections.Generic;
using System.Linq;
using System.Xml;
using CommandLine;

namespace LinqToXsd
{
    internal abstract class OptionsAbstract
    {
        private List<string> schemaFiles;

        /// <summary>
        /// CLI argument: one more schema files.
        /// </summary>
        [Value(1, HelpText = "One or more schema files.", Required = true)]
        public IEnumerable<string> SchemaFiles
        {
            get => schemaFiles;
            set => schemaFiles = value?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Returns <see cref="XmlReader"/> instances of every file specified in <see cref="SchemaFiles"/>.
        /// </summary>
        public IEnumerable<XmlReader> SchemaReaders
        {
            get {
                if (!SchemaFiles.Any()) return new XmlReader[0];
                var xmlReaderSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse
                };
                return SchemaFiles.Select(sf => XmlReader.Create(sf, xmlReaderSettings));
            }
        }
    }
}
