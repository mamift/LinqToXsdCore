using System;

namespace Xml.Schema.Linq.CodeGen
{
    internal abstract partial class ClrTypeInfo : IEquatable<ClrTypeInfo>
    {
        public bool Equals(ClrTypeInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return clrtypeName == other.clrtypeName && clrtypeNs == other.clrtypeNs;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ClrTypeInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((clrtypeName != null ? clrtypeName.GetHashCode() : 0) * 397) ^
                       (clrtypeNs != null ? clrtypeNs.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ClrTypeInfo left, ClrTypeInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ClrTypeInfo left, ClrTypeInfo right)
        {
            return !Equals(left, right);
        }
    }
}