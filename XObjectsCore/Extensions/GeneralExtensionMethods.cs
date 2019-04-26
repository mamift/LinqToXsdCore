using System;
using System.Collections.Generic;
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
        /// Generic ToString method that will execute a given <see cref="Func{TResult}"/> that accepts the current object as a sole parameter and
        /// returns a string.
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
        /// Converts the current <paramref name="sequence"/> into a delimited string.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="sequence"/> is <see langword="null"/></exception>
        public static string ToDelimitedString<TType>(this IEnumerable<TType> sequence, char delimiter = ',')
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));

            var stringBuilder = new StringBuilder();
            var enumeratedSequence = sequence as TType[] ?? sequence.ToArray();

            var count = enumeratedSequence.Length;
            for (var i = 0; i < count; i++)
            {
                stringBuilder.Append(enumeratedSequence.ElementAt(i));
                if (i == (count - 1)) break;
                stringBuilder.Append(delimiter);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the current <paramref name="sequence"/> into a delimited string.
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
    }
}
