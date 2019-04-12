using System.Collections.Generic;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    public class XTypedSubstitutedList<T> : XList<T> where T : XTypedElement
    {
        ILinqToXsdTypeManager typeManager;

        public XTypedSubstitutedList(XTypedElement container, ILinqToXsdTypeManager typeManager,
            params XName[] itemXNames) : base(container, itemXNames)
        {
            this.typeManager = typeManager;
        }

        public static XTypedSubstitutedList<T> Initialize(XTypedElement container, ILinqToXsdTypeManager typeManager,
            IEnumerable<T> typedObjects, params XName[] itemXNames)
        {
            XTypedSubstitutedList<T> typedList = new XTypedSubstitutedList<T>(container, typeManager, itemXNames);
            typedList.Clear();
            foreach (T typedItem in typedObjects)
            {
                typedList.Add(typedItem);
            }

            return typedList;
        }

        public override void Add(T value)
        {
            XName itemXName = value.Untyped.Name;
            container.SetElement(itemXName, value, true, null);
        }

        protected override bool IsEqual(XElement element, T value)
        {
            XElement newElement = value.Untyped;
            return element.Equals(newElement);
        }

        protected override XElement GetElementForValue(T value, bool createNew)
        {
            return value.Untyped;
        }

        protected override T GetValueForElement(XElement element)
        {
            //Cast to T should succeed since T is the type of the head and the members are all derived from the head
            return (T) XTypedServices.ToXTypedElement(element, typeManager); //Use auto-typing for subst members
        }

        protected override void UpdateElement(XElement oldElement, T value)
        {
            oldElement.AddBeforeSelf(GetElementForValue(value, true));
            oldElement.Remove();
        }
    }
}