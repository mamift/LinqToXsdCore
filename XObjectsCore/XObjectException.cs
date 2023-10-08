//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Xml.Schema.Linq
{
    public class LinqToXsdException : Exception
    {
        public LinqToXsdException() : base()
        {
        }

        public LinqToXsdException(string errorMsg)
            : base(errorMsg)
        {
        }

        public LinqToXsdException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public LinqToXsdException(string propertyName, string reason) :
            base("Failed to set value on the property \"" + propertyName
                                                          + "\". Possible reason: " + reason)
        {
        }
        
        protected static string CreateMessage(string facetName, string facetValue, string value)
        {
            return "The Given Value " + value + " Violates Restrictions: "
                   + facetName + " = " + facetValue;
        }

        protected static string ConvertIEnumToString(IEnumerable value)
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (object o in value)
            {
                if (strBuilder.Length != 0)
                    strBuilder.Append(' ');

                strBuilder.Append((o is IEnumerable && !(o is string))
                    ? ConvertIEnumToString(o as IEnumerable)
                    : o.ToString());
            }

            strBuilder.Insert(0, '(');
            strBuilder.Append(')');

            return strBuilder.ToString();
        }

        protected static string CreateMessage(string facetName,
            object facetValue,
            object value)
        {
            return CreateMessage(facetName,
                (facetValue is IEnumerable && !(facetValue is string))
                    ? ConvertIEnumToString(facetValue as IEnumerable)
                    : facetValue.ToString(),
                (value is IEnumerable && !(facetValue is string))
                    ? ConvertIEnumToString(value as IEnumerable)
                    : value.ToString());
        }
    }

    public class LinqToXsdFacetException : LinqToXsdException
    {
        public LinqToXsdFacetException(RestrictionFlags flags,
            object facetValue,
            object value)
            : base(CreateMessage(flags.ToString(), facetValue, value))
        {
        }
    }

    public class LinqToXsdFixedValueException : LinqToXsdException
    {
        public LinqToXsdFixedValueException(object value, object fixedValue)
            : base(CreateMessage("Checking Fixed Value Failed", fixedValue, value))
        {
        }
    }

    internal class UnionMemberTypeNotFoundException : LinqToXsdException
    {
        public UnionMemberTypeNotFoundException(object value, UnionSimpleTypeValidator typeDef)
            : base(CreateMessage("Union Type: No Matching Member Type Was Found. Valid Types ",
                GetMemberTypeCodes(typeDef), value))
        {
        }

        private static List<string> GetMemberTypeCodes(UnionSimpleTypeValidator typeDef)
        {
            List<string> codes = new List<string>();

            foreach (SimpleTypeValidator type in typeDef.MemberTypes)
            {
                codes.Add(type.DataType.TypeCode.ToString());
            }

            return codes;
        }
    }
}