using System;
using System.CodeDom;
using System.Collections.Generic;

namespace Xml.Schema.Linq
{
    public class CodeAttributeArgumentEqualityComparer: IEqualityComparer<CodeAttributeArgument>
    {
        public bool Equals(CodeAttributeArgument x, CodeAttributeArgument y)
        {
            if (x == null || y == null) return false;
            return x.Name == y.Name && x.Value == y.Value;
        }

        public int GetHashCode(CodeAttributeArgument obj) => obj.GetHashCode();

        public static readonly IEqualityComparer<CodeAttributeArgument> Default = new CodeAttributeArgumentEqualityComparer();
    }
}