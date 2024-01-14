//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Xml.Schema.Linq
{
    public class LinqToXsdException : Exception
    {
        public LinqToXsdException(string errorMsg)
            : base(errorMsg)
        {
        }

        public LinqToXsdException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public LinqToXsdException() : base()
        {
        }

        public LinqToXsdException(string propertyName, string reason) :
            base($"Failed to set value on the property \"{propertyName}\". Possible reason: {reason}")
        {
        }


        protected static string CreateMessage(string facetName, string facetValue, string value)
        {
            return $"The Given Value {value} Violates Restrictions: {facetName} = {facetValue}";
        }

        protected static string ConvertIEnumToString(IEnumerable value)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append('(');
            foreach (object o in value)
            {
                if (strBuilder.Length > 1)
                    strBuilder.Append(' ');

                strBuilder.Append(o is IEnumerable e and not string
                    ? ConvertIEnumToString(e)
                    : o.ToString());
            }
            strBuilder.Append(')');

            return strBuilder.ToString();
        }

        protected static string CreateMessage(string facetName, object facetValue, object value)
        {
            return CreateMessage(
                facetName,
                facetValue is IEnumerable e1 and not string
                    ? ConvertIEnumToString(e1)
                    : facetValue.ToString(),
                value is IEnumerable e2 and not string
                    ? ConvertIEnumToString(e2)
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
            var codes = new List<string>(typeDef.MemberTypes.Length);

            foreach (SimpleTypeValidator type in typeDef.MemberTypes)
            {
                codes.Add(type.DataType.TypeCode.ToString());
            }

            return codes;
        }
    }
}