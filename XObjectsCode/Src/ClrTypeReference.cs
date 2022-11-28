using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Schema;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.CodeGen;

internal partial class ClrTypeReference
{
    string typeName;
    string typeNs;
    string clrName;
    string clrFullTypeName;

    string typeCodeString;
    XmlSchemaObject schemaObject;
    ClrTypeRefFlags typeRefFlags;
    SchemaOrigin typeRefOrigin;

    internal ClrTypeReference(string name, string ns, XmlSchemaObject schemaObject, bool anonymousType,
        bool setVariety)
    {
        this.typeName = name;
        this.typeNs = ns;
        this.schemaObject = schemaObject;

        XmlSchemaType schemaType = schemaObject as XmlSchemaType;
        if (schemaType == null)
        {
            XmlSchemaElement elem = schemaObject as XmlSchemaElement;
            typeRefFlags |= ClrTypeRefFlags.IsElementRef;
            schemaType = elem.ElementSchemaType;
        }

        Debug.Assert(schemaType != null);

        XmlSchemaSimpleType st = schemaType as XmlSchemaSimpleType;
        if (st != null)
        {
            if (st.HasFacetRestrictions() || st.IsOrHasUnion())
            {
                typeRefFlags |= ClrTypeRefFlags.Validate;
            }

            if (st.IsEnum())
            {
                typeRefFlags |= ClrTypeRefFlags.IsEnum | ClrTypeRefFlags.IsValueType;
            }

            XmlSchemaDatatype datatype = st.Datatype;
            if (setVariety)
            {
                SetVariety(datatype);
            }

            typeRefFlags |= ClrTypeRefFlags.IsSimpleType;
            typeCodeString = datatype.TypeCodeString();
            if (datatype.ValueType.IsValueType)
            {
                typeRefFlags |= ClrTypeRefFlags.IsValueType;
            }
        }
        else if (schemaType.TypeCode == XmlTypeCode.Item)
        {
            typeRefFlags |= ClrTypeRefFlags.IsAnyType;
        }

        if (anonymousType)
        {
            typeRefFlags |= ClrTypeRefFlags.IsLocalType;
        }

        this.typeRefOrigin = SchemaOrigin.Fragment;
    }

    private void SetVariety(XmlSchemaDatatype datatype)
    {
        XmlSchemaDatatypeVariety variety = datatype.Variety;
        if (variety == XmlSchemaDatatypeVariety.List)
        {
            typeRefFlags |= ClrTypeRefFlags.IsSchemaList;
        }
        else if (variety == XmlSchemaDatatypeVariety.Union)
        {
            typeRefFlags |= ClrTypeRefFlags.IsUnion;
        }
    }

    internal SchemaOrigin Origin
    {
        get { return typeRefOrigin; }
        set { typeRefOrigin = value; }
    }

    internal string Name
    {
        get { return typeName; }
        set
        {
            typeName = value; //needed to fixup typename of anonymous types 
        }
    }

    public string ClrName
    {
        get { return clrName; }
    }

    public string Namespace
    {
        get { return typeNs; }
    }

    public string ClrFullTypeName
    {
        get { return clrFullTypeName; }
    }

    internal string TypeCodeString
    {
        get { return typeCodeString; }
    }

    internal bool IsValueType
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsValueType) != 0; }
    }

    internal bool IsLocalType
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsLocalType) != 0; }
    }

    internal bool IsSimpleType
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsSimpleType) != 0; }
    }

    internal bool Validate
    {
        get { return (typeRefFlags & ClrTypeRefFlags.Validate) != 0; }
        set
        {
            if (value)
            {
                typeRefFlags |= ClrTypeRefFlags.Validate;
            }
            else
            {
                typeRefFlags &= ~ClrTypeRefFlags.Validate;
            }
        }
    }

    internal bool IsTypeRef
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsElementRef) != 0; }
        set
        {
            if (value)
            {
                typeRefFlags |= ClrTypeRefFlags.IsElementRef;
            }
            else
            {
                typeRefFlags &= ~ClrTypeRefFlags.IsElementRef;
            }
        }
    }

    internal bool IsSchemaList
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsSchemaList) != 0; }
    }

    internal bool IsUnion
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsUnion) != 0; }
    }

    internal bool IsEnum
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsEnum) != 0; }
    }

    internal bool IsAnyType
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsAnyType) != 0; }
    }

    internal bool IsNamedComplexType
    {
        get
        {
            return (typeRefFlags & ClrTypeRefFlags.IsSimpleType) == 0 &&
                   (typeRefFlags & ClrTypeRefFlags.IsAnyType) == 0;
        }
    }

    internal XmlSchemaObject SchemaObject
    {
        get { return schemaObject; }
    }

    internal string LocalSuffix => this.IsEnum ? Constants.LocalEnumSuffix : Constants.LocalTypeSuffix;

    internal string GetSimpleTypeClrTypeDefName(string parentTypeClrNs,
        Dictionary<XmlSchemaObject, string> nameMappings)
    {
        Debug.Assert(this.IsSimpleType);
        string clrTypeName = null;
        XmlSchemaObject key = schemaObject;
        if (IsTypeRef)
        {
            //schema object is element
            key = ((XmlSchemaElement) schemaObject).ElementSchemaType as XmlSchemaSimpleType;
            Debug.Assert(key != null);
        }

        string identifier = null;
        if (nameMappings.TryGetValue(key, out identifier))
        {
            clrTypeName = identifier;
        }
        else
        {
            clrTypeName = typeName;
        }

        this.clrName = clrTypeName;

        if (IsEnum && !string.IsNullOrEmpty(clrTypeName))
        {
            clrTypeName += Constants.EnumValidator;
        }

        if (typeNs != string.Empty && !IsLocalType)
        {
            //Namespace of the property's type is different than the namespace of the enclosing CLR Type
            clrTypeName = "global::" + typeNs + "." + clrTypeName;
        }

        return clrTypeName;
    }

    internal string GetClrFullTypeName(string parentTypeClrNs, Dictionary<XmlSchemaObject, string> nameMappings,
        out string refTypeName)
    {
        string clrTypeName = null;
        refTypeName = null;
        if (IsNamedComplexType || IsTypeRef)
        {
            clrTypeName = nameMappings.TryGetValue(schemaObject, out string identifier) ? identifier : typeName;
            refTypeName = clrTypeName;
            if (typeNs != string.Empty && (typeNs != parentTypeClrNs || nameMappings.Values.Where(v => v == clrTypeName).Skip(1).Any()))
            {
                //Keep the full type name to avoid conflicts when we have types with the same name in different namespaces.
                clrTypeName = typeNs + "." + clrTypeName;
            }
        }
        else if (IsAnyType)
        {
            clrTypeName = Constants.XTypedElement;
        }
        else
        {
            //its a simple type - get default xsd -> clr mapping
            Debug.Assert(IsSimpleType);
            XmlSchemaSimpleType st = schemaObject as XmlSchemaSimpleType;
            Debug.Assert(st != null);
            clrTypeName = IsSchemaList
                ? st.GetListItemType().Datatype.ValueType.ToString()
                : st.Datatype.ValueType.ToString();
        }

        return clrTypeName;
    }

    internal string UpdateClrFullEnumTypeName(ClrPropertyInfo property, string currentTypeScope, string currentNamespaceScope)
    {
        Debug.Assert(this.IsEnum);

        var theClrNamespace = EnsureNamespace();
        var noNamespaceDefined = this.Namespace.IsNullOrEmpty() && property.ClrNamespace.IsNullOrEmpty() && theClrNamespace.IsNullOrEmpty();
        if (noNamespaceDefined)
        {
            this.clrFullTypeName = this.clrName;
        }
        else if (this.clrFullTypeName.IsEmpty())
        {
            //If the enum type is local (nested), use its parent type scope
            this.clrFullTypeName = property.TypeReference.IsLocalType
                ? $"{currentTypeScope}.{this.ClrName ?? this.Name}"
                : $"{theClrNamespace}.{this.ClrName ?? this.Name}";
        }
        return this.clrFullTypeName;

        string EnsureNamespace()
        {
            if (this.Namespace.IsNullOrEmpty())
            {
                if (currentNamespaceScope == null) throw new ArgumentNullException(nameof(currentNamespaceScope));
                return currentNamespaceScope;
            }
            return this.Namespace;
        }
    }
}