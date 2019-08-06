using System;
using System.Collections.Generic;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq
{
    /// <summary>
    /// Compares <see cref="Namespace"/> objects (from <see cref="Configuration"/> instances).
    /// </summary>
    internal class NamespaceEqualityValueComparer: IEqualityComparer<Namespace>
    {
        public bool Equals(Namespace x, Namespace y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));
            return x.Schema.ToString() == y.Schema.ToString();
        }

        public int GetHashCode(Namespace obj)
        {
            var schemaUriStr = obj.Schema.ToString();
            int uriHashCode;
            if (schemaUriStr.IsEmpty())
                uriHashCode = 0;
            else
                uriHashCode = !obj.Schema.IsAbsoluteUri ? schemaUriStr.GetHashCode() : obj.Schema.AbsoluteUri.GetHashCode();

            return uriHashCode;
        }
    }
}