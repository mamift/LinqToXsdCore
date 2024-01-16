using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq
{
    public abstract class XList<T> : XListVisualizable, IList<T>, ICountAndCopy
    {
        internal XTypedElement container;
        internal XElement containerElement;
        internal XName itemXName; //Name of head in case of substitution group
        internal XName[] namesInList;
        
        public bool SupportsXsiNil { get; init; }
        
        protected XList(XTypedElement container, params XName[] names)
        {
            this.container = container;
            this.containerElement = container.Untyped;
            namesInList = names;
            itemXName = names[names.Length - 1]; //Head is the last element name in the list
        }

        public int IndexOf(T value)
        {
            if (value == null)
            {
                return SupportsXsiNil
                    ? EnumerateElements().FindIndex(XNil.IsXsiNil)
                    : throw new ArgumentNullException(nameof(value), "Argument value should not be null.");
            }

            return EnumerateElements().FindIndex(x => IsEqual(x, value));
        }

        public void Insert(int index, T value)
        {
            if (value == null && !SupportsXsiNil)
            {
                throw new ArgumentNullException(nameof(value), "Argument value should not be null.");
            }

            XElement prevElement = GetElementAt(index, out int count);
            if (index < 0 || index > count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (index == count)
            {
                //Add to end of list
                Debug.Assert(prevElement == null);
                Add(value);
            }
            else
            {
                Debug.Assert(prevElement != null);
                prevElement.AddBeforeSelf(ElementFor(value, true));
            }
        }

        public void RemoveAt(int index)
        {
            XElement elementToRemove = GetElementAt(index, out int _)
                ?? throw new ArgumentOutOfRangeException(nameof(index));
            elementToRemove.Remove();
        }

        public bool Remove(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Argument value should not be null.");
            }

            XElement element = ElementFor(value, false);
            if (element != null && element.Parent == containerElement)
            {
                element?.Remove();
                return true;
            }

            return false;
        }

        public void Add(T value)
        {
            if (value == null)
            {
                if (!SupportsXsiNil)
                {
                    throw new ArgumentNullException(nameof(value), "Argument value should not be null.");
                }
                container.SetElement(itemXName, XNil.Value, true, null);
            }
            else
            {
                AddImpl(value);
            }
        }

        protected abstract void AddImpl(T value);

        public void Clear()
        {
            foreach (XElement listElement in EnumerateElements().ToList())
            {
                listElement.Remove();
            }
        }

        public T this[int index]
        {
            get
            {
                XElement element = GetElementAt(index, out int _);
                return ValueOf(element);
            }
            set
            {
                XElement oldElement = GetElementAt(index, out int _);
                Debug.Assert(oldElement != null);
                UpdateElement(oldElement, value);
            }
        }

        public void CopyTo(T[] valuesArray, int index)
        {
            if (valuesArray == null)
            {
                throw new ArgumentNullException(nameof(valuesArray), "Argument valuesArray should not be null.");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (valuesArray.Rank != 1 || (index >= valuesArray.Length))
            {
                throw new ArgumentException(nameof(valuesArray));
            }
            
            foreach (var element in EnumerateElements())
            {
                if (index > valuesArray.Length)
                {
                    throw new ArgumentException(nameof(valuesArray));
                }

                valuesArray[index++] = ValueOf(element);
            }
        }

        void ICountAndCopy.CopyTo(Array valuesArray, int index)
        {
            if (valuesArray == null)
            {
                throw new ArgumentNullException(nameof(valuesArray), "Argument valuesArray should not be null.");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (valuesArray.Rank != 1 || (index >= valuesArray.Length))
            {
                throw new ArgumentException(nameof(valuesArray));
            }

            foreach (var element in EnumerateElements())
            {
                if (index > valuesArray.Length)
                {
                    throw new ArgumentException(nameof(valuesArray));
                }

                valuesArray.SetValue(ValueOf(element), index++);
            }
        }

        public int Count => EnumerateElements().Count();

        public bool IsReadOnly => false;

        public bool Contains(T value) => EnumerateElements().Any(x => IsEqual(x, value));

        public IEnumerator<T> GetEnumerator() => EnumerateValues().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected abstract bool IsEqual(XElement element, T value);

        protected XElement ElementFor(T value, bool createNew)
        {            
            return value != null
                ? ElementForImpl(value, createNew)
                : !SupportsXsiNil
                    ? throw new ArgumentNullException(nameof(value), "Argument value should not be null.")
                    : createNew
                        ? XNil.Element(itemXName)
                        : EnumerateElements().FirstOrDefault(XNil.IsXsiNil);
        }

        protected abstract XElement ElementForImpl(T value, bool createNew);

        protected T ValueOf(XElement element)
        {
            return element.IsXsiNil() ? default : ValueOfImpl(element);
        }

        protected abstract T ValueOfImpl(XElement element);

        protected void UpdateElement(XElement element, T value)
        {
            if (value == null)
            {
                if (!SupportsXsiNil)
                {
                    throw new ArgumentNullException(nameof(value), "Argument value should not be null.");
                }
                element.SetXsiNil();
            }
            else
            {
                element.RemoveXsiNil();
                UpdateElement(element, value);
            }
        }

        protected abstract void UpdateElementImpl(XElement oldElement, T value);

        protected XElement GetElementAt(int index, out int count)
        {
            count = 0;
            foreach (var element in EnumerateElements())
            {
                if (count++ == index) return element;
            }
            return null;
        }

        protected IEnumerable<T> EnumerateValues() 
            => EnumerateElements().Select(ValueOf);

        protected IEnumerable<XElement> EnumerateElements()
        {
            if (container.ValidationStates == null)
            {
                if (namesInList.Length == 1)
                {
                    return containerElement.Elements(itemXName);
                }
                else
                {
                    //Need to enumerate through all members of the subst group
                    return new SubstitutionMembersList(container, namesInList);
                }
            }
            else
            {
                if (namesInList.Length == 1)
                {
                    return FSMGetEnumerator();
                }
                else
                {
                    //Need to enumerate through all members of the subst group
                    return new SubstitutionMembersList(container, namesInList).FSMGetEnumerator();
                }
            }
        }

        private IEnumerable<XElement> FSMGetEnumerator()
        {
            IEnumerator<XElement> enumerator = containerElement.Elements().GetEnumerator();
            container.StartFsm();
            while (true)
            {
                XElement elem = container.ExecuteFSM(enumerator, itemXName, null);
                if (elem == null) yield break;
                yield return elem;
            }
        }
    
        protected void InitializeFrom(IEnumerable<T> values)
        {
            Clear();
            foreach (T value in values) Add(value);
        }
    }
}