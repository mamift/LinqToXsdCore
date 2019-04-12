using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public class XListContent<T> : IList<T>
    {
        internal XElement containerElement;
        XName itemXName;
        private List<T> items;
        XmlSchemaDatatype datatype;
        ContainerType containerType;

        public XListContent(string value, XElement containerElement, XName name, ContainerType type,
            XmlSchemaDatatype datatype)
        {
            this.containerElement = containerElement;
            this.itemXName = name;
            this.datatype = datatype;
            this.containerType = type;
            GenerateList(value);
        }

        public XListContent(IList value, XElement containerElement, XName name, ContainerType type,
            XmlSchemaDatatype datatype)
        {
            this.containerElement = containerElement;
            this.itemXName = name;
            this.datatype = datatype;
            this.containerType = type;
            CopyList(value);
        }

        private void CopyList(IList value)
        {
            if (this.items == null)
            {
                this.items = new List<T>();
            }
            else
            {
                this.items.Clear();
            }

            foreach (T t in value) this.items.Add(t);
        }

        internal void GenerateList(string value)
        {
            string[] strs = value.Split(' ');

            if (value == string.Empty || strs.Length == 0)
            {
                this.items = new List<T>();
            }
            else
            {
                this.items = new List<T>(strs.Length);
                foreach (string item in strs)
                {
                    this.items.Add(XTypedServices.ParseValue<T>(item, containerElement, datatype));
                }
            }
        }


        public int IndexOf(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Argument value should not be null.");
            }

            return this.items.IndexOf(value);
        }

        private void SaveValue()
        {
            switch (containerType)
            {
                case ContainerType.Element:
                    containerElement.Value = ListSimpleTypeValidator.ToString(this.items);
                    return;
                case ContainerType.Attribute:
                    XAttribute attr = containerElement.Attribute(itemXName);
                    Debug.Assert(attr != null);
                    attr.Value = ListSimpleTypeValidator.ToString(this.items);
                    return;
            }
        }

        public void Insert(int index, T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Argument value should not be null.");
            }

            this.items.Insert(index, value);

            //Save the value in the tree
            SaveValue();
        }

        public void RemoveAt(int index)
        {
            this.items.RemoveAt(index);
            SaveValue();
        }

        public bool Remove(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("Argument value should not be null.");
            }

            if (this.items.Remove(value))
            {
                SaveValue();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Add(T value)
        {
            this.items.Add(value);
            SaveValue();
        }

        public void Clear()
        {
            this.items.Clear();
            SaveValue();
        }

        public T this[int index]
        {
            get { return this.items[index]; }
            set
            {
                this.items[index] = value;
                SaveValue();
            }
        }

        public void CopyTo(T[] valuesArray, int arrayIndex)
        {
            this.items.CopyTo(valuesArray, arrayIndex);
        }


        public int Count
        {
            get { return this.items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Contains(T value)
        {
            return this.items.Contains(value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}