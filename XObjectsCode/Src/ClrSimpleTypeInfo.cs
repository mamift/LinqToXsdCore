//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Xml.Schema;
using System.Collections.Generic;
using System.Diagnostics;

namespace Xml.Schema.Linq.CodeGen
{
    //Special casing types with valid data type
    internal abstract class ClrSimpleTypeInfo : ClrTypeInfo
    {
        XmlSchemaType innerType;
        XmlSchemaDatatypeVariety variety;

        internal ClrSimpleTypeInfo(XmlSchemaType innerType)
        {
            this.innerType = innerType;
            this.variety = innerType.Datatype.Variety;
        }

        internal CompiledFacets RestrictionFacets
        {
            get { return GetFacets(innerType); }
        }

        public XmlTypeCode TypeCode
        {
            get { return innerType.Datatype.TypeCode; }
        }

        public XmlSchemaDatatype Datatype
        {
            get { return innerType.Datatype; }
        }

        public XmlSchemaDatatypeVariety Variety
        {
            get { return variety; }
        }

        internal XmlSchemaType InnerType
        {
            get { return innerType; }
            set { innerType = value; }
        }

        internal bool IsGlobal
        {
            get
            {
                XmlSchemaSimpleType st = innerType as XmlSchemaSimpleType;
                if (st != null)
                {
                    return !st.IsBuiltInSimpleType() && !st.QualifiedName.IsEmpty;
                }

                return false;
            }
        }

        private static CompiledFacets GetFacets(XmlSchemaType type)
        {
            CompiledFacets compiledFacets = new CompiledFacets(type.Datatype);
            XmlSchemaSimpleType simpleType = type as XmlSchemaSimpleType;

            if (simpleType != null)
            {
                compiledFacets.compileFacets(simpleType);
            }

            return compiledFacets;
        }

        internal static ClrSimpleTypeInfo CreateSimpleTypeInfo(XmlSchemaType type)
        {
            ClrSimpleTypeInfo typeInfo = null;

            Debug.Assert(type.Datatype != null);
            switch (type.Datatype.Variety)
            {
                case XmlSchemaDatatypeVariety.Atomic:
                    typeInfo = new AtomicSimpleTypeInfo(type);
                    break;
                case XmlSchemaDatatypeVariety.List:
                    typeInfo = new ListSimpleTypeInfo(type);
                    break;
                case XmlSchemaDatatypeVariety.Union:
                    typeInfo = new UnionSimpleTypeInfo(type);
                    break;
                default:
                    break;
            }

            return typeInfo;
        }

        internal void UpdateClrTypeName(Dictionary<XmlSchemaObject, string> nameMappings,
            LinqToXsdSettings settings)
        {
            string identifier = null;
            string typeName = innerType.QualifiedName.Name;
            string clrNameSpace = settings.GetClrNamespace(innerType.QualifiedName.Namespace);
            if (nameMappings.TryGetValue(innerType, out identifier))
            {
                clrtypeName = identifier;
            }
            else
            {
                clrtypeName = typeName;
            }

            if (clrNameSpace != string.Empty)
            {
                clrtypeName = clrNameSpace + "." + clrtypeName;
            }
        }
    }

    internal class AtomicSimpleTypeInfo : ClrSimpleTypeInfo
    {
        internal AtomicSimpleTypeInfo(XmlSchemaType innerType)
            : base(innerType)
        {
        }
    }

    internal class ListSimpleTypeInfo : ClrSimpleTypeInfo
    {
        ClrSimpleTypeInfo itemType;

        internal ListSimpleTypeInfo(XmlSchemaType innerType) : base(innerType)
        {
        }

        public ClrSimpleTypeInfo ItemType
        {
            get
            {
                if (itemType == null)
                {
                    XmlSchemaSimpleType st = InnerType as XmlSchemaSimpleType;
                    if (st == null)
                    {
                        XmlSchemaComplexType ct = InnerType as XmlSchemaComplexType;
                        st = ct.GetBaseSimpleType();
                    }

                    Debug.Assert(st.Datatype.Variety == XmlSchemaDatatypeVariety.List);
                    itemType = CreateSimpleTypeInfo(st.GetListItemType());
                }

                return itemType;
            }
        }
    }

    internal class UnionSimpleTypeInfo : ClrSimpleTypeInfo
    {
        ClrSimpleTypeInfo[] memberTypes;

        internal UnionSimpleTypeInfo(XmlSchemaType innerType) : base(innerType)
        {
        }

        public ClrSimpleTypeInfo[] MemberTypes
        {
            get
            {
                if (memberTypes == null)
                {
                    XmlSchemaSimpleType st = InnerType as XmlSchemaSimpleType;
                    if (st == null)
                    {
                        XmlSchemaComplexType ct = InnerType as XmlSchemaComplexType;
                        st = ct.GetBaseSimpleType();
                    }

                    Debug.Assert(st.Datatype.Variety == XmlSchemaDatatypeVariety.Union);
                    XmlSchemaSimpleType[] innerMemberTypes = st.GetUnionMemberTypes();

                    memberTypes = new ClrSimpleTypeInfo[innerMemberTypes.Length];
                    for (int i = 0; i < innerMemberTypes.Length; i++)
                    {
                        memberTypes[i] = CreateSimpleTypeInfo(innerMemberTypes[i]);
                    }
                }

                return memberTypes;
            }
        }
    }
}