//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml.Schema;

namespace Xml.Schema.Linq
{
    /// <summary>
    /// Represents a mapping between an XML schema enumeration value and its corresponding CLR enum value.
    /// </summary>
    /// <remarks>This class is used to parse <see cref="RestrictionFacets.Enumeration"/> values, which may include
    /// both the XML schema value and the CLR enum value separated by a colon (e.g., "Value:Member").
    /// The last colon is treated as the separator, so schema values may themselves contain colons.
    /// If no colon is present, the value is assumed to be the same as the member name.</remarks>
    public class EnumFacetMapping
    {
        public static EnumFacetMapping Parse(object value)
        {
            if (value is string strValue)
            {
                return new EnumFacetMapping(strValue);
            }
            else if (value is XmlSchemaEnumerationFacet enumFacet)
            {
                return new EnumFacetMapping(enumFacet.Value);
            }
            else
            {
                throw new InvalidCastException($"Cannot convert {value} to {nameof(EnumFacetMapping)}");
            }
        }

        private EnumFacetMapping(string value)
        {
            // Split on the last colon: enum member names are valid identifiers and can never contain
            // a colon, whereas the schema value itself may (e.g. "12:30"). Splitting on the first
            // colon would truncate such values.
            var separatorIndex = value.LastIndexOf(':');
            if (separatorIndex >= 0)
            {
                Value  = value.Substring(0, separatorIndex);
                Member = value.Substring(separatorIndex + 1);
            }
            else
            {
                Value = Member = value;
            }
        }

        /// <summary>
        /// Represents the XML schema value of the enumeration facet.
        /// </summary>
        public string Value  { get; }
        /// <summary>
        /// Represents the CLR enum member name corresponding to the XML schema value.
        /// </summary>
        public string Member { get; }

        public override string ToString() => Value == Member ? Value : $"{Value}:{Member}";
    }
}