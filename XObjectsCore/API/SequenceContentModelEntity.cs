using System;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public class SequenceContentModelEntity : SchemaAwareContentModelEntity
    {
        public SequenceContentModelEntity(params ContentModelEntity[] items) : base(items) { }

        internal override ContentModelType ContentModelType
        {
            get { return ContentModelType.Sequence; }
        }
        public override XElement AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting, XmlSchemaDatatype datatype, Type elementBaseType)
        {
            var element = base.AddElementToParent(name, value, parentElement, addToExisting, datatype, elementBaseType);
            base.OnElementAdded(this, element, parentElement);
            return element;
        }
    }
}