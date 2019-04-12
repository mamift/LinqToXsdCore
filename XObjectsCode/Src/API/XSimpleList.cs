using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public class XSimpleList<T> : XList<T>
    {
        XmlSchemaDatatype schemaDatatype;

        public XSimpleList(XTypedElement container, XmlSchemaDatatype dataType, XName itemXName) : base(container,
            itemXName)
        {
            this.schemaDatatype = dataType;
        }

        public override void Add(T value)
        {
            container.SetElement(itemXName, value, true, schemaDatatype);
        }

        protected override bool IsEqual(XElement element, T value)
        {
            string stringValue = element.Value;
            if (schemaDatatype.ChangeType(stringValue, typeof(T)).Equals(value))
            {
                return true;
            }

            return false;
        }

        protected override XElement GetElementForValue(T value, bool createNew)
        {
            if (createNew)
            {
                return new XElement(itemXName, XTypedServices.GetXmlString(value, schemaDatatype, containerElement));
            }

            XElement current;
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                current = listElementsEnumerator.Current;
                if (IsEqual(current, value))
                {
                    return current;
                }
            }

            return null;
        }

        protected override T GetValueForElement(XElement element)
        {
            string stringValue = element.Value;
            return (T) schemaDatatype.ChangeType(stringValue, typeof(T));
        }

        protected override void UpdateElement(XElement oldElement, T value)
        {
            oldElement.Value = XTypedServices.GetXmlString(value, schemaDatatype, oldElement);
        }

        public static XSimpleList<T> CopyFromWithValidation(IEnumerable<T> values, XTypedElement container,
            XName itemXName, XmlSchemaDatatype dataType, string propertyName, SimpleTypeValidator typeDef)
        {
            return Initialize(container, dataType, values, itemXName);
        }

        public static XSimpleList<T> Initialize(XTypedElement container, XmlSchemaDatatype dataType,
            IEnumerable<T> values, XName itemXName)
        {
            XSimpleList<T> simpleList = new XSimpleList<T>(container, dataType, itemXName);
            simpleList.Clear();
            foreach (T value in values)
            {
                simpleList.Add(value);
            }

            return simpleList;
        }
    }
}