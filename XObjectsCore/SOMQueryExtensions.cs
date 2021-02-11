using System.Xml.Schema;

namespace Xml.Schema.Linq 
{
    internal static class SOMQueryExtensions
    {
        public static XmlSchemaWhiteSpace GetBuiltInWSFacet(this XmlSchemaDatatype dt)
        {
            if (dt.TypeCode == XmlTypeCode.NormalizedString)
            {
                return XmlSchemaWhiteSpace.Replace;
            }
            else if (dt.TypeCode == XmlTypeCode.String)
            {
                return XmlSchemaWhiteSpace.Preserve;
            }
            else
                return XmlSchemaWhiteSpace.Collapse;
        }
    }
}