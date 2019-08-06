using System;
using XObjects;

namespace Xml.Schema.Linq
{
    internal partial class @Namespace
    {
        /// <summary>
        /// Creates a new <see cref="Namespace"/> instance from given values. Defaults to <see cref="GeneratedTypesVisibility.Public"/> <paramref name="visibility"/>.
        /// </summary>
        /// <param name="schemaUri"></param>
        /// <param name="clrNamespace"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        public static Namespace New(string schemaUri, string clrNamespace, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            return new Namespace {
                DefaultVisibility = visibility.ToKeyword(),
                Schema = new Uri(schemaUri),
                Clr = clrNamespace
            };
        }
    }
}