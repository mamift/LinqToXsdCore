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

        public static XTypedSubstitutedList<T> InitializeNillable(
            XTypedElement container, 
            ILinqToXsdTypeManager typeManager,
            IEnumerable<T> typedObjects,
            params XName[] itemXNames)
        {
            var typedList = new XTypedSubstitutedList<T>(container, typeManager, itemXNames) { SupportsXsiNil = true };
            typedList.InitializeFrom(typedObjects);
            return typedList;
        }

        public static XTypedSubstitutedList<T> Initialize(
            XTypedElement container, 
            ILinqToXsdTypeManager typeManager,
            IEnumerable<T> typedObjects, 
            params XName[] itemXNames)
        {
            var typedList = new XTypedSubstitutedList<T>(container, typeManager, itemXNames);
            typedList.InitializeFrom(typedObjects);
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

        protected override XElement ElementForImpl(T value, bool createNew)
        {
            return value.Untyped;
        }

        protected override T ValueOfImpl(XElement element)
        {
            //Cast to T should succeed since T is the type of the head and the members are all derived from the head
            return (T) XTypedServices.ToXTypedElement(element, typeManager); //Use auto-typing for subst members
        }

        protected override void UpdateElementImpl(XElement oldElement, T value)
        {
            oldElement.AddBeforeSelf(ElementForImpl(value, true));
            oldElement.Remove();
        }
    }
}