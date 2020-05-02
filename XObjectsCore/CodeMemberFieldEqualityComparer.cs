using System.CodeDom;
using System.Collections.Generic;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq
{
    public class CodeMemberFieldEqualityComparer: IEqualityComparer<CodeMemberField>
    {
        public bool Equals(CodeMemberField x, CodeMemberField y) => x.IsEquivalent(y);

        public int GetHashCode(CodeMemberField obj) => obj.GetHashCode();

        public static readonly IEqualityComparer<CodeMemberField> Default = new CodeMemberFieldEqualityComparer();
    }
}