//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml.Linq;
using System.Diagnostics;
using System.Xml.Schema;
using System.Collections.Generic;

namespace Xml.Schema.Linq
{
    public abstract class ContentModelEntity
    {
        public static readonly ContentModelEntity Default = new OrderUnawareContentModelEntity();

        SchemaAwareContentModelEntity parentContentModel;

        internal SchemaAwareContentModelEntity ParentContentModel
        {
            get { return this.parentContentModel; }
            set { this.parentContentModel = value; }
        }

        internal IEnumerable<SchemaAwareContentModelEntity> Ancestors
        {
            get
            {
                var ancestor = this.ParentContentModel;
                while (ancestor != null)
                {
                    yield return ancestor;
                    ancestor = ancestor.ParentContentModel;
                }
            }
        }

        public void AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype)
        {
            AddElementToParent(name, value, parentElement, addToExisting, datatype, value?.GetType());
        }

        public virtual XElement AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype, Type elementBaseType)
        {
            Debug.Assert(value != null);
            if (addToExisting)
            {
                var newElement = GetNewElement(name, value, datatype, parentElement, elementBaseType);
                parentElement.Add(newElement);
                return newElement;
            }
            else
            {
                var existingElement = parentElement.Element(name);
                if (existingElement == null)
                {
                    var newElement = GetNewElement(name, value, datatype, parentElement, elementBaseType);
                    parentElement.Add(newElement);
                    return newElement;
                }
                else if (datatype != null)
                {
                    //Update simple type value
                    existingElement.Value = XTypedServices.GetXmlString(value, datatype, existingElement);
                    return existingElement;
                }
                else
                {
                    var element = XTypedServices.GetXElement(value as XTypedElement, name, elementBaseType);
                    existingElement.AddBeforeSelf(element);
                    existingElement.Remove();
                    return element;
                }
            }
        }

        private XElement GetNewElement(XName name, object value, XmlSchemaDatatype datatype, XElement parentElement, Type elementBaseType)
        {
            XElement newElement = null;
            if (datatype != null)
            {
                string stringValue = XTypedServices.GetXmlString(value, datatype, parentElement);
                newElement = new XElement(name, stringValue);
            }
            else
            {
                newElement = XTypedServices.GetXElement(value as XTypedElement, name, elementBaseType);
            }

            return newElement;
        }
    }
}