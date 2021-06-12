using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Xml.Schema.Linq.Extensions
{
    public static class GeneralExtensionMethods
    {
        /// <summary>
        /// Converts an <see cref="XmlReader"/>s to an <see cref="XmlSchemaSet"/>, assuming the reader points to an XML Schema file.
        /// </summary>
        /// <param name="reader">The current <see cref="XmlReader"/>.</param>
        /// <param name="resolver">Add a custom <see cref="XmlResolver"/>. Defaults to using an <see cref="XmlUrlResolver"/>.</param>
        /// <returns></returns>
        public static XmlSchemaSet ToXmlSchemaSet(this XmlReader reader, XmlResolver resolver = null)
        {
            var xmlResolver = resolver ?? new XmlUrlResolver();
            var newXmlSet = new XmlSchemaSet {
                XmlResolver = xmlResolver
            };

            newXmlSet.Add(null, reader);
            newXmlSet.Compile();

            return newXmlSet;
        }

        /// <summary>
        /// Converts an <see cref="XmlReader"/>s to an <see cref="XmlSchema"/>, assuming the reader points to an XML Schema file.
        /// </summary>
        /// <param name="reader">The current <see cref="XmlReader"/>.</param>
        /// <returns></returns>
        public static XmlSchema ToXmlSchema(this XmlReader reader)
        {
            return XmlSchema.Read(reader, (sender, args) => {
                if (args.Severity == XmlSeverityType.Error) {
                    throw args.Exception;
                }
            });
        }

        /// <summary>
        /// Execute a <see cref="Func{TResult}"/> on the current <paramref name="@object"/>, that returns a string.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="object"></param>
        /// <param name="functor"></param>
        /// <returns></returns>
        public static string ToString<TType>(this TType @object, Func<TType, string> functor)
        {
            var result = functor(@object);

            return result;
        }

        /// <summary>
        /// Converts the current <paramref name="sequence"/> into a delimited string, whereby the
        /// <paramref name="delimiter"/> is a given <see cref="char"/>.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="delimiter">The delimiter string. Defaults to a comma.</param>
        /// <param name="delimitAfterLast">By default the <paramref name="delimiter"/> is not appended after the last element in the <paramref name="sequence"/>.</param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="sequence"/> is <see langword="null"/></exception>
        public static string ToDelimitedString<TType>(this IEnumerable<TType> sequence, char delimiter = ',', bool delimitAfterLast = false) 
            => sequence.ToDelimitedString($"{delimiter}", delimitAfterLast);

        /// <summary>
        /// Converts the current <paramref name="sequence"/> into a delimited string, whereby the
        /// <paramref name="delimiter"/> is a given <see cref="string"/>.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="delimiter">The delimiter string. Defaults to a comma.</param>
        /// <param name="delimitAfterLast">By default the <paramref name="delimiter"/> is not appended after the last element in the <paramref name="sequence"/>.</param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="sequence"/> is <see langword="null"/></exception>
        public static string ToDelimitedString<TType>(this IEnumerable<TType> sequence, string delimiter = ",", bool delimitAfterLast = false)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));

            var stringBuilder = new StringBuilder();
            var enumeratedSequence = sequence as List<TType> ?? sequence.ToList();

            var count = enumeratedSequence.Count;
            for (var i = 0; i < count; i++) {
                stringBuilder.Append(enumeratedSequence.ElementAt(i));
                var isTheLast = i == (count - 1);
                if (isTheLast && !delimitAfterLast) break;
                stringBuilder.Append(delimiter);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the current <paramref name="sequence"/> into a delimited string by executing a <paramref name="functor"/> on each
        /// item in the sequence.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="functor">A <see cref="Func{TResult}"/> to execute on each element in the <paramref name="sequence"/> which itself wraps the call to <c>ToString()</c>.</param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="sequence"/> is <see langword="null"/></exception>
        public static string ToDelimitedString<TType>(this IEnumerable<TType> sequence, Func<TType, string> functor, char delimiter = ',')
        {
            if (functor == null) throw new ArgumentNullException(nameof(functor));

            return ToDelimitedString(sequence.Select(functor), delimiter);
        }

        /// <summary>
        /// Tries to get a value for a given key, otherwise returns default value. No exceptions.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="tsDictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue ValueForKey<TKey, TValue>(this IDictionary<TKey, TValue> tsDictionary, TKey key)
        {
            if (tsDictionary.Count == 0) return default(TValue);
            try {
                return tsDictionary[key];
            }
            catch {
                return default(TValue);
            }
        }

        /// <summary>
        /// Extracts all global/top-level schema items in the current <see cref="XmlSchemaSet"/> to a single instance of an
        /// <see cref="XmlSchema"/>.
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        public static XmlSchema ExtractGlobalItemsToSingleFileSchema(this XmlSchemaSet set)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (!set.IsCompiled)
                throw new Exception($"{nameof(ExtractGlobalItemsToSingleFileSchema)}() only works on compiled schema sets");

            var newSchema = new XmlSchema();

            foreach (DictionaryEntry de in set.GlobalElements) {
                var globalElement = (XmlSchemaObject) de.Value;
                newSchema.Items.Add(globalElement);
            }

            foreach (DictionaryEntry de in set.GlobalTypes) {
                XmlSchemaType globalType = (XmlSchemaType) de.Value;
                newSchema.Items.Add(globalType);
            }

            foreach (DictionaryEntry de in set.GlobalAttributes) {
                XmlSchemaAttribute globalAttr = (XmlSchemaAttribute) de.Value;
                newSchema.Items.Add(globalAttr);
            }

            return newSchema;
        }

        /// <summary>
        /// Returns the current schema as an XML string.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static string ToXmlString(this XmlSchema schema)
        {
            var writer = new StringWriter();

            schema.Write(writer);

            return writer.ToString();
        }
    }
}
