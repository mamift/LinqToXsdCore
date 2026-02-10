//Copyright (c) Microsoft Corporation.  All rights reserved.
#nullable enable

using System.Linq;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.CodeGen
{
    public static class StringExtensions
    {
        public static string ToLowerCaseFirstChar(this string str)
        {
            char[] asCharArray = str.ToArray();
            char first = char.ToLower(asCharArray[0]);

            asCharArray[0] = first;

            return new string(asCharArray);
        }

        /// <summary>
        /// Equivalent to T-SQL COALESCE function for strings.
        /// <seealso cref="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/coalesce-transact-sql?view=sql-server-ver17"/>
        /// <para>Unlike the T-SQL function, this will use <see cref="string.IsNullOrWhiteSpace"/> for checking emptiness.</para>
        /// </summary>
        /// <param name="str"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static string? Coalesce(this string? str, params string?[] others)
        {
            var list = new[] { str }.Concat(others);

            foreach (var item in list) {
                if (item.IsEmpty()) {
                    continue;
                }

                return item;
            }

            return null;
        }

        /// <summary>
        /// Converts the first character of the given Unicode string to its uppercase equivalent using the casing rules of the invariant culture.
        /// </summary>
        /// <param name="self">The Unicode string to convert.</param>
        /// <returns></returns>
        public static string ToUpperFirstInvariant(this string self)
        {
            if (!string.IsNullOrEmpty(self) && self.Length > 0 && char.IsLower(self[0]))
            {
                return char.ToUpperInvariant(self[0]) + self.Substring(1);
            }
            return self;
        }
    }
}