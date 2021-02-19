//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Diagnostics;

namespace Xml.Schema.Linq
{
    public partial class XTypedElement
    {
        protected void SetAttributeWithValidation(XName name, object value, string propertyName,
            SimpleTypeValidator typeDef)
        {
            //Set value with validation
            object typedValue = null;
            SimpleTypeValidator matchingType = null;
            Exception e = typeDef.TryParseValue(value, XTypedServices.NameTable,
                new XNamespaceResolver(this.GetUntyped()), out matchingType, out typedValue);

            if (e == null)
            {
                SetAttribute(name, typedValue, typeDef.DataType);
            }
            else
            {
                throw new LinqToXsdException(propertyName, e.Message);
            }
        }


        protected void SetElementWithValidation(XName name, object value, string propertyName,
            SimpleTypeValidator typeDef)
        {
            //Set value after validation
            object typedValue = null;
            SimpleTypeValidator matchingType = null;
            Exception e = typeDef.TryParseValue(value, XTypedServices.NameTable,
                new XNamespaceResolver(this.GetUntyped()), out matchingType, out typedValue);

            if (e == null)
            {
                SetElement(name, typedValue, typeDef.DataType);
            }
            else
            {
                throw new LinqToXsdException(propertyName, e.Message);
            }
        }

        //Method for setting values of a simple typed root element
        protected void SetValueWithValidation(object value, string propertyName, SimpleTypeValidator simpleType)
        {
            //Set value after validation
            Debug.Assert((value as XTypedElement) == null,
                "Cannot set an XTypedElement value as type of simple typed root element");

            object typedValue = null;
            SimpleTypeValidator matchingType = null;
            Exception e = simpleType.TryParseValue(value, XTypedServices.NameTable,
                new XNamespaceResolver(this.GetUntyped()), out matchingType, out typedValue);

            if (e == null)
            {
                SetValue(typedValue, simpleType.DataType);
            }
            else
            {
                throw new LinqToXsdException(propertyName, e.Message);
            }
        }

        protected void SetUnionValue(object value,
            string propertyName,
            XTypedElement container,
            SimpleTypeValidator typeDef)
        {
            SetUnionCatchAll(value, propertyName, this, null, typeDef, SchemaOrigin.Text);
        }

        protected void SetUnionElement(object value,
            string propertyName,
            XTypedElement container,
            XName itemXName,
            SimpleTypeValidator typeDef)
        {
            SetUnionCatchAll(value, propertyName, container, itemXName, typeDef, SchemaOrigin.Element);
        }

        protected void SetUnionAttribute(object value,
            string propertyName,
            XTypedElement container,
            XName itemXName,
            SimpleTypeValidator typeDef)
        {
            SetUnionCatchAll(value, propertyName, container, itemXName, typeDef, SchemaOrigin.Attribute);
        }


        private void SetUnionCatchAll(object value,
            string propertyName,
            XTypedElement container,
            XName itemXName,
            SimpleTypeValidator typeDef,
            SchemaOrigin origin)
        {
            UnionSimpleTypeValidator unionDef = typeDef as UnionSimpleTypeValidator;

            Debug.Assert(unionDef != null);
            SimpleTypeValidator matchingType = null;
            object typedValue;
            Exception e = unionDef.TryParseValue(value,
                XTypedServices.NameTable,
                new XNamespaceResolver(container.GetUntyped()),
                out matchingType,
                out typedValue);

            if (e != null)
                throw new LinqToXsdException(propertyName, e.Message);
            else
            {
                if (matchingType is ListSimpleTypeValidator)
                {
                    ListSimpleTypeValidator listType = matchingType as ListSimpleTypeValidator;
                    switch (origin)
                    {
                        case SchemaOrigin.Element:
                            SetListElement(itemXName, value, listType.ItemType.DataType);
                            break;
                        case SchemaOrigin.Text:
                            SetListValue(value, listType.ItemType.DataType);
                            break;
                        case SchemaOrigin.Attribute:
                            SetListAttribute(itemXName, value, listType.ItemType.DataType);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (origin)
                    {
                        case SchemaOrigin.Element:
                            SetElement(itemXName, value, matchingType.DataType);
                            break;
                        case SchemaOrigin.Text:
                            SetValue(value, matchingType.DataType);
                            break;
                        case SchemaOrigin.Attribute:
                            SetAttribute(itemXName, value, matchingType.DataType);
                            break;
                        default: break;
                    }
                }
            }
        }


        protected void SetListValueWithValidation(object value,
            string propertyName,
            SimpleTypeValidator typeDef)
        {
            //This is for list type, will be saved as space-separated strings
            ListSimpleTypeValidator listDef = typeDef as ListSimpleTypeValidator;
            Debug.Assert(listDef != null);

            object typedValue;
            SimpleTypeValidator matchingType = null;

            Exception e = typeDef.TryParseValue(value, XTypedServices.NameTable,
                new XNamespaceResolver(this.GetUntyped()), out matchingType, out typedValue);

            if (e == null)
            {
                SetListValue(value, matchingType.DataType);
            }
            else
            {
                throw new LinqToXsdException(propertyName, e.Message);
            }
        }

        protected void SetListValue(object value, XmlSchemaDatatype datatype)
        {
            string strValue = ListFormatter.ToString(value);
            XElement element = this.GetUntyped();
            element.Value = strValue;
        }

        protected void SetListElementWithValidation(XName name,
            object value,
            string propertyName,
            SimpleTypeValidator typeDef)
        {
            object typedValue;
            SimpleTypeValidator matchingType = null;
            Exception e = typeDef.TryParseValue(value, XTypedServices.NameTable, new XNamespaceResolver(this.Untyped),
                out matchingType, out typedValue);

            if (e != null)
            {
                throw new LinqToXsdException(propertyName, e.Message);
            }

            ListSimpleTypeValidator listDef = typeDef as ListSimpleTypeValidator;
            Debug.Assert(listDef != null);

            SetListElement(name, typedValue, listDef.ItemType.DataType);
        }

        protected void SetListElement(XName name,
            object value,
            XmlSchemaDatatype datatype)
        {
            SetElement(name, ListFormatter.ToString(value), datatype);
        }

        protected void SetListAttributeWithValidation(XName name,
            object value,
            string propertyName,
            SimpleTypeValidator typeDef)
        {
            object typedValue;
            SimpleTypeValidator matchingType = null;
            Exception e = typeDef.TryParseValue(value, XTypedServices.NameTable, new XNamespaceResolver(this.Untyped),
                out matchingType, out typedValue);

            if (e != null)
            {
                throw new LinqToXsdException(propertyName, e.Message);
            }

            ListSimpleTypeValidator listDef = typeDef as ListSimpleTypeValidator;
            Debug.Assert(listDef != null);

            SetListAttribute(name, value, listDef.ItemType.DataType);
        }

        protected void SetListAttribute(XName name,
            object value,
            XmlSchemaDatatype datatype)
        {
            SetAttribute(name, ListFormatter.ToString(value), datatype);
        }
    }
}