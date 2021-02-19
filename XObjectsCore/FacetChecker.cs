//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq
{
    internal class FacetsChecker
    {
        //Numeric10FacetsChecker has varied minValue and maxValue for different
        //decimal data types, and will be initialized when the mapping is initialized
        internal static Numeric2FacetsChecker numeric2FacetsChecker;
        internal static DurationFacetsChecker durationFacetsChecker;
        internal static DateTimeFacetsChecker dateTimeFacetsChecker;
        internal static StringFacetsChecker stringFacetsChecker;
        internal static QNameFacetsChecker qNameFacetsChecker;
        internal static MiscFacetsChecker miscFacetsChecker;
        internal static BinaryFacetsChecker binaryFacetsChecker;
        internal static ListFacetsChecker listFacetsChecker;
        internal static UnionFacetsChecker unionFacetsChecker;
        private static Dictionary<XmlTypeCode, FacetsChecker> FacetsCheckerMapping;

        internal static FacetsChecker ListFacetsChecker
        {
            get { return FacetsChecker.listFacetsChecker; }
        }

        internal static FacetsChecker UnionFacetsChecker
        {
            get { return FacetsChecker.unionFacetsChecker; }
        }

        static FacetsChecker()
        {
            numeric2FacetsChecker = new Numeric2FacetsChecker();
            durationFacetsChecker = new DurationFacetsChecker();
            dateTimeFacetsChecker = new DateTimeFacetsChecker();
            stringFacetsChecker = new StringFacetsChecker();
            qNameFacetsChecker = new QNameFacetsChecker();
            miscFacetsChecker = new MiscFacetsChecker();
            binaryFacetsChecker = new BinaryFacetsChecker();
            listFacetsChecker = new ListFacetsChecker();
            unionFacetsChecker = new UnionFacetsChecker();
        }


        internal static FacetsChecker GetFacetsChecker(XmlTypeCode typeCode)
        {
            if (FacetsCheckerMapping == null)
            {
                InitMapping();
            }

            return FacetsCheckerMapping[typeCode];
        }

        private static void InitMapping()
        {
            //This mapping is based on Datatypeimplementation in System.Xml.Schema
            FacetsCheckerMapping = new Dictionary<XmlTypeCode, FacetsChecker>();
            FacetsCheckerMapping.Add(XmlTypeCode.AnyAtomicType, miscFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.AnyUri, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Base64Binary, binaryFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Boolean, miscFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Byte, new Numeric10FacetsChecker(sbyte.MinValue, sbyte.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.Date, dateTimeFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.DateTime, dateTimeFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.DayTimeDuration, durationFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Decimal,
                new Numeric10FacetsChecker(decimal.MinValue, decimal.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.Double, numeric2FacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Duration, durationFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Entity, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Float, numeric2FacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.GDay, dateTimeFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.GMonth, dateTimeFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.GMonthDay, dateTimeFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.GYear, dateTimeFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.GYearMonth, dateTimeFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.HexBinary, binaryFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Id, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Idref, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Int, new Numeric10FacetsChecker(int.MinValue, int.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.Integer,
                new Numeric10FacetsChecker(decimal.MinValue, decimal.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.Item, miscFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Language, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Long, new Numeric10FacetsChecker(long.MinValue, long.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.Name, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.NCName, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.NegativeInteger,
                new Numeric10FacetsChecker(decimal.MinValue, decimal.MinusOne));
            FacetsCheckerMapping.Add(XmlTypeCode.NmToken, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.NonNegativeInteger,
                new Numeric10FacetsChecker(decimal.Zero, decimal.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.NonPositiveInteger,
                new Numeric10FacetsChecker(decimal.MinValue, decimal.Zero));
            FacetsCheckerMapping.Add(XmlTypeCode.NormalizedString, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Notation, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.PositiveInteger,
                new Numeric10FacetsChecker(decimal.One, decimal.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.QName, qNameFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Short, new Numeric10FacetsChecker(short.MinValue, short.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.String, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Time, dateTimeFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.Token, stringFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.UnsignedByte,
                new Numeric10FacetsChecker(byte.MinValue, byte.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.UnsignedInt, new Numeric10FacetsChecker(uint.MinValue, uint.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.UnsignedLong,
                new Numeric10FacetsChecker(ulong.MinValue, ulong.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.UnsignedShort,
                new Numeric10FacetsChecker(ushort.MinValue, ushort.MaxValue));
            FacetsCheckerMapping.Add(XmlTypeCode.UntypedAtomic, miscFacetsChecker);
            FacetsCheckerMapping.Add(XmlTypeCode.YearMonthDuration, durationFacetsChecker);
        }

        internal static decimal Power(int x, int y)
        {
            //Returns X raised to the power Y
            decimal returnValue = 1m;
            decimal decimalValue = (decimal) x;
            if (y > 28)
            {
                //CLR decimal cannot handle more than 29 digits (10 power 28.)
                return decimal.MaxValue;
            }

            for (int i = 0; i < y; i++)
            {
                returnValue = returnValue * decimalValue;
            }

            return returnValue;
        }

        internal Exception CheckTotalAndFractionDigits(decimal value, int totalDigits, int fractionDigits,
            bool checkTotal, bool checkFraction)
        {
            decimal maxValue = FacetsChecker.Power(10, totalDigits) - 1; //(decimal)Math.Pow(10, totalDigits) - 1 ;
            int powerCnt = 0;
            if (value < 0)
            {
                value = Decimal.Negate(value); //Need to compare maxValue allowed against the absolute value
            }

            while (Decimal.Truncate(value) != value)
            {
                //Till it has a fraction
                value = value * 10;
                powerCnt++;
            }

            if (checkTotal & (value > maxValue || powerCnt > totalDigits))
            {
                return new LinqToXsdException();
            }

            if (checkFraction & powerCnt > fractionDigits)
            {
                return new LinqToXsdException();
            }

            return null;
        }

        internal virtual Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(decimal value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(Int64 value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(Int32 value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(Int16 value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(byte value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(DateTime value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(double value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(float value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(string value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(byte[] value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(TimeSpan value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual Exception CheckValueFacets(XmlQualifiedName value, SimpleTypeValidator type)
        {
            return null;
        }

        internal virtual bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return false;
        }

        internal virtual Exception CheckLexicalFacets(ref string parsedString, object value, NameTable nameTable,
            XNamespaceResolver resolver, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasLexicalFacets)
            {
                return null;
            }

            RestrictionFlags flags = facets.Flags;
            XmlSchemaWhiteSpace wsPattern = XmlSchemaWhiteSpace.Collapse;


            if ((flags & RestrictionFlags.WhiteSpace) != 0)
            {
                if (facets.WhiteSpace == XmlSchemaWhiteSpace.Collapse)
                {
                    wsPattern = XmlSchemaWhiteSpace.Collapse;
                }
                else if (facets.WhiteSpace == XmlSchemaWhiteSpace.Preserve)
                {
                    wsPattern = XmlSchemaWhiteSpace.Preserve;
                }
            }


            return CheckLexicalFacets(ref parsedString, type, facets.Patterns, wsPattern);
        }

        internal virtual Exception CheckLexicalFacets(ref string parsedString,
            SimpleTypeValidator type,
            ArrayList patterns,
            XmlSchemaWhiteSpace wsPattern)
        {
            CheckWhitespaceFacets(ref parsedString, type, wsPattern);
            return CheckPatternFacets(patterns, parsedString);
        }

        internal Exception CheckPatternFacets(ArrayList patterns, string value)
        {
            if (patterns == null) return null;
            foreach (object pattern in patterns)
            {
                Regex regex = pattern as Regex;
                Debug.Assert(regex != null);

                if (regex.IsMatch(value))
                {
                    return null;
                }
            }

            return new LinqToXsdFacetException(RestrictionFlags.Pattern, patterns, value);
        }

        internal void CheckWhitespaceFacets(ref string s,
            SimpleTypeValidator type,
            XmlSchemaWhiteSpace wsPattern)
        {
            // before parsing, check whitespace facet
            RestrictionFacets restriction = type.RestrictionFacets;

            if (type.Variety == XmlSchemaDatatypeVariety.List)
            {
                s = s.Trim();
                return;
            }
            else if (type.Variety == XmlSchemaDatatypeVariety.Atomic)
            {
                XmlSchemaDatatype datatype = type.DataType;
                if (datatype.GetBuiltInWSFacet() == XmlSchemaWhiteSpace.Collapse)
                {
                    s = XmlComplianceUtil.NonCDataNormalize(s);
                }
                else if (datatype.GetBuiltInWSFacet() == XmlSchemaWhiteSpace.Replace)
                {
                    s = XmlComplianceUtil.CDataNormalize(s);
                }
                else if (restriction != null & (restriction.Flags & RestrictionFlags.WhiteSpace) != 0)
                {
                    //Restriction has whitespace facet specified
                    if (restriction.WhiteSpace == XmlSchemaWhiteSpace.Replace)
                    {
                        s = XmlComplianceUtil.CDataNormalize(s);
                    }
                    else if (restriction.WhiteSpace == XmlSchemaWhiteSpace.Collapse)
                    {
                        s = XmlComplianceUtil.NonCDataNormalize(s);
                    }
                }
            }

            return;
        }
    }


    internal class Numeric10FacetsChecker : FacetsChecker
    {
        decimal maxValue;
        decimal minValue;

        internal Numeric10FacetsChecker(decimal minVal, decimal maxVal)
        {
            minValue = minVal;
            maxValue = maxVal;
        }

        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            if (type.RestrictionFacets == null || !type.RestrictionFacets.HasValueFacets)
            {
                return null;
            }

            XmlSchemaDatatype datatype = type.DataType;

            decimal decimalValue = (decimal) datatype.ChangeType(value, typeof(decimal));
            return CheckValueFacets(decimalValue, type);
        }

        internal override Exception CheckValueFacets(decimal value, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasValueFacets)
            {
                return null;
            }

            //No need to check built-in type because CLR should've already done that

            RestrictionFlags flags = facets.Flags;
            XmlSchemaDatatype datatype = type.DataType;

            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;


                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }

            if ((flags & RestrictionFlags.FractionDigits) != 0)
            {
                Exception e = CheckTotalAndFractionDigits(value,
                    Constants.DecimalMaxPower,
                    facets.FractionDigits,
                    false,
                    true);

                if (e != null)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.FractionDigits, facets.FractionDigits, value);
                }
            }


            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                if (value >= (decimal) datatype.ChangeType(facets.MaxExclusive, typeof(decimal)))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxExclusive, facets.MaxExclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                if (value > (decimal) datatype.ChangeType(facets.MaxInclusive, typeof(decimal)))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxInclusive, facets.MaxInclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                if (value <= (decimal) datatype.ChangeType(facets.MinExclusive, typeof(decimal)))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinExclusive, facets.MinExclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                if (value < (decimal) datatype.ChangeType(facets.MinInclusive, typeof(decimal)))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinInclusive, facets.MinInclusive, value);
                }
            }

            if ((flags & RestrictionFlags.TotalDigits) != 0)
            {
                Exception e = CheckTotalAndFractionDigits(value,
                    System.Convert.ToInt32(facets.TotalDigits),
                    0,
                    true,
                    false);

                if (e != null)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.TotalDigits, facets.TotalDigits, value);
                }
            }

            return null;
        }


        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((decimal) datatype.ChangeType(value, typeof(decimal)), enumeration, datatype);
        }

        internal bool MatchEnumeration(decimal value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            foreach (object correctValue in enumeration)
            {
                if (value == (decimal) datatype.ChangeType(correctValue, typeof(decimal)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class Numeric2FacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            if (type.RestrictionFacets == null || !type.RestrictionFacets.HasValueFacets)
            {
                return null;
            }

            XmlSchemaDatatype datatype = type.DataType;
            double doubleValue = (double) datatype.ChangeType(value, typeof(double));
            return CheckValueFacets(doubleValue, type);
        }

        internal override Exception CheckValueFacets(double value, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasValueFacets)
            {
                return null;
            }

            RestrictionFlags flags = facets.Flags;
            XmlSchemaDatatype datatype = type.DataType;


            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                if (value < (double) datatype.ChangeType(facets.MinInclusive, typeof(double)))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinInclusive, facets.MinInclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                if (value <= (double) datatype.ChangeType(facets.MinExclusive, typeof(double)))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinExclusive, facets.MinExclusive, value);
                }
            }


            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                if (value > (double) datatype.ChangeType(facets.MaxInclusive, typeof(double)))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxInclusive, facets.MaxInclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                if (value >= (double) datatype.ChangeType(facets.MaxExclusive, typeof(double)))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxExclusive, facets.MaxExclusive, value);
                }
            }


            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;

                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }


            return null;
        }

        internal override Exception CheckValueFacets(float value, SimpleTypeValidator type)
        {
            double doubleValue = (double) value;
            return CheckValueFacets(doubleValue, type);
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((double) datatype.ChangeType(value, typeof(double)), enumeration, datatype);
        }

        private bool MatchEnumeration(double value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            foreach (object correctValue in enumeration)
            {
                if (value == (double) datatype.ChangeType(correctValue, typeof(double)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class DurationFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            if (type.RestrictionFacets == null || !type.RestrictionFacets.HasValueFacets)
            {
                return null;
            }

            XmlSchemaDatatype datatype = type.DataType;
            TimeSpan timeSpanValue = (TimeSpan) datatype.ChangeType(value, typeof(TimeSpan));
            return CheckValueFacets(timeSpanValue, type);
        }

        internal override Exception CheckValueFacets(TimeSpan value, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasValueFacets)
            {
                return null;
            }

            RestrictionFlags flags = facets.Flags;
            XmlSchemaDatatype datatype = type.DataType;

            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;


                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }

            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                if (TimeSpan.Compare(value, (TimeSpan) datatype.ChangeType(facets.MaxInclusive, typeof(TimeSpan))) > 0)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxInclusive, facets.MaxInclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                if (TimeSpan.Compare(value, (TimeSpan) datatype.ChangeType(facets.MaxExclusive, typeof(TimeSpan))) >= 0)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxExclusive, facets.MaxExclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                if (TimeSpan.Compare(value, (TimeSpan) datatype.ChangeType(facets.MinInclusive, typeof(TimeSpan))) < 0)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxExclusive, facets.MinInclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                if (TimeSpan.Compare(value, (TimeSpan) datatype.ChangeType(facets.MinExclusive, typeof(TimeSpan))) <= 0)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxExclusive, facets.MinExclusive, value);
                }
            }


            return null;
        }


        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((TimeSpan) value, enumeration, datatype);
        }

        private bool MatchEnumeration(TimeSpan value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            foreach (TimeSpan correctValue in enumeration)
            {
                if (TimeSpan.Compare(value, correctValue) == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class DateTimeFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            if (type.RestrictionFacets == null || !type.RestrictionFacets.HasValueFacets)
            {
                return null;
            }

            XmlSchemaDatatype datatype = type.DataType;
            DateTime dateTimeValue = (DateTime) datatype.ChangeType(value, typeof(DateTime));
            return CheckValueFacets(dateTimeValue, type);
        }

        internal override Exception CheckValueFacets(DateTime value, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasValueFacets)
            {
                return null;
            }

            RestrictionFlags flags = facets.Flags;
            XmlSchemaDatatype datatype = type.DataType;

            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;

                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }


            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                if (DateTime.Compare(value, (DateTime) datatype.ChangeType(facets.MinInclusive, typeof(DateTime))) < 0)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinInclusive, facets.MinInclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                if (DateTime.Compare(value, (DateTime) datatype.ChangeType(facets.MinExclusive, typeof(DateTime))) <= 0)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinExclusive, facets.MinInclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                if (DateTime.Compare(value, (DateTime) datatype.ChangeType(facets.MaxExclusive, typeof(DateTime))) >= 0)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxExclusive, facets.MaxExclusive, value);
                }
            }

            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                if (DateTime.Compare(value, (DateTime) datatype.ChangeType(facets.MaxInclusive, typeof(DateTime))) > 0)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxInclusive, facets.MaxInclusive, value);
                }
            }


            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((DateTime) datatype.ChangeType(value, typeof(DateTime)), enumeration, datatype);
        }

        private bool MatchEnumeration(DateTime value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            foreach (DateTime correctValue in enumeration)
            {
                if (DateTime.Compare(value, correctValue) == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class StringFacetsChecker : FacetsChecker
    {
        //All types derived from string & anyURI
        static Regex languagePattern;

        static Regex LanguagePattern
        {
            get
            {
                if (languagePattern == null)
                {
                    Regex langRegex = new Regex("^([a-zA-Z]{1,8})(-[a-zA-Z0-9]{1,8})*$", RegexOptions.None);
                    Interlocked.CompareExchange(ref languagePattern, langRegex, null);
                }

                return languagePattern;
            }
        }

        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            if (type.RestrictionFacets == null || !type.RestrictionFacets.HasValueFacets)
            {
                return null;
            }

            XmlSchemaDatatype datatype = type.DataType;
            string stringValue = null;
            if (type.DataType.TypeCode == XmlTypeCode.AnyUri)
            {
                stringValue = ((Uri) datatype.ChangeType(value, typeof(Uri))).OriginalString;
            }
            else
            {
                stringValue = (string) datatype.ChangeType(value, XTypedServices.typeOfString);
            }

            return CheckValueFacets(stringValue, type);
        }

        internal override Exception CheckValueFacets(string value, SimpleTypeValidator type)
        {
            return CheckValueFacets(value, type, true);
        }

        internal Exception CheckValueFacets(string value, SimpleTypeValidator type, bool verifyUri)
        {
            //Length, MinLength, MaxLength
            int length = value.Length;
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null)
            {
                return null;
            }

            RestrictionFlags flags = facets.Flags;
            XmlSchemaDatatype datatype = type.DataType;

            Exception exception;

            exception = CheckBuiltInFacets(value, datatype.TypeCode, verifyUri);
            if (exception != null) return exception;

            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;


                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }

            if ((flags & RestrictionFlags.Length) != 0)
            {
                if (length != facets.Length)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Length, facets.Length, value);
                }
            }

            if ((flags & RestrictionFlags.MaxLength) != 0)
            {
                if (length > facets.MaxLength)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxLength, facets.MaxLength, value);
                }
            }

            if ((flags & RestrictionFlags.MinLength) != 0)
            {
                if (length < facets.MinLength)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinLength, facets.MinLength, value);
                }
            }

            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((string) datatype.ChangeType(value, XTypedServices.typeOfString), enumeration,
                datatype);
        }

        private bool MatchEnumeration(string value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            if (datatype.TypeCode == XmlTypeCode.AnyUri)
            {
                foreach (Uri correctValue in enumeration)
                {
                    if (value.Equals(correctValue.OriginalString))
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (string correctValue in enumeration)
                {
                    if (value.Equals(correctValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Exception CheckBuiltInFacets(string s, XmlTypeCode typeCode, bool verifyUri)
        {
            Exception exception = null;

            switch (typeCode)
            {
                case XmlTypeCode.AnyUri:

                    if (verifyUri)
                    {
                        Uri uri = null;
                        exception = XmlConvertExt.TryToUri(s, out uri);
                    }

                    break;

                case XmlTypeCode.NormalizedString:
                    exception = XmlConvertExt.VerifyNormalizedString(s);
                    break;

                case XmlTypeCode.Token:
                    try
                    {
                        XmlConvert.VerifyTOKEN(s);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }

                    break;

                case XmlTypeCode.Language:
                    if (s == null || s.Length == 0)
                    {
                        exception = new LinqToXsdException();
                    }

                    if (!LanguagePattern.IsMatch(s))
                    {
                        exception = new LinqToXsdException();
                    }

                    break;

                case XmlTypeCode.NmToken:
                    try
                    {
                        XmlConvert.VerifyNMTOKEN(s);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }

                    break;

                case XmlTypeCode.Name:
                    try
                    {
                        XmlConvert.VerifyName(s);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }

                    break;

                case XmlTypeCode.NCName:
                case XmlTypeCode.Id:
                case XmlTypeCode.Idref:
                case XmlTypeCode.Entity:
                    try
                    {
                        XmlConvert.VerifyNCName(s);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }

                    break;
                default:
                    break;
            }

            return exception;
        }
    }

    internal class QNameFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            if (type.RestrictionFacets == null || !type.RestrictionFacets.HasValueFacets)
            {
                return null;
            }

            XmlSchemaDatatype datatype = type.DataType;
            XmlQualifiedName qualifiedNameValue =
                (XmlQualifiedName) datatype.ChangeType(value, typeof(XmlQualifiedName));
            return CheckValueFacets(qualifiedNameValue, type);
        }

        internal override Exception CheckValueFacets(XmlQualifiedName value, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasValueFacets)
            {
                return null;
            }


            if (facets == null)
            {
                return null;
            }

            RestrictionFlags flags = facets.Flags;
            XmlSchemaDatatype datatype = type.DataType;


            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;


                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }

            string strValue = value.ToString();
            int length = strValue.Length;
            if ((flags & RestrictionFlags.Length) != 0)
            {
                if (length != facets.Length)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Length, facets.Length, value);
                }
            }

            if ((flags & RestrictionFlags.MaxLength) != 0)
            {
                if (length > facets.MaxLength)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxLength, facets.MaxLength, value);
                }
            }

            if ((flags & RestrictionFlags.MinLength) != 0)
            {
                if (length < facets.MinLength)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinLength, facets.MinLength, value);
                }
            }

            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((XmlQualifiedName) datatype.ChangeType(value, typeof(XmlQualifiedName)),
                enumeration, datatype);
        }

        private bool MatchEnumeration(XmlQualifiedName value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            foreach (XmlQualifiedName correctValue in enumeration)
            {
                if (value.Equals(correctValue))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class MiscFacetsChecker : FacetsChecker
    {
        //For bool, anySimpleType
    }

    internal class BinaryFacetsChecker : FacetsChecker
    {
        //hexBinary & Base64Binary

        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            if (type.RestrictionFacets == null || !type.RestrictionFacets.HasValueFacets)
            {
                return null;
            }

            byte[] byteArrayValue = (byte[]) value;
            return CheckValueFacets(byteArrayValue, type);
        }

        internal override Exception CheckValueFacets(byte[] value, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasValueFacets)
            {
                return null;
            }

            //Length, MinLength, MaxLength

            int length = value.Length;
            RestrictionFlags flags = facets.Flags;
            XmlSchemaDatatype datatype = type.DataType;


            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;


                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }

            if ((flags & RestrictionFlags.Length) != 0)
            {
                if (length != facets.Length)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Length, facets.Length, value);
                }
            }

            if ((flags & RestrictionFlags.MaxLength) != 0)
            {
                if (length > facets.MaxLength)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxLength, facets.MaxLength, value);
                }
            }

            if ((flags & RestrictionFlags.MinLength) != 0)
            {
                if (length < facets.MinLength)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinLength, facets.MinLength, value);
                }
            }

            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            return MatchEnumeration((byte[]) value, enumeration, datatype);
        }

        private bool MatchEnumeration(byte[] value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            foreach (byte[] correctValue in enumeration)
            {
                if (IsEqual(value, correctValue))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsEqual(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length) return false;

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal class ListFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasValueFacets)
            {
                return null;
            }

            //Check for facets allowed on lists - Length, MinLength, MaxLength
            //value is a list
            IList listValue = null;
            Exception e = ListSimpleTypeValidator.ToList(value, ref listValue);
            if (e != null) return e;

            int length = listValue.Count;

            XmlSchemaDatatype datatype = type.DataType;
            RestrictionFlags flags = facets.Flags;

            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;

                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }

            if ((flags & RestrictionFlags.Length) != 0)
            {
                if (length != facets.Length)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Length, facets.Length, value);
                }
            }

            if ((flags & RestrictionFlags.MaxLength) != 0)
            {
                if (length > facets.MaxLength)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MaxLength, facets.MaxLength, value);
                }
            }

            if ((flags & RestrictionFlags.MinLength) != 0)
            {
                if (length < facets.MinLength)
                {
                    return new LinqToXsdFacetException(RestrictionFlags.MinLength, facets.MinLength, value);
                }
            }

            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            string strValue = ListFormatter.ToString(value);
            foreach (object correctArray in enumeration)
            {
                if (strValue.Equals(correctArray))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class UnionFacetsChecker : FacetsChecker
    {
        internal override Exception CheckValueFacets(object value, SimpleTypeValidator type)
        {
            RestrictionFacets facets = type.RestrictionFacets;
            if (facets == null || !facets.HasValueFacets)
            {
                return null;
            }

            RestrictionFlags flags = facets.Flags;
            XmlSchemaDatatype datatype = type.DataType;

            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                ArrayList enums = facets.Enumeration;

                if (!MatchEnumeration(value, enums, datatype))
                {
                    return new LinqToXsdFacetException(RestrictionFlags.Enumeration,
                        facets.Enumeration,
                        value);
                }
            }

            return null;
        }

        internal override bool MatchEnumeration(object value, ArrayList enumeration, XmlSchemaDatatype datatype)
        {
            foreach (object correctValue in enumeration)
            {
                if (value.Equals(correctValue))
                {
                    return true;
                }
            }

            return false;
        }
    }
}