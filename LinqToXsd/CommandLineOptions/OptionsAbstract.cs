using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using CommandLine;

namespace LinqToXsd
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
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
        public Dictionary<string, XmlReader> SchemaReaders
        {
            get
            {
                if (!SchemaFiles.Any()) return new Dictionary<string, XmlReader>();
                var xmlReaderSettings = new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Parse
                };
                return SchemaFiles.ToDictionary(f => f, f => XmlReader.Create(f, xmlReaderSettings));
            }
        }

        public abstract string Output { get; set; }
    }
}
