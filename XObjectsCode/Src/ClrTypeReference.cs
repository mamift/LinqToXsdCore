#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq.CodeGen;

public partial class ClrTypeReference
{
    string typeName;
    string typeNs;
    string? clrName;
    string? clrFullTypeName;

    string typeCodeString;
    XmlSchemaObject schemaObject;
    ClrTypeRefFlags typeRefFlags;
    SchemaOrigin typeRefOrigin;

    public ClrTypeReference(string name, string ns, XmlSchemaObject schemaObject, bool anonymousType,
        bool setVariety)
    {
        this.typeName = name;
        this.typeNs = ns;
        this.schemaObject = schemaObject;
        this.typeRefOrigin = SchemaOrigin.Fragment;

        SetTypeRefFlags(anonymousType, setVariety);
    }

    private void SetTypeRefFlags(bool anonymousType, bool setVariety)
    {
        Debug.Assert(schemaObject != null);
        XmlSchemaType schemaType = this.schemaObject as XmlSchemaType;
        if (schemaType == null)
        {
            XmlSchemaElement elem = this.schemaObject as XmlSchemaElement;
            this.typeRefFlags |= ClrTypeRefFlags.IsElementRef;
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
            Debug.Assert(datatype != null, nameof(datatype) + " != null");
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

    public SchemaOrigin Origin
    {
        get { return typeRefOrigin; }
        set { typeRefOrigin = value; }
    }

    public string Name
    {
        get { return typeName; }
        set
        {
            typeName = value; //needed to fixup typename of anonymous types
        }
    }

    public string? ClrName
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

    public string TypeCodeString
    {
        get { return typeCodeString; }
    }

    public bool IsValueType
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsValueType) != 0; }
    }

    public bool IsLocalType
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsLocalType) != 0; }
    }

    public bool IsSimpleType
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsSimpleType) != 0; }
    }

    public bool Validate
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

    public bool IsTypeRef
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

    public bool IsSchemaList
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsSchemaList) != 0; }
    }

    public bool IsUnion
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsUnion) != 0; }
    }

    public bool IsEnum
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsEnum) != 0; }
    }

    public bool IsAnyType
    {
        get { return (typeRefFlags & ClrTypeRefFlags.IsAnyType) != 0; }
    }

    public bool IsNamedComplexType
    {
        get
        {
            return (typeRefFlags & ClrTypeRefFlags.IsSimpleType) == 0 &&
                   (typeRefFlags & ClrTypeRefFlags.IsAnyType) == 0;
        }
    }

    public XmlSchemaObject SchemaObject
    {
        get { return schemaObject; }
    }

    public string LocalSuffix => this.IsEnum ? Constants.LocalEnumSuffix : Constants.LocalTypeSuffix;

    public string GetSimpleTypeClrTypeDefName(string parentTypeClrNs, Dictionary<XmlSchemaObject, string> nameMappings)
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

        // unfortunately this doesn't handle simple type unions
        if (clrTypeName.IsEmpty() && key is XmlSchemaSimpleType { Content: XmlSchemaSimpleTypeUnion union }) 
        {
            clrTypeName = typeof(object).FullName;
        }

        return clrTypeName;
    }

    public string GetClrFullTypeName(
        string parentTypeClrNs,
        Dictionary<XmlSchemaObject, string> nameMappings,
        LinqToXsdSettings settings,
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
            var st = schemaObject as XmlSchemaSimpleType;
            Debug.Assert(st != null);

            var schemaType = IsSchemaList ? st.GetListItemType().Datatype : st.Datatype;

            clrTypeName = schemaType.TypeCode switch
            {
                XmlTypeCode.Date when settings.UseDateOnly => "System.DateOnly",
                XmlTypeCode.Time when settings.UseTimeOnly => "System.TimeOnly",
                XmlTypeCode.DateTime when settings.UseDateTimeOffset => "System.DateTimeOffset",
                _ => schemaType.ValueType.ToString(),
            };
        }

        return clrTypeName;
    }

    internal string? UpdateClrFullEnumTypeName(ClrPropertyInfo property, string currentTypeScope, string currentNamespaceScope)
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
            if (property.TypeReference.IsLocalType)
                this.clrFullTypeName = $"{currentTypeScope}.{this.ClrName ?? this.Name}";
            else
                this.clrFullTypeName = $"{theClrNamespace}.{this.ClrName ?? this.Name}";
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

    public bool IsForAnonymousXsdType
    {
        get {
            return (schemaObject as XmlSchemaType)?.IsAnonymous() == true;
        }
    }

    /// <summary>
    /// When this type reference is for a <see cref="XmlSchemaSimpleTypeUnion"/>, this property returns all the member types.
    /// </summary>
    public XmlSchemaObject[]? UnionMemberTypes
    {
        get {
            Debug.Assert(typeRefFlags.HasFlag(ClrTypeRefFlags.IsUnion));

            if (schemaObject is XmlSchemaSimpleType { Content: XmlSchemaSimpleTypeUnion union }) {
                XmlSchemaSimpleType[] xmlSchemaSimpleTypes = union.GetUnionMemberTypes();
                return xmlSchemaSimpleTypes.Cast<XmlSchemaObject>().ToArray();
            }

            return null;
        }
    }

    /// <summary>
    /// When this type reference is for a <see cref="XmlSchemaChoice"/> particle, this property returns all the possible choices.
    /// </summary>
    public XmlSchemaObject[]? ChoiceMemberTypes
    {
        get {
            if (schemaObject is XmlSchemaComplexType type) {
                if (type.ContentModel is XmlSchemaComplexContent complexContent) {
                    if (complexContent.Content is XmlSchemaComplexContentRestriction complexContentRestriction) {
                        if (complexContentRestriction.Particle is XmlSchemaChoice choice) {
                            return choice.Items.Cast<XmlSchemaObject>().ToArray();
                        }
                    }

                    if (complexContent.Content is XmlSchemaComplexContentExtension complexContentExtension) {
                        if (complexContentExtension.Particle is XmlSchemaChoice choice) {
                            return choice.Items.Cast<XmlSchemaObject>().ToArray();
                        }
                    }
                }
            }

            return null;
        }
    }

    public XDocument ToXDoc()
    {
        return new XDocument(
            new XElement(nameof(ClrTypeReference),
                new XElement(nameof(Origin), Origin),
                new XElement(nameof(Name), Name),
                new XElement(nameof(ClrName), ClrName),
                new XElement(nameof(Namespace), Namespace),
                new XElement(nameof(ClrFullTypeName), ClrFullTypeName),
                new XElement(nameof(TypeCodeString), TypeCodeString),
                new XElement(nameof(IsValueType), IsValueType),
                new XElement(nameof(IsLocalType), IsLocalType),
                new XElement(nameof(IsSimpleType), IsSimpleType),
                new XElement(nameof(Validate), Validate),
                new XElement(nameof(IsTypeRef), IsTypeRef),
                new XElement(nameof(IsSchemaList), IsSchemaList),
                new XElement(nameof(IsUnion), IsUnion),
                new XElement(nameof(IsEnum), IsEnum),
                new XElement(nameof(IsAnyType), IsAnyType),
                new XElement(nameof(IsNamedComplexType), IsNamedComplexType),
                new XElement(nameof(SchemaObject), SchemaObject?.ToString()),
                new XElement(nameof(LocalSuffix), LocalSuffix)
            ));
    }
}