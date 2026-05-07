//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq
{
    public class CompiledFacets(XmlSchemaDatatype dt)
    {
        public XmlSchemaWhiteSpace WhiteSpace { get; private set; } = dt.GetBuiltInWSFacet();

        public RestrictionFlags Flags { get; private set; }

        public int Length { get; private set; }
        public int MinLength { get; private set; }
        public int MaxLength { get; private set; }
        
        public List<string> Patterns { get; private set; }

        public List<object> Enumeration { get; private set; }

        public object MaxInclusive { get; private set; }
        public object MaxExclusive { get; private set; }
        public object MinInclusive { get; private set; }
        public object MinExclusive { get; private set; }

        public int TotalDigits { get; private set; }
        public int FractionDigits { get; private set; }

        public void CompileFacets(XmlSchemaSimpleType simpleType)
        {
            var isEnum = simpleType.IsEnum();
            XmlSchemaSimpleType type = simpleType;
            XmlSchemaSimpleType enumSimpleType = null; // simpletype that has most restricted enums.
            Flags = 0;
            while (type != null &&
                   !string.Equals(type.QualifiedName.Namespace, Constants.XSD, StringComparison.Ordinal))
            {
                if (type.Content is XmlSchemaSimpleTypeRestriction { Facets: var facets })
                {
                    foreach (XmlSchemaFacet facet in facets)
                    {
                        switch (facet) 
                        {
                            case XmlSchemaMinLengthFacet when !Flags.HasFlag(RestrictionFlags.MinLength):
                                Flags |= RestrictionFlags.MinLength;
                                MinLength = XmlConvert.ToInt32(facet.Value);
                                break;

                            case XmlSchemaMaxLengthFacet when !Flags.HasFlag(RestrictionFlags.MaxLength):
                                Flags |= RestrictionFlags.MaxLength;
                                MaxLength = XmlConvert.ToInt32(facet.Value);
                                break;

                            case XmlSchemaLengthFacet when !Flags.HasFlag(RestrictionFlags.Length):
                                Flags |= RestrictionFlags.Length;
                                Length = XmlConvert.ToInt32(facet.Value);
                                break;

                            case XmlSchemaEnumerationFacet: {
                                    if (enumSimpleType == null)
                                        enumSimpleType = type;
                                    else if (enumSimpleType != type)
                                        continue;

                                    Flags |= RestrictionFlags.Enumeration;
                                    Enumeration ??= [];

                                    // if datatype is NCName then a null nametable causes an exception
                                    var nameTable = type.BaseXmlSchemaType.Datatype.TypeCode == XmlTypeCode.NCName
                                        ? new NameTable()
                                        : null;

                                    var value = type.BaseXmlSchemaType.Datatype.ParseValue(s: facet.Value, nameTable: nameTable, nsmgr: null);

                                    Enumeration.Add(isEnum
                                        ? EnumFacet.Stringify(value)
                                        : value);
                                    break;
                                }

                            case XmlSchemaPatternFacet:
                                Flags |= RestrictionFlags.Pattern;
                                Patterns ??= [];
                                Patterns.Add(facet.Value);
                                break;

                            case XmlSchemaMaxInclusiveFacet when !Flags.HasFlag(RestrictionFlags.MaxInclusive):
                                Flags |= RestrictionFlags.MaxInclusive;
                                MaxInclusive = type.BaseXmlSchemaType.Datatype.ParseValue(facet.Value, null, null);
                                break;

                            case XmlSchemaMaxExclusiveFacet when !Flags.HasFlag(RestrictionFlags.MaxExclusive):
                                Flags |= RestrictionFlags.MaxExclusive;
                                MaxExclusive = type.BaseXmlSchemaType.Datatype.ParseValue(facet.Value, null, null);
                                break;

                            case XmlSchemaMinExclusiveFacet when !Flags.HasFlag(RestrictionFlags.MinExclusive):
                                Flags |= RestrictionFlags.MinExclusive;
                                MinExclusive = type.BaseXmlSchemaType.Datatype.ParseValue(facet.Value, null, null);
                                break;

                            case XmlSchemaMinInclusiveFacet when !Flags.HasFlag(RestrictionFlags.MinInclusive):
                                Flags |= RestrictionFlags.MinInclusive;
                                MinInclusive = type.BaseXmlSchemaType.Datatype.ParseValue(facet.Value, null, null);
                                break;

                            case XmlSchemaFractionDigitsFacet when !Flags.HasFlag(RestrictionFlags.FractionDigits):
                                Flags |= RestrictionFlags.FractionDigits;
                                FractionDigits = XmlConvert.ToInt32(facet.Value);
                                break;

                            case XmlSchemaTotalDigitsFacet when !Flags.HasFlag(RestrictionFlags.TotalDigits):
                                Flags |= RestrictionFlags.TotalDigits;
                                TotalDigits = XmlConvert.ToInt32(facet.Value);
                                break;

                            case XmlSchemaWhiteSpaceFacet when !Flags.HasFlag(RestrictionFlags.WhiteSpace):
                                Flags |= RestrictionFlags.WhiteSpace;
                                WhiteSpace = facet.Value switch {
                                    "preserve" => XmlSchemaWhiteSpace.Preserve,
                                    "replace" => XmlSchemaWhiteSpace.Replace,
                                    "collapse" => XmlSchemaWhiteSpace.Collapse,
                                    _ => WhiteSpace,
                                };
                                break;
                        }
                    }
                }

                type = type.BaseXmlSchemaType as XmlSchemaSimpleType;
            }
        }
    }
}