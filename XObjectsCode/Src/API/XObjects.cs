//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Linq;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Xml.Schema.Linq
{
    //Class that represents xs:anyType, the root of the schema type system
    public partial class XTypedElement : IXMetaData, IXTyped, IXmlSerializable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private XElement xElement = null;

        public XTypedElement() { }

        public XTypedElement(XElement xe)
        {
            xElement = xe;
        }

        public override string ToString()
        {
            return this.Untyped.ToString();
        }

        public static explicit operator XElement(XTypedElement xo)
        {
            return xo.Untyped;
        }

        public static explicit operator XTypedElement(XElement xe)
        {
            return new XTypedElement(xe);
        }

        // introduce this GetUntyped, so that we dont call any virtual
        // methods from within constructors.
        private XElement GetUntyped()
        {
            if (xElement == null)
            {
                xElement = CreateXElement();
            }

            return xElement;
        }

        // Cast XTypedElement subtypes to XElement
        // GetUntyped() is preferred within XTypedElement,
        // to avoid calling into virtual functions from within 
        // constructor call stacks
        public virtual XElement Untyped
        {
            get { return GetUntyped(); }
            set { xElement = value; }
        }

        //Cast XTypedElement subtypes to IXTyped for Query
        public IXTyped Query
        {
            get { return (IXTyped) this; }
        }

        public virtual XTypedElement Clone()
        {
            XElement toCloneElement = this.Untyped;
            XTypedElement newObject = new XTypedElement();
            newObject.Untyped = new XElement(toCloneElement);
            return newObject;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Dictionary<XName, System.Type> IXMetaData.LocalElementsDictionary
        {
            get { return XTypedServices.EmptyDictionary; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ILinqToXsdTypeManager IXMetaData.TypeManager
        {
            get { return TypeManager.Default; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        XName IXMetaData.SchemaName
        {
            get { return XName.Get("anyType", XmlSchema.Namespace); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        SchemaOrigin IXMetaData.TypeOrigin
        {
            get { return SchemaOrigin.Fragment; }
        }

        XTypedElement IXMetaData.Content
        {
            //Content is non-null only for root element wrapper types
            get { return null; }
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            XElement deserializedElement = new XElement(((IXMetaData) this).SchemaName);
            ((IXmlSerializable) deserializedElement).ReadXml(reader);
            this.Untyped = deserializedElement;
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            IXmlSerializable serializable = this.Untyped as IXmlSerializable;
            serializable.WriteXml(writer);
        }

        ContentModelEntity IXMetaData.GetContentModel()
        {
            return ContentModelEntity.Default;
        }

        protected XElement Element(XName xname)
        {
            return this.GetUntyped().Element(xname);
        }

        protected XAttribute Attribute(XName name)
        {
            return this.GetUntyped().Attribute(name);
        }

        protected void SetAttribute(XName name, object value, XmlSchemaDatatype datatype)
        {
            XElement element = this.GetUntyped();
            string stringValue = null;
            if (value != null)
            {
                stringValue = XTypedServices.GetXmlString(value, datatype, element);
            }

            element.SetAttributeValue(name, stringValue);
        }

        protected void SetElement(XName name, XTypedElement typedElement)
        {
            if (ValidationStates == null)
            {
                Debug.Assert(name != null);
                SetElement(name, typedElement, false, null);
            }
            else FSMSetElement(name, typedElement, false, null);
        }

        //Set XSD simple values
        protected void SetElement(XName name, object value, XmlSchemaDatatype datatype)
        {
            if (ValidationStates == null)
            {
                Debug.Assert(name != null);
                SetElement(name, value, false, datatype);
            }
            else FSMSetElement(name, value, false, datatype);
        }

        protected internal XElement GetElement(XName requestingName)
        {
            return GetElement(requestingName, null);
        }

        protected internal XElement GetElement(WildCard requestingWildCard)
        {
            return GetElement(null, requestingWildCard);
        }

        private XElement GetElement(XName requestingName, WildCard requestingWildCard)
        {
            if (ValidationStates == null)
            {
                Debug.Assert(requestingName != null);
                return this.GetUntyped().Element(requestingName);
            }
            else
            {
                StartFsm();
                return ExecuteFSM(GetUntyped().Elements().GetEnumerator(), requestingName, requestingWildCard);
            }
        }

        protected void SetValue(object value, XmlSchemaDatatype datatype)
        {
            Debug.Assert((value as XTypedElement) == null,
                "Cannot set an XTypedElement value as type of simple typed root element");
            XElement element = this.GetUntyped();
            element.Value = XTypedServices.GetXmlString(value, datatype, element);
        }

        internal void SetElement(XName name, object value, bool addToExisting, XmlSchemaDatatype datatype)
        {
            SetElement(name, value, addToExisting, datatype, null);
        }

        internal void SetElement(XName name, object value, bool addToExisting, XmlSchemaDatatype datatype, Type elementBaseType)
        {
            XElement parentElement = this.GetUntyped();
            CheckXsiNil(parentElement);
            if (value == null)
            {
                //Delete existing node
                Debug.Assert(addToExisting == false);
                DeleteChild(name);
            }
            else
            {
                IXMetaData schemaMetaData = this as IXMetaData; //Get parent's content model
                Debug.Assert(schemaMetaData != null);
                ContentModelEntity cm = schemaMetaData.GetContentModel();

                if (elementBaseType == null)
                {
                    if (!schemaMetaData.LocalElementsDictionary.TryGetValue(name, out elementBaseType))
                    {
                        elementBaseType = value?.GetType();
                    }
                }


                cm.AddElementToParent(name, value, parentElement, addToExisting, datatype, elementBaseType);
            }
        }

        private void CheckXsiNil(XElement parentElement)
        {
            XAttribute xsiNil = parentElement.Attributes(XName.Get("nil", XmlSchema.InstanceNamespace))
                                             .FirstOrDefault();
            if (xsiNil != null && xsiNil.Value == "true")
            {
                //Since we are adding content
                xsiNil.Remove();
            }
        }

        private void DeleteChild(XName name)
        {
            XElement elementToDelete = this.GetElement(name);
            if (elementToDelete != null)
            {
                elementToDelete.Remove();
            }
        }

        private XElement CreateXElement()
        {
            IXMetaData schemaMetaData = this as IXMetaData;
            Debug.Assert(schemaMetaData != null);

            XName elementName = schemaMetaData.SchemaName;
            Debug.Assert(elementName != null);

            XElement element = new XElement(elementName);
            element.AddAnnotation(new XTypedElementAnnotation(this)); //Link xelement and XTypedElement
            return element;
        }
    }


    //Little explanation on ICountAndCopy, XListVisualizable, XList<T>:
    //1. DebuggerTypeProxy is not allowed on a Generic class, so we cannot
    //put it on XList, so we need a non-generic base class that we can apply
    //the DebuggerTypeProxy attribute to. This is the reason for the life of the class XListVisualizable
    //
    //2. Additionally we need a non-generic interface which to cast the _xList variable inside of XListVisualizer,since
    //we cannot cast an XList<T> to an IList or even IList<>. We need to know the T in order to cast even though we dont use any methods
    // that rely on T.
    //So we create the new interface, ICountAndCopy. It has come redundant information from ICollection,
    //but we use a new interface, so that it can be internal and we dont need to implement things like SyncRoot, and IsSynchronized.
    //If we ever make XList implement non-generic IList, or ICollection, then we can get rid of ICountAndCopy.
    internal interface ICountAndCopy
    {
        void CopyTo(Array valuesArray, int arrayIndex);
        int Count { get; }
    }
}