using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    public abstract class XList<T> : XListVisualizable, IList<T>, ICountAndCopy
    {
        internal XTypedElement container;
        internal XElement containerElement;
        internal XName itemXName; //Name of head in case of substitution group
        internal XName[] namesInList;

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
                throw new ArgumentNullException("Argument value should not be null.");
            }

            return GetIndexOf(value);
        }

        public void Insert(int index, T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Argument value should not be null.");
            }

            int count = 0;
            XElement prevElement = GetElementAt(index, out count);
            if (index < 0 || index > count)
            {
                throw new ArgumentOutOfRangeException("index");
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
                XElement elementToAdd = GetElementForValue(value, true);
                prevElement.AddBeforeSelf(elementToAdd);
            }
        }

        public void RemoveAt(int index)
        {
            int count = 0;
            XElement elementToRemove = GetElementAt(index, out count);
            Debug.Assert(elementToRemove != null);
            elementToRemove.Remove();
        }

        public bool Remove(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Argument value should not be null.");
            }

            XElement element = GetElementForValue(value, false);
            XElement x = containerElement.Elements(element.Name).Where(e => e == element).FirstOrDefault();
            if (x != null)
            {
                //Found it in the list
                element.Remove();
                return true;
            }

            return false;
        }

        public virtual void Add(T value)
        {
            XElement element = GetElementForValue(value, true);
            container.SetElement(element.Name, value, true, null);
        }

        public void Clear()
        {
            ArrayList elementArray = new ArrayList();
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                elementArray.Add(listElementsEnumerator.Current);
            }

            foreach (XElement listElement in elementArray)
            {
                listElement.Remove();
            }
        }

        public T this[int index]
        {
            get
            {
                int count = 0;
                XElement element = GetElementAt(index, out count);
                return GetValueForElement(element);
            }
            set
            {
                int count = 0;
                XElement oldElement = GetElementAt(index, out count);
                Debug.Assert(oldElement != null);
                UpdateElement(oldElement, value);
            }
        }

        public void CopyTo(T[] valuesArray, int arrayIndex)
        {
            if (valuesArray == null)
            {
                throw new ArgumentNullException("Argument valuesArray should not be null.");
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            if (valuesArray.Rank != 1 || (arrayIndex >= valuesArray.Length))
            {
                throw new ArgumentException("valuesArray");
            }

            int index = arrayIndex;
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                if (index > valuesArray.Length)
                {
                    throw new ArgumentException("valuesArray");
                }

                valuesArray[index++] = GetValueForElement(listElementsEnumerator.Current);
            }
        }

        void ICountAndCopy.CopyTo(Array valuesArray, int arrayIndex)
        {
            if (valuesArray == null)
            {
                throw new ArgumentNullException("Argument valuesArray should not be null.");
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            if (valuesArray.Rank != 1 || (arrayIndex >= valuesArray.Length))
            {
                throw new ArgumentException("valuesArray");
            }

            int index = arrayIndex;
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                if (index > valuesArray.Length)
                {
                    throw new ArgumentException("valuesArray");
                }

                valuesArray.SetValue(GetValueForElement(listElementsEnumerator.Current), index++);
            }
        }


        public int Count
        {
            get
            {
                int count = 0;
                IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
                while (listElementsEnumerator.MoveNext())
                {
                    count++;
                }

                return count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Contains(T value)
        {
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                if (IsEqual(listElementsEnumerator.Current, value))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                yield return GetValueForElement(listElementsEnumerator.Current);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected abstract bool IsEqual(XElement element, T value);

        protected abstract XElement GetElementForValue(T value, bool createNew);

        protected abstract T GetValueForElement(XElement element);

        protected abstract void UpdateElement(XElement oldElement, T value);

        protected XElement GetElementAt(int index, out int count)
        {
            count = 0;
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                if (count++ == index)
                {
                    return listElementsEnumerator.Current;
                }
            }

            return null;
        }

        protected int GetIndexOf(T value)
        {
            int currentIndex = 0;
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                if (IsEqual(listElementsEnumerator.Current, value))
                {
                    return currentIndex;
                }

                currentIndex++;
            }

            return -1;
        }

        protected IEnumerator<XElement> GetListElementsEnumerator()
        {
            if (container.ValidationStates == null)
            {
                if (namesInList.Length == 1)
                {
                    return containerElement.Elements(itemXName).GetEnumerator();
                }
                else
                {
                    //Need to enumerate through all members of the subst group
                    return new SubstitutionMembersList(container, namesInList).GetEnumerator();
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

        private IEnumerator<XElement> FSMGetEnumerator()
        {
            IEnumerator<XElement> enumerator = containerElement.Elements().GetEnumerator();
            XElement elem = null;
            container.StartFsm();

            do
            {
                elem = container.ExecuteFSM(enumerator, itemXName, null);
                if (elem != null) yield return elem;
                else yield break;
            } while (elem != null);
        }
    }
}