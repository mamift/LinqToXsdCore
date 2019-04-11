using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    public class XAttributeNameValueEqualityComparer: IEqualityComparer<XAttribute>
    {
        public bool Equals(XAttribute left, XAttribute right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            var sameNsName = left.Name.NamespaceName == right.Name.NamespaceName;
            var sameLocalName = left.Name.LocalName == right.Name.LocalName;
            var sameNs = left.Name.Namespace.ToString() == right.Name.Namespace.ToString();

            return sameNs && sameLocalName && sameNsName;
        }

        public int GetHashCode(XAttribute obj)
        {
            var ns = obj.Name.Namespace.NamespaceName.GetHashCode();
            var localName = obj.Name.LocalName.GetHashCode();
            var nsName = obj.Name.NamespaceName.GetHashCode();
            unchecked
            {
                return ((ns + localName + nsName) ^ 17) * 
                       (obj.Value.GetHashCode() ^ 17);
            }
        }
    }
}