//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Collections;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Xml.Schema.Linq
{
    public class RestrictionFacets
    {
        internal int Length;
        internal int MinLength;
        internal int MaxLength;
        internal ArrayList Patterns;
        internal ArrayList Enumeration;
        internal XmlSchemaWhiteSpace WhiteSpace;
        internal object MaxInclusive;
        internal object MaxExclusive;
        internal object MinInclusive;
        internal object MinExclusive;
        internal int TotalDigits;
        internal int FractionDigits;
        internal RestrictionFlags Flags = 0;

        private bool hasValueFacets;

        public bool HasValueFacets
        {
            get { return hasValueFacets; }
        }

        private bool hasLexicalFacets;

        public bool HasLexicalFacets
        {
            get { return hasLexicalFacets; }
        }

        public RestrictionFacets(RestrictionFlags flags,
            object[] enumeration,
            int fractionDigits,
            int length,
            object maxExclusive,
            object maxInclusive,
            int maxLength,
            object minExclusive,
            object minInclusive,
            int minLength,
            string[] patterns,
            int totalDigits,
            XmlSchemaWhiteSpace whiteSpace)
        {
            hasValueFacets = false;
            hasLexicalFacets = false;
            if ((flags & RestrictionFlags.Enumeration) != 0)
            {
                this.Enumeration = new ArrayList();
                foreach (object o in enumeration)
                {
                    this.Enumeration.Add(o);
                }

                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.FractionDigits) != 0)
            {
                this.FractionDigits = fractionDigits;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.Length) != 0)
            {
                this.Length = length;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.MaxExclusive) != 0)
            {
                this.MaxExclusive = maxExclusive;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.MaxInclusive) != 0)
            {
                this.MaxInclusive = maxInclusive;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.MaxLength) != 0)
            {
                this.MaxLength = maxLength;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.MinExclusive) != 0)
            {
                this.MinExclusive = minExclusive;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.MinInclusive) != 0)
            {
                this.MinInclusive = minInclusive;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.MinLength) != 0)
            {
                this.MinLength = minLength;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.Pattern) != 0)
            {
                CompilePatterns(patterns);
                hasLexicalFacets = true;
            }

            if ((flags & RestrictionFlags.TotalDigits) != 0)
            {
                this.TotalDigits = totalDigits;
                hasValueFacets = true;
            }

            if ((flags & RestrictionFlags.WhiteSpace) != 0)
            {
                hasLexicalFacets = true;
                this.WhiteSpace = whiteSpace;
            }

            this.Flags = flags;
        }


        internal void CompilePatterns(string[] patternStrs)
        {
            if (Patterns == null)
            {
                Patterns = new ArrayList();
            }
            else
            {
                Patterns.Clear();
            }

            foreach (string str in patternStrs)
            {
                Patterns.Add(new Regex(str));
            }
        }
    }

    public class SimpleTypeValidator
    {
        private RestrictionFacets restrictionFacets;
        private XmlSchemaDatatype dataType;
        private XmlSchemaDatatypeVariety variety;
        internal FacetsChecker facetsChecker;

        internal SimpleTypeValidator(XmlSchemaDatatypeVariety variety,
            XmlSchemaSimpleType type,
            FacetsChecker facetsChecker,
            RestrictionFacets facets)
        {
            this.restrictionFacets = facets;
            this.facetsChecker = facetsChecker;
            this.dataType = type.Datatype;
            this.variety = variety;
        }

        internal XmlSchemaDatatype DataType
        {
            get { return dataType; }
        }

        internal XmlSchemaDatatypeVariety Variety
        {
            get { return variety; }
        }

        internal RestrictionFacets RestrictionFacets
        {
            get { return restrictionFacets; }
        }

        internal virtual Exception TryParseValue(object value,
            NameTable nameTable,
            XNamespaceResolver resolver,
            out SimpleTypeValidator matchingType,
            out object typedValue)
        {
            throw new InvalidOperationException();
        }

        internal virtual Exception TryParseString(object value,
            NameTable nameTable,
            XNamespaceResolver resolver,
            out string parsedString)
        {
            throw new InvalidOperationException();
        }
    }

    public class AtomicSimpleTypeValidator : SimpleTypeValidator
    {
        public AtomicSimpleTypeValidator(XmlSchemaSimpleType type,
            RestrictionFacets facets)
            : base(XmlSchemaDatatypeVariety.Atomic,
                type,
                FacetsChecker.GetFacetsChecker(type.Datatype.TypeCode),
                facets)
        {
        }

        internal override Exception TryParseString(object value,
            NameTable nameTable,
            XNamespaceResolver resolver,
            out string parsedString)
        {
            parsedString = value as string;
            if (parsedString == null)
            {
                try
                {
                    parsedString = XTypedServices.GetXmlString(value, DataType, null);
                }
                catch (Exception e)
                {
                    return e;
                }
            }

            return null;
        }

        private Exception TryMatchAtomicType(object value, NameTable nameTable, XNamespaceResolver resolver)
        {
            try
            {
                XTypedServices.TryConvert(value, DataType, resolver);
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        internal override Exception TryParseValue(object value,
            NameTable nameTable,
            XNamespaceResolver resolver,
            out SimpleTypeValidator matchingType,
            out object typedValue)
        {
            Exception e = TryMatchAtomicType(value, nameTable, resolver);
            matchingType = null;
            typedValue = null;
            if (e != null) return e;
            try
            {
                if (RestrictionFacets != null && RestrictionFacets.HasLexicalFacets)
                {
                    e = TryParseString(value, nameTable, resolver, out var parsedString)
                        ?? facetsChecker.CheckLexicalFacets(ref parsedString, value, nameTable, resolver, this);
                }

                e ??= facetsChecker.CheckValueFacets(value, this);

                if (e == null)
                {
                    matchingType = this;
                    typedValue = XTypedServices.Convert(value, DataType);
                }

                return e;
            }
            catch (Exception ee)
            {
                return ee;
            }
        }
    }

    public class ListSimpleTypeValidator : SimpleTypeValidator
    {
        SimpleTypeValidator itemType;

        internal SimpleTypeValidator ItemType
        {
            get { return itemType; }
        }

        public ListSimpleTypeValidator(XmlSchemaSimpleType type,
            RestrictionFacets facets,
            SimpleTypeValidator itemType)
            : base(XmlSchemaDatatypeVariety.List, type, FacetsChecker.ListFacetsChecker, facets)
        {
            this.itemType = itemType;
        }

        internal override Exception TryParseString(object value,
            NameTable nameTable,
            XNamespaceResolver resolver,
            out string parsedString)
        {
            parsedString = value as string;

            if (parsedString != null) return null;

            IEnumerable list = value as IEnumerable;
            if (list == null) return new InvalidCastException();

            StringBuilder bldr = new StringBuilder();
            foreach (object o in list)
            {
                // Separate values by single space character
                if (bldr.Length != 0)
                    bldr.Append(' ');

                // Append string value of next item in the list
                string s = null;
                Exception ie = itemType.TryParseString(o, nameTable, resolver, out s);
                if (ie == null)
                {
                    bldr.Append(s);
                }
                else
                {
                    return ie;
                }
            }

            parsedString = bldr.ToString();


            return null;
        }


        internal override Exception TryParseValue(object value,
            NameTable nameTable,
            XNamespaceResolver resolver,
            out SimpleTypeValidator matchingType,
            out object typedValue)
        {
            //Only accepts string and IEnumerable
            Exception e = null;
            typedValue = null;
            matchingType = null;


            if (RestrictionFacets != null && RestrictionFacets.HasLexicalFacets)
            {
                string parsedString = null;
                e = TryParseString(value, nameTable, resolver, out parsedString);
                if (e == null) e = facetsChecker.CheckLexicalFacets(ref parsedString, value, nameTable, resolver, this);
            }

            if (e == null) e = facetsChecker.CheckValueFacets(value, this);
            if (e != null) return e;

            //Check item type level
            IList listItems = null;
            e = ToList(value, ref listItems);
            if (e != null) return e;

            foreach (object listItem in listItems)
            {
                object typedItemValue = null;
                SimpleTypeValidator itemMatchingType = null;
                e = itemType.TryParseValue(listItem, nameTable, resolver, out itemMatchingType, out typedItemValue);
                if (e != null) return e;
            }

            //Passed all the restriction checks
            typedValue = listItems;
            matchingType = this;
            return null;
        }

        //Two helper functions for the two-way conversions between list and string
        internal static Exception ToList(object value, ref IList list)
        {
            Exception e = null;
            if (value is IList)
            {
                list = (IList) value;
            }
            else if (value is string)
            {
                string strValue = value as string;
                string[] items = strValue.Split(' ');
                list = new List<object>(items);
            }
            else e = new InvalidCastException();

            return e;
        }
    }

    public class UnionSimpleTypeValidator : SimpleTypeValidator
    {
        SimpleTypeValidator[] memberTypes;

        internal SimpleTypeValidator[] MemberTypes
        {
            get { return memberTypes; }
        }

        public UnionSimpleTypeValidator(XmlSchemaSimpleType type,
            RestrictionFacets facets,
            SimpleTypeValidator[] memberTypes)
            : base(XmlSchemaDatatypeVariety.Union, type, FacetsChecker.UnionFacetsChecker, facets)
        {
            this.memberTypes = memberTypes;
        }

        internal override Exception TryParseValue(object value,
            NameTable nameTable,
            XNamespaceResolver resolver,
            out SimpleTypeValidator matchingType,
            out object typedValue)
        {
            typedValue = null;
            matchingType = null;
            if (value == null) return new ArgumentNullException("Argument value should not be null.");

            object typedMemberValue = null;
            foreach (SimpleTypeValidator memberType in memberTypes)
            {
                if (memberType.TryParseValue(value, nameTable, resolver, out matchingType, out typedMemberValue) ==
                    null)
                {
                    break;
                }
            }

            if (typedMemberValue == null)
            {
                return new UnionMemberTypeNotFoundException(value, this);
            }
            else
            {
                Exception e = null;
                if (RestrictionFacets != null && RestrictionFacets.HasLexicalFacets)
                {
                    string parsedString = null;
                    e = matchingType.TryParseString(value, nameTable, resolver, out parsedString);
                    if (e == null)
                        e = facetsChecker.CheckLexicalFacets(ref parsedString, value, nameTable, resolver, this);
                }

                if (e == null) e = facetsChecker.CheckValueFacets(typedMemberValue, this);
                if (e != null) return e;
                else
                {
                    typedValue = typedMemberValue;
                    return null;
                }
            }
        }
    }
}