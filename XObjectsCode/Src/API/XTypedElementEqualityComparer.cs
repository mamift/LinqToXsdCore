using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    /// <summary>
    /// Deep <see cref="IEqualityComparer{T}"/> for the <see cref="XTypedElement"/> class.
    /// <para>Invokes <see cref="XNode.DeepEquals"/>.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XTypedElementDeepEqualityComparer<T> : IEqualityComparer<T>
        where T: XTypedElement
    {
        public bool Equals(T x, T y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            return XNode.DeepEquals(x.Untyped, y.Untyped);
        }

        public int GetHashCode(T obj) => obj.Untyped.GetHashCode();

        public static IEqualityComparer<T> Default { get; } = new XTypedElementDeepEqualityComparer<T>();
    }

    /// <summary>
    /// Shallow <see cref="IEqualityComparer{T}"/> for the <see cref="XTypedElement"/> class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XTypedElementEqualityComparer<T> : IEqualityComparer<T>
        where T: XTypedElement
    {
        public bool Equals(T x, T y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            return x.Untyped == y.Untyped;
        }

        public int GetHashCode(T obj) => obj.Untyped.GetHashCode();

        public static IEqualityComparer<T> Default { get; } = new XTypedElementEqualityComparer<T>();
    }
}