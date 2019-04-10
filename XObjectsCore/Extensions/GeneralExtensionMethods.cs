using System.Xml;
using System.Xml.Schema;

namespace Xml.Schema.Linq.Extensions
{
    internal static class GeneralExtensionMethods
    {
        /// <summary>
        /// Converts an <see cref="XmlReader"/>s to an <see cref="XmlSchemaSet"/>, assuming the reader points to an XML Schema file.
        /// </summary>
        /// <param name="reader">The current <see cref="XmlReader"/>.</param>
        /// <param name="resolver">Add a custom <see cref="XmlResolver"/>. Defaults to using an <see cref="XmlUrlResolver"/>.</param>
        /// <returns></returns>
        public static XmlSchemaSet ToXmlSchemaSet(this XmlReader reader, XmlResolver resolver = null)
        {
            var xmlResolver = resolver ?? new XmlUrlResolver();
            var newXmlSet = new XmlSchemaSet {
                XmlResolver = xmlResolver
            };

            newXmlSet.Add(null, reader);

            newXmlSet.Compile();

            return newXmlSet;
        }
    }
}
