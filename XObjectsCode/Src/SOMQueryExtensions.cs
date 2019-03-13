//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;

namespace Xml.Schema.Linq.CodeGen
{
    internal static class SOMQueryExtensions
    {
        //XmlSchemaType helpers
        public static XmlSchemaContentType GetContentType(this XmlSchemaType schemaType)
        {
            if (schemaType == null)
            {
                return XmlSchemaContentType.Empty;
            }

            if (schemaType.Datatype != null)
            {
                return XmlSchemaContentType.TextOnly;
            }

            XmlSchemaComplexType ct = schemaType as XmlSchemaComplexType;
            Debug.Assert(ct != null);
            return ct.ContentType;
        }

        public static bool IsGlobal(this XmlSchemaType schemaType)
        {
            if (!schemaType.QualifiedName.IsEmpty && schemaType.TypeCode != XmlTypeCode.Item)
            {
                return true;
            }

            return false;
        }

        public static bool FromSchemaNamespace(this XmlSchemaType schemaType)
        {
            if (schemaType.QualifiedName.Namespace == XmlSchema.Namespace)
            {
                return true;
            }

            return false;
        }

        public static bool IsDerivedByRestriction(this XmlSchemaType derivedType)
        {
            XmlSchemaComplexType ct = derivedType as XmlSchemaComplexType;
            if (ct != null)
            {
                return ct.IsDerivedByRestriction();
            }
            else
            {
                XmlSchemaSimpleType st = derivedType as XmlSchemaSimpleType;
                return st.IsDerivedByRestriction();
            }
        }

        public static bool IsDerivedByRestriction(this XmlSchemaComplexType derivedType)
        {
            if (derivedType.DerivedBy == XmlSchemaDerivationMethod.Restriction
                && derivedType.BaseXmlSchemaType != XmlSchemaType.GetBuiltInComplexType(XmlTypeCode.Item))
            {
                return true;
            }

            return false;
        }

        public static bool IsDerivedByRestriction(this XmlSchemaSimpleType derivedType)
        {
            if ((derivedType.DerivedBy == XmlSchemaDerivationMethod.Restriction)
                && !derivedType.IsBuiltInSimpleType())
            {
                return true;
            }

            return false;
        }

        public static bool IsBuiltInSimpleType(this XmlSchemaSimpleType derivedType)
        {
            if (derivedType.QualifiedName.Namespace == XmlSchema.Namespace)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static XmlSchemaSimpleType GetListItemType(this XmlSchemaSimpleType type)
        {
            Debug.Assert(type != null);
            Debug.Assert(type.Datatype.Variety == XmlSchemaDatatypeVariety.List);

            XmlSchemaSimpleTypeList listContent = type.Content as XmlSchemaSimpleTypeList;

            if (listContent != null)
            {
                //directly derived by list
                return (XmlSchemaSimpleType) listContent.BaseItemType;
            }
            else
            {
                //could derive by restriction to enforce some restrictions on the list
                Debug.Assert(type.DerivedBy == XmlSchemaDerivationMethod.Restriction);
                XmlSchemaSimpleType baseType = type.BaseXmlSchemaType as XmlSchemaSimpleType;
                return (XmlSchemaSimpleType) baseType.GetListItemType();
            }
        }

        public static bool IsOrHasUnion(this XmlSchemaSimpleType type)
        {
            switch (type.Datatype.Variety)
            {
                case XmlSchemaDatatypeVariety.Atomic: return false;
                case XmlSchemaDatatypeVariety.List:
                    return type.GetListItemType().IsOrHasUnion();
                case XmlSchemaDatatypeVariety.Union:
                    return true;

                default:
                    throw new InvalidOperationException("Unknown type variety");
            }
        }

        public static XmlSchemaWhiteSpace GetBuiltInWSFacet(this XmlSchemaDatatype dt)
        {
            if (dt.TypeCode == XmlTypeCode.NormalizedString)
            {
                return XmlSchemaWhiteSpace.Replace;
            }
            else if (dt.TypeCode == XmlTypeCode.String)
            {
                return XmlSchemaWhiteSpace.Preserve;
            }
            else
                return XmlSchemaWhiteSpace.Collapse;
        }

        public static XmlSchemaSimpleType GetBaseSimpleType(this XmlSchemaComplexType type)
        {
            XmlSchemaType baseType = type.BaseXmlSchemaType;

            while (baseType != null && baseType is XmlSchemaComplexType)
            {
                baseType = baseType.BaseXmlSchemaType;
            }

            if (baseType == null)
            {
                baseType = XmlSchemaType.GetBuiltInSimpleType(type.TypeCode);
            }

            return baseType as XmlSchemaSimpleType;
        }

        public static bool IsOrHasList(this XmlSchemaSimpleType type)
        {
            switch (type.Datatype.Variety)
            {
                case XmlSchemaDatatypeVariety.Atomic: return false;
                case XmlSchemaDatatypeVariety.List:
                    return true;
                case XmlSchemaDatatypeVariety.Union:
                    foreach (XmlSchemaSimpleType memberType in type.GetUnionMemberTypes())
                    {
                        if (memberType.IsOrHasList())
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    throw new InvalidOperationException("Unknown type variety");
            }
        }

        public static XmlSchemaSimpleType[] GetUnionMemberTypes(this XmlSchemaSimpleType type)
        {
            Debug.Assert(type != null);
            Debug.Assert(type.Datatype.Variety == XmlSchemaDatatypeVariety.Union);

            XmlSchemaSimpleTypeUnion unionContent = type.Content as XmlSchemaSimpleTypeUnion;

            if (unionContent != null)
            {
                //directly derived by union
                return unionContent.BaseMemberTypes;
            }
            else
            {
                Debug.Assert(type.DerivedBy == XmlSchemaDerivationMethod.Restriction);
                XmlSchemaSimpleType baseType = type.BaseXmlSchemaType as XmlSchemaSimpleType;
                return baseType.GetUnionMemberTypes();
            }
        }

        public static bool HasFacetRestrictions(this XmlSchemaSimpleType sst)
        {
            if (sst.IsDerivedByRestriction())
            {
                return true;
            }
            else if (sst.Datatype.Variety == XmlSchemaDatatypeVariety.List)
            {
                //If it is a list, also check item type
                return sst.GetListItemType().HasFacetRestrictions();
            }
            else if (sst.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
            {
                foreach (XmlSchemaSimpleType memberType in sst.GetUnionMemberTypes())
                {
                    if (memberType.HasFacetRestrictions())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasFacetRestrictions(this XmlSchemaComplexType ct)
        {
            if (ct.GetContentType() == XmlSchemaContentType.TextOnly)
            {
                XmlSchemaSimpleType baseType = ct.BaseXmlSchemaType as XmlSchemaSimpleType;
                if (baseType != null)
                {
                    //Derived from Simple type, first step simpleContent extension
                    return baseType.HasFacetRestrictions();
                }
                else if (ct.IsDerivedByRestriction())
                {
                    //derived from another complex type, simple content restriction
                    return true;
                }
            }

            return false;
        }

        public static bool IsGlobal(this XmlSchemaElement elem)
        {
            if (!elem.RefName.IsEmpty)
            {
                return true;
            }

            XmlSchemaObject parent = elem.Parent;
            if (parent is XmlSchema)
            {
                return true;
            }

            return false;
        }

        public static XmlSchemaForm FormResolved(this XmlSchemaElement elem)
        {
            if (!elem.RefName.IsEmpty)
                return XmlSchemaForm.Qualified;

            XmlSchemaForm form = elem.Form;
            if (form == XmlSchemaForm.None)
            {
                XmlSchema parentSchema = GetParentSchema(elem);
                return parentSchema.ElementFormDefault;
            }

            return form;
        }

        public static XmlSchemaForm FormResolved(this XmlSchemaAttribute attribute)
        {
            XmlSchemaForm form = attribute.Form;
            if (form == XmlSchemaForm.None)
            {
                XmlSchema parentSchema = GetParentSchema(attribute);
                return parentSchema.AttributeFormDefault;
            }

            return form;
        }

        public static XmlSchema GetParentSchema(XmlSchemaObject currentSchemaObject)
        {
            XmlSchema parentSchema = null;
            while (parentSchema == null && currentSchemaObject != null)
            {
                currentSchemaObject = currentSchemaObject.Parent;
                parentSchema = currentSchemaObject as XmlSchema;
            }

            return parentSchema;
        }

        //XmlSchemaParticle helpers
        public static ParticleType GetParticleType(this XmlSchemaParticle particle)
        {
            if (particle is XmlSchemaElement)
            {
                return ParticleType.Element;
            }
            else if (particle is XmlSchemaSequence)
            {
                return ParticleType.Sequence;
            }
            else if (particle is XmlSchemaChoice)
            {
                return ParticleType.Choice;
            }
            else if (particle is XmlSchemaAll)
            {
                return ParticleType.All;
            }
            else if (particle is XmlSchemaAny)
            {
                return ParticleType.Any;
            }
            else if (particle is XmlSchemaGroupRef)
            {
                return ParticleType.GroupRef;
            }
            else
            {
                return ParticleType.Empty;
            }
        }

        public static bool Contains(this XmlSchemaParticle particle, XmlSchemaParticle childParticle)
        {
            XmlSchemaGroupBase groupBase = particle as XmlSchemaGroupBase;
            if (groupBase != null)
            {
                foreach (XmlSchemaParticle item in groupBase.Items)
                {
                    if (item == childParticle)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool ContainsElement(this XmlSchemaParticle particle, XmlSchemaElement element)
        {
            XmlSchemaGroupBase groupBase = particle as XmlSchemaGroupBase;
            if (groupBase != null)
            {
                foreach (XmlSchemaParticle p in groupBase.Items)
                {
                    if (p == element)
                    {
                        return true;
                    }
                    else if (p.ContainsElement(element))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool ContainsName(this XmlSchemaParticle particle, XmlQualifiedName elementName)
        {
            XmlSchemaGroupBase groupBase = particle as XmlSchemaGroupBase;
            if (groupBase != null)
            {
                foreach (XmlSchemaParticle p in groupBase.Items)
                {
                    XmlSchemaElement localElement = p as XmlSchemaElement;
                    if (localElement != null)
                    {
                        if (localElement.QualifiedName == elementName)
                            return true;
                    }
                    else if (p.ContainsName(elementName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool ContainsName(this XmlSchemaComplexType baseType, XmlQualifiedName elementName)
        {
            return baseType.ContentTypeParticle.ContainsName(elementName);
        }

        public static bool ContainsWildCard(this XmlSchemaParticle particle, XmlSchemaAny any)
        {
            XmlSchemaGroupBase groupBase = particle as XmlSchemaGroupBase;
            if (groupBase != null)
            {
                foreach (XmlSchemaParticle p in groupBase.Items)
                {
                    if (p == any)
                    {
                        return true;
                    }
                    else if (p.ContainsWildCard(any))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GetTargetNS(this XmlSchemaAny any)
        {
            XmlSchemaObject parentObj = any.Parent;
            XmlSchema schemaObj = parentObj as XmlSchema;
            while (schemaObj == null)
            {
                if (parentObj == null) break;
                parentObj = parentObj.Parent;
                schemaObj = parentObj as XmlSchema;
            }

            if (schemaObj == null) return "";
            else return schemaObj.TargetNamespace;
        }

        public static bool IsFinal(this XmlSchemaComplexType ct)
        {
            if (ct.FinalResolved == XmlSchemaDerivationMethod.All ||
                ct.FinalResolved == XmlSchemaDerivationMethod.Extension ||
                ct.FinalResolved == XmlSchemaDerivationMethod.Restriction)
            {
                return true;
            }

            return false;
        }

        public static string TypeCodeString(this XmlSchemaDatatype datatype)
        {
            string typeCodeString = string.Empty;
            XmlTypeCode typeCode = datatype.TypeCode;
            switch (typeCode)
            {
                case XmlTypeCode.None:
                    return "None";
                case XmlTypeCode.Item:
                    return "AnyType";
                case XmlTypeCode.AnyAtomicType:
                    return "AnyAtomicType";
                case XmlTypeCode.String:
                    return "String";
                case XmlTypeCode.Boolean:
                    return "Boolean";
                case XmlTypeCode.Decimal:
                    return "Decimal";
                case XmlTypeCode.Float:
                    return "Float";
                case XmlTypeCode.Double:
                    return "Double";
                case XmlTypeCode.Duration:
                    return "Duration";
                case XmlTypeCode.DateTime:
                    return "DateTime";
                case XmlTypeCode.Time:
                    return "Time";
                case XmlTypeCode.Date:
                    return "Date";
                case XmlTypeCode.GYearMonth:
                    return "GYearMonth";
                case XmlTypeCode.GYear:
                    return "GYear";
                case XmlTypeCode.GMonthDay:
                    return "GMonthDay";
                case XmlTypeCode.GDay:
                    return "GDay";
                case XmlTypeCode.GMonth:
                    return "GMonth";
                case XmlTypeCode.HexBinary:
                    return "HexBinary";
                case XmlTypeCode.Base64Binary:
                    return "Base64Binary";
                case XmlTypeCode.AnyUri:
                    return "AnyUri";
                case XmlTypeCode.QName:
                    return "QName";
                case XmlTypeCode.Notation:
                    return "Notation";
                case XmlTypeCode.NormalizedString:
                    return "NormalizedString";
                case XmlTypeCode.Token:
                    return "Token";
                case XmlTypeCode.Language:
                    return "Language";
                case XmlTypeCode.NmToken:
                    return "NmToken";
                case XmlTypeCode.Name:
                    return "Name";
                case XmlTypeCode.NCName:
                    return "NCName";
                case XmlTypeCode.Id:
                    return "Id";
                case XmlTypeCode.Idref:
                    return "Idref";
                case XmlTypeCode.Entity:
                    return "Entity";
                case XmlTypeCode.Integer:
                    return "Integer";
                case XmlTypeCode.NonPositiveInteger:
                    return "NonPositiveInteger";
                case XmlTypeCode.NegativeInteger:
                    return "NegativeInteger";
                case XmlTypeCode.Long:
                    return "Long";
                case XmlTypeCode.Int:
                    return "Int";
                case XmlTypeCode.Short:
                    return "Short";
                case XmlTypeCode.Byte:
                    return "Byte";
                case XmlTypeCode.NonNegativeInteger:
                    return "NonNegativeInteger";
                case XmlTypeCode.UnsignedLong:
                    return "UnsignedLong";
                case XmlTypeCode.UnsignedInt:
                    return "UnsignedInt";
                case XmlTypeCode.UnsignedShort:
                    return "UnsignedShort";
                case XmlTypeCode.UnsignedByte:
                    return "UnsignedByte";
                case XmlTypeCode.PositiveInteger:
                    return "PositiveInteger";

                default:
                    return typeCode.ToString();
            }
        }
    }
}