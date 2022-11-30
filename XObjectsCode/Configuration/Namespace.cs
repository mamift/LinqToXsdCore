using System;
using Xml.Schema.Linq.Extensions;
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
            Uri possibleSchemaUriInstance;

            if (schemaUri.IsEmpty()) {
                possibleSchemaUriInstance = null;
            }
            else {
                if (Uri.IsWellFormedUriString(schemaUri, UriKind.Absolute)) {
                    possibleSchemaUriInstance = new Uri(schemaUri);
                }
                else {
                    possibleSchemaUriInstance = new Uri("urn:" + schemaUri);
                }
            }
            
            return new Namespace {
                DefaultVisibility = visibility.ToKeyword(),
                Schema = possibleSchemaUriInstance,
                Clr = clrNamespace
            };
        }
    }
}