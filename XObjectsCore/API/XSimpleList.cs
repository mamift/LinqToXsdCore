using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public class XSimpleList<T> : XList<T>
    {
        XmlSchemaDatatype schemaDatatype;
#nullable enable
        public IList<T>? DefaultValues { get; private set; }
#nullable disable

        public XSimpleList(XTypedElement container, XmlSchemaDatatype dataType, XName itemXName) : base(container,
            itemXName)
        {
            this.schemaDatatype = dataType;
        }

        public XSimpleList(XTypedElement container, XmlSchemaDatatype dataType, XName itemXName, IList<T> defaultValues) : this(container, dataType, itemXName)
        {
            this.DefaultValues = defaultValues;
            if (defaultValues != null && !EnumerateElements().Any())
            {
                InitializeFrom(defaultValues, defaultValues);
            }
        }

        protected override void AddImpl(T value)
        {
            container.SetElement(itemXName, value, true, schemaDatatype);
        }

        protected override bool IsEqual(XElement element, T value)
        {
            string stringValue = element.Value;
            return schemaDatatype.ChangeType(stringValue, typeof(T)).Equals(value);
        }

        protected override XElement ElementForImpl(T value, bool createNew)
        {
            return createNew
                ? new XElement(itemXName, XTypedServices.GetXmlString(value, schemaDatatype, containerElement))
                : EnumerateElements().FirstOrDefault(x => IsEqual(x, value));
        }

        protected override T ValueOfImpl(XElement element)
        {
            string stringValue = element.Value;
            return (T) schemaDatatype.ChangeType(stringValue, typeof(T));
        }

        protected override void UpdateElementImpl(XElement oldElement, T value)
        {
            oldElement.Value = XTypedServices.GetXmlString(value, schemaDatatype, oldElement);
        }

        public static XSimpleList<T> CopyFromWithValidation(
            IEnumerable<T> values, 
            XTypedElement container,
            XName itemXName, 
            XmlSchemaDatatype dataType, 
            string propertyName = null, 
            SimpleTypeValidator typeDef = null, 
            bool supportsXsiNil = false)
        {
            var simpleList = new XSimpleList<T>(container, dataType, itemXName) { SupportsXsiNil = supportsXsiNil };
            simpleList.InitializeFrom(values);
            return simpleList;            
        }

        public static XSimpleList<T> InitializeNillable(
            XTypedElement container, 
            XmlSchemaDatatype dataType,
            IEnumerable<T> values,
            XName itemXName)
        {
            var simpleList = new XSimpleList<T>(container, dataType, itemXName) { SupportsXsiNil = true };
            simpleList.InitializeFrom(values);
            return simpleList;
        }

        public static XSimpleList<T> Initialize(
            XTypedElement container, 
            XmlSchemaDatatype dataType,
            IEnumerable<T> values, 
            XName itemXName)
        {
            var simpleList = new XSimpleList<T>(container, dataType, itemXName);
            simpleList.InitializeFrom(values);
            return simpleList;
        }

#nullable enable
        public static XSimpleList<T> Initialize(
            XTypedElement container, 
            XmlSchemaDatatype dataType,
            IEnumerable<T> values, 
            XName itemXName, IList<T> defaultValues)
        {
            XSimpleList<T> simpleList = new(container, dataType, itemXName, defaultValues);
            simpleList.InitializeFrom(values, defaultValues);
            return simpleList;
        }
    }
}