//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;

namespace Xml.Schema.Linq
{
    //Class that represents xs:anyType, the root of the schema type system
    public partial class XTypedElement : IXMetaData, IXTyped, IXmlSerializable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private XElement xElement = null;

        public XTypedElement()
        {
        }

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
                cm.AddElementToParent(name, value, parentElement, addToExisting, datatype);
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

    public static class XTypedServices
    {
        public static readonly Dictionary<XName, System.Type> EmptyDictionary = new Dictionary<XName, System.Type>();

        public static readonly Dictionary<System.Type, System.Type> EmptyTypeMappingDictionary =
            new Dictionary<System.Type, System.Type>();

        internal static readonly Type typeOfString = typeof(System.String);

        static NameTable nameTable;

        //Cast XElement to wrapper type W with wrapee type T, Called from Load and Parse
        public static W ToXTypedElement<W, T>(XElement xe, ILinqToXsdTypeManager typeManager)
            where T : XTypedElement where W : XTypedElement
        {
            return (W) ToXTypedElement(xe, typeManager, typeof(W), typeof(T));
        }

        //Called from Load/parse and auto-typing
        public static XTypedElement ToXTypedElement(XElement xe, ILinqToXsdTypeManager typeManager, Type rootType,
            Type contentType)
        {
            XTypedElement rootElement = GetAnnotation(rootType, xe);
            if (rootElement == null)
            {
                //If not already created
                XName
                    instanceElementName =
                        xe.Name; //Storing the name since SetName() called from the functional const of rootType will update the name to that of the root type
                XTypedElement innerType = ToXTypedElement(xe, typeManager, contentType);
                Debug.Assert(innerType != null);
                ConstructorInfo constInfo = rootType.GetConstructor(new Type[] {contentType});

                if (constInfo != null)
                {
                    rootElement = (XTypedElement) constInfo.Invoke(new object[] {innerType});
                }
                else
                {
                    throw new LinqToXsdException(contentType.ToString() +
                                                 " is not an expected content type for root element type " +
                                                 rootType.ToString());
                }

                if (!TypeValid(rootElement, instanceElementName))
                {
                    throw new LinqToXsdException("Element is not an instance of type " + rootType);
                }
            }

            return rootElement;
        }

        //Cast XElement to XTypedElement type T, Called from Load and Parse
        public static T ToXTypedElement<T>(XElement xe) where T : XTypedElement, new()
        {
            if (xe == null)
            {
                return null;
            }

            T xoSubType = GetAnnotation<T>(xe);
            if (xoSubType == null)
            {
                //No association bet XTypedElement and xelement
                xoSubType = new T();
                if (TypeValid(xoSubType, xe.Name))
                {
                    xoSubType.Untyped = xe;
                    xe.AddAnnotation(new XTypedElementAnnotation(xoSubType));
                }
                else
                {
                    throw new LinqToXsdException("Element is not an instance of type " + xoSubType.GetType());
                }
            }

            return xoSubType;
        }

        //For instantiating objects with derived types
        public static T ToXTypedElement<T>(XElement xe, ILinqToXsdTypeManager typeManager) where T : XTypedElement
        {
            return ToXTypedElement(xe, typeManager, typeof(T)) as T;
        }

        public static XTypedElement ToXTypedElement(XElement xe, ILinqToXsdTypeManager typeManager, System.Type t)
        {
            if (xe == null)
            {
                return null;
            }

            if (!t.IsSubclassOf(typeof(XTypedElement)))
            {
                throw new InvalidOperationException("Type t is not a subtype of XTypedElement");
            }

            if (typeManager == null)
            {
                throw new ArgumentNullException("typeManager");
            }

            XTypedElement
                xoSubType = GetAnnotation(t,
                    xe); //Try getting back as the type first, optimized for the cases where xsi:type cannot appear
            if (xoSubType == null)
            {
                //Try xsi:type and lookup in the typeDictionary
                Type clrType = GetXsiClrType(xe, typeManager);
                if (clrType != null)
                {
                    xoSubType = GetAnnotation(clrType, xe);
                    if (xoSubType != null)
                    {
                        return xoSubType;
                    }

                    if (!t.IsAssignableFrom(clrType))
                    {
                        //xsi:type is not subtype of schema type
                        clrType = t;
                    }
                }
                else
                {
                    //xsi:type not present or CLRType not found for xsi:type name 
                    clrType = t;
                }

                if (clrType.IsAbstract)
                {
                    throw new InvalidOperationException("Cannot cast XElement to an abstract type");
                }

                ConstructorInfo constrInfo = clrType.GetConstructor(System.Type.EmptyTypes);
                xoSubType = (XTypedElement) constrInfo.Invoke(null);
                xoSubType.Untyped = xe;
                xe.AddAnnotation(new XTypedElementAnnotation(xoSubType));
            }

            return xoSubType;
        }

        //Auto-typing
        public static XTypedElement ToXTypedElement(XElement xe, ILinqToXsdTypeManager typeManager)
        {
            if (xe == null)
            {
                return null;
            }

            XName elementName = xe.Name;
            Type clrType = null;
            Dictionary<XName, Type> elementDictionary = typeManager.GlobalElementDictionary;
            if (elementDictionary.TryGetValue(elementName, out clrType))
            {
                //Check if its a root wrapper type
                Type contentType = null;
                if (typeManager.RootContentTypeMapping.TryGetValue(clrType, out contentType))
                {
                    return ToXTypedElement(xe, typeManager, clrType, contentType);
                }

                return ToXTypedElement(xe, typeManager, clrType);
            }

            Type xsiClrType =
                GetXsiClrType(xe,
                    typeManager); //Check for global xsi:type, tag might be a fragment or unknown element name
            if (xsiClrType != null)
            {
                return ToXTypedElement(xe, typeManager, xsiClrType);
            }

            return null;
        }

        public static XTypedElement ToSubstitutedXTypedElement(XTypedElement parentType,
            ILinqToXsdTypeManager typeManager, params XName[] substitutedMembers)
        {
            XElement substElement = null;
            XElement parentElement = parentType.Untyped;
            int index = 0;
            while (substElement == null && index < substitutedMembers.Length)
            {
                substElement = parentType.GetElement(substitutedMembers[index++]);
            }

            if (substElement != null)
            {
                return XTypedServices.ToXTypedElement(substElement, typeManager);
            }

            return null;
        }

        public static T CloneXTypedElement<T>(T xTypedElement) where T : XTypedElement, new()
        {
            if (xTypedElement == null)
            {
                throw new ArgumentNullException("Argument xTypedElement should not be null.");
            }

            XElement clonedElement = new XElement(xTypedElement.Untyped);
            T newObject = new T();
            newObject.Untyped = clonedElement;
            clonedElement.AddAnnotation(
                new XTypedElementAnnotation(newObject)); //Need to set up association for the cloned type
            return newObject;
        }

        public static T Load<T>(System.IO.TextReader reader) where T : XTypedElement, new()
        {
            XDocument doc = XDocument.Load(reader);
            XElement xeroot = doc.Root;
            return ToXTypedElement<T>(xeroot);
        }

        public static T Load<T>(string uri) where T : XTypedElement, new()
        {
            XDocument doc = XDocument.Load(uri);
            XElement xeroot = doc.Root;
            return ToXTypedElement<T>(xeroot);
        }

        public static W Load<W, T>(string uri, ILinqToXsdTypeManager typeManager)
            where T : XTypedElement where W : XTypedElement
        {
            XDocument doc = XDocument.Load(uri);
            XElement xeroot = doc.Root;
            return ToXTypedElement<W, T>(xeroot, typeManager);
        }

        public static W Load<W, T>(System.IO.TextReader reader, ILinqToXsdTypeManager typeManager)
            where T : XTypedElement
            where W : XTypedElement
        {
            XDocument doc = XDocument.Load(reader);
            XElement xeroot = doc.Root;
            return ToXTypedElement<W, T>(xeroot, typeManager);
        }

        public static T Parse<T>(string xml) where T : XTypedElement, new()
        {
            return ToXTypedElement<T>(XElement.Parse(xml));
        }

        public static W Parse<W, T>(string xml, ILinqToXsdTypeManager typeManager)
            where T : XTypedElement where W : XTypedElement
        {
            return ToXTypedElement<W, T>(XElement.Parse(xml), typeManager);
        }

        public static void Save(string xmlFile, XElement xe)
        {
            if (xmlFile == null)
            {
                throw new ArgumentNullException("xmlFile");
            }

            Debug.Assert(xe != null);
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.Indent = true;
            using (XmlWriter w = XmlWriter.Create(xmlFile, ws))
            {
                Save(w, xe);
            }
        }

        public static void Save(TextWriter tw, XElement xe)
        {
            if (tw == null)
            {
                throw new ArgumentNullException("tw");
            }

            Debug.Assert(xe != null);
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.Indent = true;
            using (XmlWriter w = XmlWriter.Create(tw, ws))
            {
                Save(w, xe);
            }
        }

        public static void Save(XmlWriter xmlWriter, XElement xe)
        {
            if (xmlWriter == null)
            {
                throw new ArgumentNullException("xmlWriter");
            }

            Debug.Assert(xe != null);
            xe.Save(xmlWriter);
        }

        //Services for Generated types
        public static void SetContentToNil(XTypedElement xTypedElement)
        {
            XName xsiNil = XName.Get("nil", XmlSchema.InstanceNamespace);
            XElement thisElement = xTypedElement.Untyped;
            thisElement.RemoveNodes();
            if (thisElement.Attribute(xsiNil) == null)
            {
                //xsi:nil attribute not already present
                thisElement.Add(new XAttribute(xsiNil, "true"));
            }
        }

        public static void SetName(XTypedElement root, XTypedElement type)
        {
            //Root with correct element name
            IXMetaData schemaMetaData = root as IXMetaData;
            Debug.Assert(schemaMetaData != null);
            XName elementName = schemaMetaData.SchemaName;
            Debug.Assert(elementName != null);

            //Set the wrapper object's XElement pointer and vice versa
            XElement currentElement = type.Untyped;
            currentElement.Name = elementName;
            currentElement.AddAnnotation(
                new XTypedElementWrapperAnnotation(
                    root)); //The XElement tree is hooked up to both root type as well as content type?
            root.Untyped = currentElement;
        }

        public static XTypedElement GetCloneIfRooted(XTypedElement innerType)
        {
            if (innerType == null)
            {
                throw new ArgumentNullException("Argument innerType should not be null.");
            }

            XElement fragment = innerType.Untyped;
            XTypedElementWrapperAnnotation wrapperAnnotation = fragment.Annotation<XTypedElementWrapperAnnotation>();
            if (wrapperAnnotation != null)
            {
                //Already rooted to a wrapper
                return innerType.Clone();
            }
            else
            {
                return innerType;
            }
        }

        public static void SetList<T>(IList<T> list, IList<T> value)
        {
            if ((object) list == (object) value)
            {
                //set property to itself, do nothing
                return;
            }

            list.Clear();
            foreach (T obj in value)
            {
                list.Add(obj);
            }
        }

        public static T ParseValue<T>(XElement element, XmlSchemaDatatype datatype)
        {
            if (element == null)
            {
                return default(T);
            }

            return ParseValue<T>(element.Value, element, datatype);
        }

        public static T ParseValue<T>(XAttribute attribute, XmlSchemaDatatype datatype)
        {
            if (attribute == null)
            {
                return default(T);
            }

            return ParseValue<T>(attribute.Value, attribute.Parent, datatype);
        }

        public static T ParseValue<T>(XAttribute attribute, XmlSchemaDatatype datatype, T defaultValue)
        {
            if (attribute == null)
            {
                return defaultValue;
            }

            return ParseValue<T>(attribute.Value, attribute.Parent, datatype);
        }

        public static IList<T> ParseListValue<T>(XElement element, XmlSchemaDatatype datatype)
        {
            if (element == null) return null;

            return ParseListValue<T>(element.Value, element, element.Name, ContainerType.Element, datatype);
        }

        public static IList<T> ParseListValue<T>(XAttribute attribute, XmlSchemaDatatype datatype)
        {
            if (attribute == null) return null;

            return ParseListValue<T>(attribute.Value, attribute.Parent, attribute.Name, ContainerType.Attribute,
                datatype);
        }

        public static IList<T> ParseListValue<T>(XAttribute attribute, XmlSchemaDatatype datatype,
            IList<T> defaultValue)
        {
            if (attribute == null)
                return defaultValue;

            return ParseListValue<T>(attribute.Value, attribute.Parent, attribute.Name, ContainerType.Attribute,
                datatype);
        }

        private static IList<T> ParseListValue<T>(string value, XElement element, XName name,
            ContainerType containerType, XmlSchemaDatatype datatype)
        {
            return new XListContent<T>(value, element, name, containerType, datatype);
        }

        public static object ParseUnionValue(XElement element, SimpleTypeValidator typeDef)
        {
            if (element == null) return null;
            return ParseUnionValue(element.Value, element, element.Name, typeDef, ContainerType.Element);
        }

        public static object ParseUnionValue(XAttribute attribute, SimpleTypeValidator typeDef)
        {
            if (attribute == null) return null;
            return ParseUnionValue(attribute.Value, attribute.Parent, attribute.Name, typeDef, ContainerType.Attribute);
        }

        private static object ParseUnionValue(string value, XElement element, XName itemXName,
            SimpleTypeValidator typeDef, ContainerType containerType)
        {
            //Parse the string value based on the member types
            UnionSimpleTypeValidator unionDef = typeDef as UnionSimpleTypeValidator;

            Debug.Assert(unionDef != null);
            SimpleTypeValidator matchingType = null;
            object typedValue;
            Exception e = unionDef.TryParseValue(value, NameTable, new XNamespaceResolver(element), out matchingType,
                out typedValue);

            ListSimpleTypeValidator listType = matchingType as ListSimpleTypeValidator;
            if (listType != null)
            {
                SimpleTypeValidator itemType = listType.ItemType;
                return new XListContent<object>((IList) typedValue, element, itemXName, containerType,
                    itemType.DataType);
            }

            Debug.Assert(e == null);

            return typedValue;
        }

        internal static T GetAnnotation<T>(XElement xe) where T : XTypedElement
        {
            return (T) GetAnnotation(typeof(T), xe);
        }

        internal static XTypedElement GetAnnotation(Type t, XElement xe)
        {
            XTypedElementWrapperAnnotation xoWrapperAnnotation = xe.Annotation<XTypedElementWrapperAnnotation>();
            XTypedElement xObj = null;
            if (xoWrapperAnnotation != null)
            {
                //Return the root type if the element is annotated with the root
                xObj = xoWrapperAnnotation.typedElement;
                if (t.IsAssignableFrom(xObj.GetType()))
                {
                    //Check if we are asking for element wrapper
                    return xObj;
                }
            }

            XTypedElementAnnotation xoAnnotation = xe.Annotation<XTypedElementAnnotation>();
            if (xoAnnotation != null)
            {
                xObj = xoAnnotation.typedElement;
                if (t.IsAssignableFrom(xObj.GetType()))
                {
                    //Check if we are asking for type
                    return xObj;
                }
            }

            return null;
        }

        internal static T ParseValue<T>(string value, XElement element, XmlSchemaDatatype datatype)
        {
            if (datatype.TypeCode == XmlTypeCode.QName || datatype.TypeCode == XmlTypeCode.NCName)
            {
                return (T) datatype.ParseValue(value, NameTable, new XNamespaceResolver(element));
            }
            else
            {
                return (T) datatype.ChangeType(value, typeof(T));
            }
        }

        internal static NameTable NameTable
        {
            get
            {
                if (nameTable == null)
                {
                    Interlocked.CompareExchange(ref nameTable, new NameTable(), null);
                }

                return nameTable;
            }
        }

        internal static System.Type GetXsiClrType(XElement xe, ILinqToXsdTypeManager typeManager)
        {
            XName typeName = GetXsiType(xe);
            Type clrType = null;
            if (typeName != null)
            {
                Dictionary<XName, Type> typeDictionary = typeManager.GlobalTypeDictionary;
                typeDictionary.TryGetValue(typeName, out clrType);
            }

            return clrType;
        }

        internal static XName GetXsiType(XElement xe)
        {
            string type = (string) xe.Attribute(XName.Get("type", XmlSchema.InstanceNamespace));
            if (type != null)
            {
                string prefix = string.Empty;
                string localName = ParseQName(type, out prefix);
                XNamespace ns = null;
                if (prefix.Length == 0)
                {
                    ns = xe.GetDefaultNamespace();
                }
                else
                {
                    ns = xe.GetNamespaceOfPrefix(prefix);
                }

                if (ns != null)
                {
                    return ns.GetName(localName);
                }
                else
                {
                    return XName.Get(localName);
                }
            }

            return null;
        }

        internal static string GetXmlString(object value, XmlSchemaDatatype datatype, XElement element)
        {
            string stringValue = null;
            if (datatype.TypeCode == XmlTypeCode.QName)
            {
                XmlQualifiedName qName = value as XmlQualifiedName;
                Debug.Assert(qName != null);
                stringValue = XTypedServices.QNameToString(qName, element);
            }
            else
            {
                stringValue = (string) datatype.ChangeType(value, XTypedServices.typeOfString);
            }

            return stringValue;
        }

        internal static XElement GetXElement(XTypedElement xObj, XName name)
        {
            XElement newElement = xObj.Untyped;
            if (newElement.Parent != null)
            {
                //Element/XTypedElement already added to the tree, need to clone XTypedElement
                newElement = xObj.Clone().Untyped;
            }

            IXMetaData metaData = xObj as IXMetaData;
            Debug.Assert(metaData != null);
            if (metaData.TypeOrigin == SchemaOrigin.Fragment)
            {
                //Set correct element name as the name of the property/element
                newElement.Name = name;
            }

            return newElement;
        }

        private static string ParseQName(string qName, out string prefix)
        {
            prefix = string.Empty;
            int colonPos = qName.IndexOf(':');
            if (colonPos == -1)
            {
                //the whole name is the localName and prefix is empty
                return qName;
            }
            else
            {
                prefix = qName.Substring(0, colonPos);
                return qName.Substring(colonPos + 1);
            }
        }

        internal static string QNameToString(XmlQualifiedName qName, XElement element)
        {
            Debug.Assert(qName != null);
            string prefix = element.GetPrefixOfNamespace(qName.Namespace);
            if (prefix == null || prefix.Length == 0)
            {
                return qName.Name;
            }

            return string.Concat(prefix, ":", qName.Name);
        }

        private static bool TypeValid(XTypedElement typedElement, XName instanceName)
        {
            IXMetaData metaData = typedElement as IXMetaData;
            Debug.Assert(metaData.TypeOrigin == SchemaOrigin.Element);
            if (metaData.SchemaName.Equals(instanceName))
            {
                //Element names match
                return true;
            }

            return false;
        }
    }

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

    [DebuggerTypeProxy(typeof(XListDebugVisualizer))]
    [DebuggerDisplay("Count = {((ICountAndCopy)((object)this)).Count}")]
    public abstract class XListVisualizable
    {
        internal class XListDebugVisualizer
        {
            object _xList = null;

            public XListDebugVisualizer(object xList)
            {
                _xList = xList;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public XTypedElement[] Items
            {
                get
                {
                    XTypedElement[] tArray = new XTypedElement[((ICountAndCopy) _xList).Count];
                    ((ICountAndCopy) _xList).CopyTo(tArray, 0);
                    return tArray;
                }
            }
        }
    }

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


    public class XTypedList<T> : XList<T> where T : XTypedElement
    {
        ILinqToXsdTypeManager typeManager;

        public XTypedList(XTypedElement container, XName itemXName) : this(container, null, itemXName)
        {
        }

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

        protected override XElement GetElementForValue(T value, bool createNew)
        {
            XElement element = value.Untyped;
            element.Name = itemXName;
            return element;
        }

        protected override T GetValueForElement(XElement element)
        {
            return XTypedServices.ToXTypedElement<T>(element, typeManager);
        }

        protected override void UpdateElement(XElement oldElement, T value)
        {
            oldElement.AddBeforeSelf(GetElementForValue(value, true));
            oldElement.Remove();
        }
    }

    public class XSimpleList<T> : XList<T>
    {
        XmlSchemaDatatype schemaDatatype;

        public XSimpleList(XTypedElement container, XmlSchemaDatatype dataType, XName itemXName) : base(container,
            itemXName)
        {
            this.schemaDatatype = dataType;
        }

        public override void Add(T value)
        {
            container.SetElement(itemXName, value, true, schemaDatatype);
        }

        protected override bool IsEqual(XElement element, T value)
        {
            string stringValue = element.Value;
            if (schemaDatatype.ChangeType(stringValue, typeof(T)).Equals(value))
            {
                return true;
            }

            return false;
        }

        protected override XElement GetElementForValue(T value, bool createNew)
        {
            if (createNew)
            {
                return new XElement(itemXName, XTypedServices.GetXmlString(value, schemaDatatype, containerElement));
            }

            XElement current;
            IEnumerator<XElement> listElementsEnumerator = GetListElementsEnumerator();
            while (listElementsEnumerator.MoveNext())
            {
                current = listElementsEnumerator.Current;
                if (IsEqual(current, value))
                {
                    return current;
                }
            }

            return null;
        }

        protected override T GetValueForElement(XElement element)
        {
            string stringValue = element.Value;
            return (T) schemaDatatype.ChangeType(stringValue, typeof(T));
        }

        protected override void UpdateElement(XElement oldElement, T value)
        {
            oldElement.Value = XTypedServices.GetXmlString(value, schemaDatatype, oldElement);
        }

        public static XSimpleList<T> CopyFromWithValidation(IEnumerable<T> values, XTypedElement container,
            XName itemXName, XmlSchemaDatatype dataType, string propertyName, SimpleTypeValidator typeDef)
        {
            return Initialize(container, dataType, values, itemXName);
        }

        public static XSimpleList<T> Initialize(XTypedElement container, XmlSchemaDatatype dataType,
            IEnumerable<T> values, XName itemXName)
        {
            XSimpleList<T> simpleList = new XSimpleList<T>(container, dataType, itemXName);
            simpleList.Clear();
            foreach (T value in values)
            {
                simpleList.Add(value);
            }

            return simpleList;
        }
    }

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

    internal class SubstitutionMembersList : IEnumerable<XElement>
    {
        XTypedElement container;
        XName[] namesInList;

        internal SubstitutionMembersList(XTypedElement container, params XName[] memberNames)
        {
            this.container = container;
            this.namesInList = memberNames;
        }

        public IEnumerator<XElement> GetEnumerator()
        {
            foreach (XElement childElement in container.Untyped.Elements())
            {
                for (int i = 0; i < namesInList.Length; i++)
                {
                    if (namesInList.GetValue(i).Equals(childElement.Name))
                    {
                        yield return childElement;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) GetEnumerator();
        }

        internal IEnumerator<XElement> FSMGetEnumerator()
        {
            IEnumerator<XElement> enumerator = container.Untyped.Elements().GetEnumerator();
            XElement elem = null;
            container.StartFsm();
            do
            {
                elem = container.ExecuteFSMSubGroup(enumerator, namesInList);
                if (elem != null) yield return elem;
                else yield break;
            } while (elem != null);
        }
    }

    //Using seperate annotationType object that will be added to XElement annotation 
    //Since XElement does not support looking up annotations by Super Type
    internal class XTypedElementAnnotation
    {
        internal XTypedElement typedElement;

        internal XTypedElementAnnotation(XTypedElement typedElement)
        {
            this.typedElement = typedElement;
        }
    }

    internal class XTypedElementWrapperAnnotation
    {
        //Seperate class for annotating root elements
        internal XTypedElement typedElement;

        internal XTypedElementWrapperAnnotation(XTypedElement typedElement)
        {
            this.typedElement = typedElement;
        }
    }
}