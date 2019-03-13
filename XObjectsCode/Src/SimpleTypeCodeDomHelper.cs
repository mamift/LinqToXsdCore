//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.CodeDom;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;

namespace Xml.Schema.Linq.CodeGen
{
    class SimpleTypeCodeDomHelper
    {
        internal static CodeExpression CreateSimpleTypeDef(ClrSimpleTypeInfo typeInfo,
            Dictionary<XmlSchemaObject, string> nameMappings,
            LinqToXsdSettings settings, bool memberOrItemType)
        {
            //If the enclosed member type or item type is a global named type, reuse the definition
            if (memberOrItemType && typeInfo.IsGlobal)
            {
                typeInfo.UpdateClrTypeName(nameMappings, settings);
                return CodeDomHelper.CreateFieldReference(typeInfo.clrtypeName, Constants.SimpleTypeDefInnerType);
            }
            else
            {
                return MaterializeSimpleTypeDef(typeInfo, nameMappings, settings);
            }
        }

        internal static CodeExpression MaterializeSimpleTypeDef(ClrSimpleTypeInfo typeInfo,
            Dictionary<XmlSchemaObject, string> nameMappings,
            LinqToXsdSettings settings)
        {
            CodeObjectCreateExpression simpleTypeCreate = null;
            CodeExpressionCollection expressions = null;
            switch (typeInfo.Variety)
            {
                case XmlSchemaDatatypeVariety.Atomic:
                    simpleTypeCreate = new CodeObjectCreateExpression(Constants.AtomicSimpleTypeValidator);
                    expressions = simpleTypeCreate.Parameters;
                    expressions.Add(CreateGetBuiltInSimpleType(typeInfo.TypeCode));
                    expressions.Add(CreateFacets(typeInfo));
                    break;

                case XmlSchemaDatatypeVariety.List:
                    simpleTypeCreate = new CodeObjectCreateExpression(Constants.ListSimpleTypeValidator);
                    expressions = simpleTypeCreate.Parameters;
                    expressions.Add(CreateGetBuiltInSimpleType(typeInfo.TypeCode));
                    expressions.Add(CreateFacets(typeInfo));

                    ListSimpleTypeInfo listType = typeInfo as ListSimpleTypeInfo;
                    ClrSimpleTypeInfo itemType = listType.ItemType;
                    expressions.Add(CreateSimpleTypeDef(itemType, nameMappings, settings, true));
                    break;

                case XmlSchemaDatatypeVariety.Union:
                    simpleTypeCreate = new CodeObjectCreateExpression(Constants.UnionSimpleTypeValidator);
                    expressions = simpleTypeCreate.Parameters;
                    expressions.Add(CreateGetBuiltInSimpleType(typeInfo.TypeCode));
                    expressions.Add(CreateFacets(typeInfo));

                    UnionSimpleTypeInfo unionType = typeInfo as UnionSimpleTypeInfo;
                    CodeArrayCreateExpression memberTypeCreate = new CodeArrayCreateExpression();
                    memberTypeCreate.CreateType = new CodeTypeReference(Constants.SimpleTypeValidator);
                    foreach (ClrSimpleTypeInfo st in unionType.MemberTypes)
                    {
                        memberTypeCreate.Initializers.Add(CreateSimpleTypeDef(st, nameMappings, settings, true));
                    }

                    expressions.Add(memberTypeCreate);
                    break;
            }

            return simpleTypeCreate;
        }


        internal static CodeExpression CreateGetBuiltInSimpleType(XmlTypeCode typeCode)
        {
            return CodeDomHelper.CreateMethodCall(
                new CodeTypeReferenceExpression("XmlSchemaType"),
                Constants.GetBuiltInSimpleType,
                CodeDomHelper.CreateFieldReference(Constants.XmlTypeCode, typeCode.ToString()));
        }

        public static CodeExpression CreateFacets(ClrSimpleTypeInfo type)
        {
            CompiledFacets facets = type.RestrictionFacets;

            CodeObjectCreateExpression createFacets = new CodeObjectCreateExpression();
            createFacets.CreateType = new CodeTypeReference(Constants.RestrictionFacets);

            RestrictionFlags flags = facets.Flags;

            if (flags == 0)
                return new CodePrimitiveExpression(null);
            else
            {
                CodeCastExpression cast = new CodeCastExpression(new CodeTypeReference(Constants.RestrictionFlags),
                    new CodePrimitiveExpression(
                        System.Convert.ToInt32(flags, CultureInfo.InvariantCulture.NumberFormat)));
                createFacets.Parameters.Add(cast);
            }


            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                CodeArrayCreateExpression enums = new CodeArrayCreateExpression();
                enums.CreateType = new CodeTypeReference("System.Object");

                foreach (object o in facets.Enumeration)
                {
                    GetCreateValueExpression(o, type, enums.Initializers);
                }

                createFacets.Parameters.Add(enums);
            }
            else
            {
                createFacets.Parameters.Add(new CodePrimitiveExpression(null));
            }

            int fractionDigits = default(int);
            if ((flags & RestrictionFlags.FractionDigits) != 0)
            {
                fractionDigits = facets.FractionDigits;
            }

            createFacets.Parameters.Add(new CodePrimitiveExpression(fractionDigits));

            int length = default(int);
            if ((flags & RestrictionFlags.Length) != 0)
            {
                length = facets.Length;
            }

            createFacets.Parameters.Add(new CodePrimitiveExpression(length));

            object maxExclusive = default(object);
            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                maxExclusive = facets.MaxExclusive;
            }

            GetCreateValueExpression(maxExclusive, type, createFacets.Parameters);


            object maxInclusive = default(object);
            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                maxInclusive = facets.MaxInclusive;
            }

            GetCreateValueExpression(maxInclusive, type, createFacets.Parameters);


            int maxLength = default(int);
            if ((flags & RestrictionFlags.MaxLength) != 0)
            {
                maxLength = facets.MaxLength;
            }

            createFacets.Parameters.Add(new CodePrimitiveExpression(maxLength));

            object minExclusive = default(object);
            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                minExclusive = facets.MinExclusive;
            }

            GetCreateValueExpression(minExclusive, type, createFacets.Parameters);


            object minInclusive = default(object);
            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                minInclusive = facets.MinInclusive;
            }

            GetCreateValueExpression(minInclusive, type, createFacets.Parameters);


            int minLength = default(int);
            if ((flags & RestrictionFlags.MinLength) != 0)
            {
                minLength = facets.MinLength;
            }

            createFacets.Parameters.Add(new CodePrimitiveExpression(minLength));

            if ((flags & RestrictionFlags.Pattern) != 0)
            {
                CodeArrayCreateExpression patternStrs = new CodeArrayCreateExpression();
                patternStrs.CreateType = new CodeTypeReference(XTypedServices.typeOfString);

                foreach (object o in facets.Patterns)
                {
                    string str = o.ToString();
                    patternStrs.Initializers.Add(new CodePrimitiveExpression(str));
                }

                createFacets.Parameters.Add(patternStrs);
            }
            else
            {
                createFacets.Parameters.Add(new CodePrimitiveExpression(null));
            }

            int totalDigits = default(int);
            if ((flags & RestrictionFlags.TotalDigits) != 0)
            {
                totalDigits = facets.TotalDigits;
            }

            createFacets.Parameters.Add(new CodePrimitiveExpression(totalDigits));

            XmlSchemaWhiteSpace ws = facets.WhiteSpace;
            createFacets.Parameters.Add(
                CodeDomHelper.CreateFieldReference(Constants.XmlSchemaWhiteSpace, ws.ToString()));


            return createFacets;
        }

        public static void GetCreateUnionValueExpression(object value,
            UnionSimpleTypeInfo unionDef,
            CodeExpressionCollection collection)
        {
            Debug.Assert(unionDef != null);

            //Use reflection to get real value and type from "value", which is an XsdSimpleValue
            object typedValue = value.GetType().InvokeMember("TypedValue",
                BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance,
                null,
                value,
                null,
                CultureInfo.InvariantCulture);

            CodeExpressionCollection dummy = new CodeExpressionCollection();

            ClrSimpleTypeInfo matchingType = null;
            foreach (ClrSimpleTypeInfo type in unionDef.MemberTypes)
            {
                try
                {
                    GetCreateValueExpression(typedValue, type, dummy);
                    matchingType = type;
                    break;
                }
                catch (Exception)
                {
                    continue;
                }
            }

            Debug.Assert(matchingType != null);
            GetCreateValueExpression(typedValue, matchingType, collection);
        }

        public static void GetCreateValueExpression(object value,
            ClrSimpleTypeInfo typeDef,
            CodeExpressionCollection collection)
        {
            if (value == null)
            {
                collection.Add(new CodePrimitiveExpression(value));
                return;
            }

            switch (typeDef.Variety)
            {
                case XmlSchemaDatatypeVariety.List:
                    string str = ListSimpleTypeValidator.ToString(value);
                    collection.Add(new CodePrimitiveExpression(str));
                    break;

                case XmlSchemaDatatypeVariety.Atomic:
                    if (value is string) collection.Add(new CodePrimitiveExpression(value));
                    else collection.Add(CreateTypedValueExpression(typeDef.InnerType.Datatype, value));
                    break;

                case XmlSchemaDatatypeVariety.Union:
                    GetCreateUnionValueExpression(value, typeDef as UnionSimpleTypeInfo, collection);
                    break;

                default:
                    break;
            }
        }

        private static CodeExpression CreateTypedValueExpression(XmlSchemaDatatype dataType, object value)
        {
            XmlTypeCode typeCode = dataType.TypeCode;
            switch (typeCode)
            {
                case XmlTypeCode.String:
                case XmlTypeCode.Notation:
                case XmlTypeCode.NormalizedString:
                case XmlTypeCode.Token:
                case XmlTypeCode.Language:
                case XmlTypeCode.Id:
                    string str = value as string;
                    Debug.Assert(str != null);
                    return new CodePrimitiveExpression(str);

                case XmlTypeCode.AnyUri:
                    Debug.Assert(value is Uri);
                    return new CodeObjectCreateExpression(typeof(Uri),
                        new CodePrimitiveExpression(((Uri) value).OriginalString));

                case XmlTypeCode.QName:
                    XmlQualifiedName qname = value as XmlQualifiedName;
                    return new CodeObjectCreateExpression(typeof(XmlQualifiedName),
                        new CodePrimitiveExpression(qname.Name), new CodePrimitiveExpression(qname.Namespace));

                case XmlTypeCode.NmToken:
                case XmlTypeCode.Name:
                case XmlTypeCode.NCName:
                    return CodeDomHelper.CreateMethodCall(
                        new CodeTypeReferenceExpression(typeof(XmlConvert)),
                        "EncodeName",
                        new CodePrimitiveExpression(value.ToString()));

                case XmlTypeCode.Boolean:
                    Debug.Assert(value is bool);
                    return new CodePrimitiveExpression(value);

                case XmlTypeCode.Float:
                case XmlTypeCode.Double:
                    Debug.Assert(value is double || value is float);
                    return new CodePrimitiveExpression(value);

                case XmlTypeCode.Duration:
                    Debug.Assert(value is TimeSpan);
                    TimeSpan ts = (TimeSpan) value;
                    return new CodeObjectCreateExpression(typeof(TimeSpan), new CodePrimitiveExpression(ts.Ticks));

                case XmlTypeCode.Time:
                case XmlTypeCode.Date:
                case XmlTypeCode.GYearMonth:
                case XmlTypeCode.GYear:
                case XmlTypeCode.GMonthDay:
                case XmlTypeCode.GDay:
                case XmlTypeCode.GMonth:
                case XmlTypeCode.DateTime:
                    Debug.Assert(value is DateTime);
                    DateTime dt = (DateTime) value;
                    return new CodeObjectCreateExpression(typeof(DateTime), new CodePrimitiveExpression(dt.Ticks));

                case XmlTypeCode.Integer:
                case XmlTypeCode.NonPositiveInteger:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.Long:
                case XmlTypeCode.Int:
                case XmlTypeCode.Short:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.UnsignedLong:
                case XmlTypeCode.UnsignedInt:
                case XmlTypeCode.UnsignedShort:
                case XmlTypeCode.PositiveInteger:
                case XmlTypeCode.Decimal:
                case XmlTypeCode.Byte:
                case XmlTypeCode.UnsignedByte:
                    return new CodePrimitiveExpression(value);

                case XmlTypeCode.HexBinary:
                case XmlTypeCode.Base64Binary:
                    return CreateByteArrayExpression(value);

                case XmlTypeCode.None:
                case XmlTypeCode.Item:
                case XmlTypeCode.AnyAtomicType:
                case XmlTypeCode.Idref:
                case XmlTypeCode.Entity:
                    throw new InvalidOperationException();
                default:
                    throw new InvalidOperationException();
            }
        }

        private static CodeExpression CreateByteArrayExpression(object value)
        {
            byte[] bytes = (byte[]) value;
            CodeArrayCreateExpression array = new CodeArrayCreateExpression();
            array.CreateType = new CodeTypeReference(typeof(byte));
            foreach (byte b in bytes)
            {
                array.Initializers.Add(new CodePrimitiveExpression(b));
            }

            return array;
        }

        private static CodeExpression CreateTypeConversionExpr(string typeName, object value)
        {
            return CodeDomHelper.CreateMethodCall(
                new CodeTypeReferenceExpression(typeof(XmlConvert)),
                "To" + typeName,
                new CodePrimitiveExpression(value.ToString()));
        }

        internal static CodeExpression CreateValueExpression(string builtInType, string strValue)
        {
            int dot = builtInType.LastIndexOf('.');
            Debug.Assert(dot != -1);

            string localType = builtInType.Substring(dot + 1);

            if (localType == "String" || localType == "Object")
            {
                return new CodePrimitiveExpression(strValue);
            }
            else if (localType == "Uri")
            {
                return new CodeObjectCreateExpression("Uri", new CodePrimitiveExpression(strValue));
            }
            else
            {
                return CreateTypeConversionExpr(localType, strValue);
            }
        }

        internal static CodeArrayCreateExpression CreateFixedDefaultArrayValueInit(string baseType, string value)
        {
            CodeArrayCreateExpression array = new CodeArrayCreateExpression(baseType);
            foreach (string s in value.Split(' '))
            {
                array.Initializers.Add(CreateValueExpression(baseType, s));
            }

            return array;
        }

        internal static CodeExpression CreateFixedDefaultValueExpression(CodeTypeReference type, string value)
        {
            string baseType = type.BaseType;
            if (baseType.Contains("Nullable"))
            {
                Debug.Assert(type.TypeArguments.Count == 1);
                baseType = type.TypeArguments[0].BaseType;
                return CreateValueExpression(baseType, value);
            }
            else if (type.ArrayRank != 0)
            {
                baseType = type.ArrayElementType.BaseType;
                return CreateFixedDefaultArrayValueInit(baseType, value);
            }
            else if (baseType.Contains("List"))
            {
                //Create sth like: new List<string>(new string[] { });
                Debug.Assert(type.TypeArguments.Count == 1);

                baseType = type.TypeArguments[0].BaseType;
                return CreateFixedDefaultArrayValueInit(baseType, value);
            }

            return CreateValueExpression(baseType, value);
        }
    }
}