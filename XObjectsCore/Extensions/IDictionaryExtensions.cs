using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xml.Schema.Linq.Extensions
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// https://stackoverflow.com/questions/13470335/comparing-two-dictionaries-for-equal-data-in-c
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsEquals<TKey, TVal>(this IDictionary<TKey, TVal> a, IDictionary<TKey, TVal> b)
        {
            if (a == null || b == null) return false;
            if (a.Keys.Count != b.Keys.Count) return false;
            if (a.Values.Count != b.Values.Count) return false;

            return a.Keys.All(k => b.ContainsKey(k) && object.Equals(b[k], a[k]));
        }

        /// <summary>
        /// Creates a new <see cref="KeyValuePair{TKey,TValue}"/> based on the current <see cref="DictionaryEntry"/>.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionaryEntry"></param>
        /// <returns></returns>
        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(this DictionaryEntry dictionaryEntry)
        {
            var castedKey = (TKey) dictionaryEntry.Key;
            var castedValue = (TValue) dictionaryEntry.Value;
            return new KeyValuePair<TKey, TValue>(castedKey, castedValue);
        }
    }
}