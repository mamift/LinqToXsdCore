using System;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public class NamedContentModelEntity : ContentModelEntity
    {
        internal XName name;
        int elementPosition = -1;

        public NamedContentModelEntity(XName name)
        {
            this.name = name;
        }

        public override XElement AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype, Type elementBaseType)
        {
            throw new InvalidOperationException();
        }

        internal XName Name
        {
            get { return name; }
        }

        internal int ElementPosition
        {
            get { return elementPosition; }
            set { elementPosition = value; }
        }
    }
}