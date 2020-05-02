using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace Xml.Schema.Linq
{
    public class CodeAttributeDeclarationEqualityComparer: IEqualityComparer<CodeAttributeDeclaration>
    {
        public bool Equals(CodeAttributeDeclaration x, CodeAttributeDeclaration y)
        {
            if (x == null || y == null) return false;

            bool argCollectionEqual = x.Arguments.Count == y.Arguments.Count;

            if (argCollectionEqual) {
                argCollectionEqual = x.Arguments.Cast<CodeAttributeArgument>()
                    .SequenceEqual(y.Arguments.Cast<CodeAttributeArgument>(), CodeAttributeArgumentEqualityComparer.Default);
            }

            return x.Name == y.Name && x.AttributeType == y.AttributeType && argCollectionEqual;
        }

        public int GetHashCode(CodeAttributeDeclaration obj) => obj.GetHashCode();

        public static readonly IEqualityComparer<CodeAttributeDeclaration> Default = new CodeAttributeDeclarationEqualityComparer();
    }
}