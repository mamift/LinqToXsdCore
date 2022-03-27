//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Xml.Schema.Linq.CodeGen
{
    public static class StringExtensions
    {
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