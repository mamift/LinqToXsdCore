using System.Collections.Generic;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    public class XTypedList<T> : XList<T> where T : XTypedElement
    {
        ILinqToXsdTypeManager typeManager;

        public XTypedList(XTypedElement container, XName itemXName) : this(container, null, itemXName) { }

        public XTypedList(XTypedElement container, ILinqToXsdTypeManager typeManager, XName itemXName) : base(container,
            itemXName)
        {
            this.typeManager = typeManager;
        }

        public static XTypedList<T> CopyFromWithValidation(IEnumerable<T> typedObjects, XTypedElement container,
            XName itemXName, ILinqToXsdTypeManager typeManager, string propertyName, SimpleTypeValidator typeDef)
        {
            return Initialize(container, typeManager, typedObjects, itemXName);
        }

        public static XTypedList<T> Initialize(XTypedElement container, ILinqToXsdTypeManager typeManager,
            IEnumerable<T> typedObjects, XName itemXName)
        {
            XTypedList<T> typedList = new XTypedList<T>(container, typeManager, itemXName);
            typedList.Clear();
            foreach (T typedItem in typedObjects)
            {
                typedList.Add(typedItem);
            }

            return typedList;
        }

        public override void Add(T value)
        {
            container.SetElement(itemXName, value, true, null);
        }

        protected override bool IsEqual(XElement element, T value)
        {
            XElement newElement = value.Untyped;
            return element.Equals(newElement);
        }

        protected override XElement ElementForImpl(T value, bool createNew)
        {
            XElement element = value.Untyped;
            element.Name = itemXName;
            return element;
        }

        protected override T ValueOfImpl(XElement element)
        {
            return XTypedServices.ToXTypedElement<T>(element, typeManager);
        }

        protected override void UpdateElementImpl(XElement oldElement, T value)
        {
            oldElement.AddBeforeSelf(ElementForImpl(value, true));
            oldElement.Remove();
        }
    }
}