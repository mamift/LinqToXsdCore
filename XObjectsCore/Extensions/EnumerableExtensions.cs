using System.Collections.Generic;
using System.Linq;

namespace Xml.Schema.Linq.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Returns an array where the default values of <typeparamref name="T"/> are removed in the resultant array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    public static T[] ToNoDefaultArray<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Where(e => !EqualityComparer<T>.Default.Equals(e, default(T))).ToArray();
    }
}