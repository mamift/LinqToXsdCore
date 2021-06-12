//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq
{
    internal class CompiledFacets
    {
        RestrictionFlags flags;

        int length;
        int minLength;
        int maxLength;
        object maxInclusive;
        object maxExclusive;
        object minInclusive;
        object minExclusive;
        int totalDigits;
        int fractionDigits;
        ArrayList patterns;
        ArrayList enumerations;
        XmlSchemaWhiteSpace whiteSpace;

        public CompiledFacets(XmlSchemaDatatype dt)
        {
            whiteSpace = dt.GetBuiltInWSFacet();
        }

        public int Length
        {
            get { return length; }
        }

        public int MinLength
        {
            get { return minLength; }
        }

        public int MaxLength
        {
            get { return maxLength; }
        }

        public ArrayList Patterns
        {
            get { return patterns; }
        }

        public ArrayList Enumeration
        {
            get { return enumerations; }
        }

        public XmlSchemaWhiteSpace WhiteSpace
        {
            get { return whiteSpace; }
        }

        public object MaxInclusive
        {
            get { return maxInclusive; }
        }

        public object MaxExclusive
        {
            get { return maxExclusive; }
        }

        public object MinInclusive
        {
            get { return minInclusive; }
        }

        public object MinExclusive
        {
            get { return minExclusive; }
        }

        public int TotalDigits
        {
            get { return totalDigits; }
        }

        public int FractionDigits
        {
            get { return fractionDigits; }
        }

        public RestrictionFlags Flags
        {
            get { return flags; }
        }

        public void compileFacets(XmlSchemaSimpleType simpleType)
        {
            XmlSchemaSimpleType type = simpleType;
            XmlSchemaSimpleType enumSimpleType = null; //simpletype that has most restricted enums.
            flags = 0;
            while (type != null &&
                   !String.Equals(type.QualifiedName.Namespace, Constants.XSD, StringComparison.Ordinal))
            {
                XmlSchemaSimpleTypeRestriction simpleTypeRestriction = type.Content as XmlSchemaSimpleTypeRestriction;
                if (simpleTypeRestriction != null)
                {
                    foreach (XmlSchemaFacet facet in simpleTypeRestriction.Facets)
                    {
                        if (facet is XmlSchemaMinLengthFacet)
                        {
                            if ((flags & RestrictionFlags.MinLength) == 0)
                            {
                                minLength = XmlConvert.ToInt32(facet.Value);
                                flags |= RestrictionFlags.MinLength;
                            }
                        }
                        else if (facet is XmlSchemaMaxLengthFacet)
                        {
                            if ((flags & RestrictionFlags.MaxLength) == 0)
                            {
                                maxLength = XmlConvert.ToInt32(facet.Value);
                                flags |= RestrictionFlags.MaxLength;
                            }
                        }
                        else if (facet is XmlSchemaLengthFacet)
                        {
                            if ((flags & RestrictionFlags.Length) == 0)
                            {
                                length = XmlConvert.ToInt32(facet.Value);
                                flags |= RestrictionFlags.Length;
                            }
                        }
                        else if (facet is XmlSchemaEnumerationFacet)
                        {
                            if (enumSimpleType == null)
                            {
                                enumerations = new ArrayList();
                                flags |= RestrictionFlags.Enumeration;
                                enumSimpleType = type;
                            }
                            else if (enumSimpleType != type)
                            {
                                continue;
                            }
                            // if datatype is NCName then this causes an exception
                            var value = type.BaseXmlSchemaType.Datatype.
                                ParseValue(s: facet.Value, nameTable: null, nsmgr: null);
                            enumerations.Add(value);
                        }
                        else if (facet is XmlSchemaPatternFacet)
                        {
                            if (patterns == null)
                            {
                                patterns = new ArrayList();
                                flags |= RestrictionFlags.Pattern;
                            }

                            patterns.Add(facet.Value);
                        }
                        else if (facet is XmlSchemaMaxInclusiveFacet)
                        {
                            if ((flags & RestrictionFlags.MaxInclusive) == 0)
                            {
                                maxInclusive = type.BaseXmlSchemaType.Datatype.ParseValue(facet.Value, null, null);
                                flags |= RestrictionFlags.MaxInclusive;
                            }
                        }
                        else if (facet is XmlSchemaMaxExclusiveFacet)
                        {
                            if ((flags & RestrictionFlags.MaxExclusive) == 0)
                            {
                                maxExclusive = type.BaseXmlSchemaType.Datatype.ParseValue(facet.Value, null, null);
                                flags |= RestrictionFlags.MaxExclusive;
                            }
                        }
                        else if (facet is XmlSchemaMinExclusiveFacet)
                        {
                            if ((flags & RestrictionFlags.MinExclusive) == 0)
                            {
                                minExclusive = type.BaseXmlSchemaType.Datatype.ParseValue(facet.Value, null, null);
                                flags |= RestrictionFlags.MinExclusive;
                            }
                        }
                        else if (facet is XmlSchemaMinInclusiveFacet)
                        {
                            if ((flags & RestrictionFlags.MinInclusive) == 0)
                            {
                                minInclusive = type.BaseXmlSchemaType.Datatype.ParseValue(facet.Value, null, null);
                                flags |= RestrictionFlags.MinInclusive;
                            }
                        }
                        else if (facet is XmlSchemaFractionDigitsFacet)
                        {
                            if ((flags & RestrictionFlags.FractionDigits) == 0)
                            {
                                fractionDigits = XmlConvert.ToInt32(facet.Value);
                                flags |= RestrictionFlags.FractionDigits;
                            }
                        }
                        else if (facet is XmlSchemaTotalDigitsFacet)
                        {
                            if ((flags & RestrictionFlags.TotalDigits) == 0)
                            {
                                totalDigits = XmlConvert.ToInt32(facet.Value);
                                flags |= RestrictionFlags.TotalDigits;
                            }
                        }
                        else if (facet is XmlSchemaWhiteSpaceFacet)
                        {
                            if ((flags & RestrictionFlags.WhiteSpace) == 0)
                            {
                                if (facet.Value == "preserve")
                                {
                                    whiteSpace = XmlSchemaWhiteSpace.Preserve;
                                }
                                else if (facet.Value == "replace")
                                {
                                    whiteSpace = XmlSchemaWhiteSpace.Replace;
                                }
                                else if (facet.Value == "collapse")
                                {
                                    whiteSpace = XmlSchemaWhiteSpace.Collapse;
                                }

                                flags |= RestrictionFlags.WhiteSpace;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                type = type.BaseXmlSchemaType as XmlSchemaSimpleType;
            }
        }
    }
}