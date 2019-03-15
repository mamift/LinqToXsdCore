using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace Xml.Schema.Linq.Extensions
{
    internal static class GeneralExtensionMethods
    {
        /// <summary>
        /// Converts a sequence of <see cref="XmlReader"/>s to an <see cref="XmlSchemaSet"/>, assuming the readers point to XML Schema files.
        /// </summary>
        /// <param name="theReaders"></param>
        /// <returns></returns>
        public static XmlSchemaSet ToXmlSchemaSet(this IEnumerable<XmlReader> theReaders)
        {
            var newXmlSet = new XmlSchemaSet();

            foreach (var reader in theReaders)
                newXmlSet.Add(null, reader);

            return newXmlSet;
        }
    }
}
