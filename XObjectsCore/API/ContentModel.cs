//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml.Linq;
using System.Diagnostics;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    public abstract class ContentModelEntity
    {
        public static readonly ContentModelEntity Default = new OrderUnawareContentModelEntity();

        public virtual void AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype)
        {
            AddElementToParent(name, value, parentElement, addToExisting, datatype, value?.GetType());
        }

        public virtual void AddElementToParent(XName name, object value, XElement parentElement, bool addToExisting,
            XmlSchemaDatatype datatype, Type elementBaseType)
        {
            Debug.Assert(value != null);
            if (addToExisting)
            {
                parentElement.Add(GetNewElement(name, value, datatype, parentElement, elementBaseType));
            }
            else
            {
                XElement existingElement = parentElement.Element(name);
                if (existingElement == null)
                {
                    parentElement.Add(GetNewElement(name, value, datatype, parentElement, elementBaseType));
                }
                else if (datatype != null)
                {
                    //Update simple type value
                    existingElement.Value = XTypedServices.GetXmlString(value, datatype, existingElement);
                }
                else
                {
                    existingElement.AddBeforeSelf(XTypedServices.GetXElement(value as XTypedElement, name, elementBaseType));
                    existingElement.Remove();
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