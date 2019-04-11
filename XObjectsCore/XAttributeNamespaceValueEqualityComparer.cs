using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    public class XAttributeNamespaceValueEqualityComparer: IEqualityComparer<XAttribute>
    {
        public bool Equals(XAttribute left, XAttribute right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));
            
            return left.Value == right.Value;
        }

        public int GetHashCode(XAttribute obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}