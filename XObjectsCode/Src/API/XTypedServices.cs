using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
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
        public static T ToXTypedElement<T>(XElement xe) where T : XTypedElement
        {
            if (xe == null)
            {
                return null;
            }

            T xoSubType = GetAnnotation<T>(xe);
            if (xoSubType == null)
            {
                //No association bet XTypedElement and xelement
                xoSubType = Activator.CreateInstance<T>();
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

        public static T CloneXTypedElement<T>(T xTypedElement) where T : XTypedElement
        {
            if (xTypedElement == null)
            {
                throw new ArgumentNullException("Argument xTypedElement should not be null.");
            }

            XElement clonedElement = new XElement(xTypedElement.Untyped);
            T newObject = (T)Activator.CreateInstance(typeof(T), nonPublic: true); // this allows cloning XTypeElements that are internal
            newObject.Untyped = clonedElement;
            clonedElement.AddAnnotation(
                new XTypedElementAnnotation(newObject)); //Need to set up association for the cloned type
            return newObject;
        }

        public static T Load<T>(System.IO.TextReader reader) where T : XTypedElement
        {
            XDocument doc = XDocument.Load(reader);
            XElement xeroot = doc.Root;
            return ToXTypedElement<T>(xeroot);
        }

        public static T Load<T>(string uri) where T : XTypedElement
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

        public static T Parse<T>(string xml) where T : XTypedElement
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
}