using System.CodeDom;
using System.Collections.Generic;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq
{
    public class CodeTypeMemberEqualityComparer: IEqualityComparer<CodeTypeMember>
    {
        public bool Equals(CodeTypeMember x, CodeTypeMember y)
        {
            if (x == null || y == null) return false;

            return x.IsEquivalent(y);
        }

        public int GetHashCode(CodeTypeMember obj)
        {
            return obj.GetHashCode();
        }

        public static IEqualityComparer<CodeTypeMember> Default = new CodeTypeMemberEqualityComparer();
    }
}