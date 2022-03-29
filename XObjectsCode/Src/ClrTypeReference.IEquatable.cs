using System;

namespace Xml.Schema.Linq.CodeGen
{
    internal partial class ClrTypeReference : IEquatable<ClrTypeReference>
    {
        public bool Equals(ClrTypeReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            //return typeName == other.typeName && typeNs == other.typeNs && clrName == other.clrName &&
            //       clrFullTypeName == other.clrFullTypeName && typeCodeString == other.typeCodeString;
            return Name == other.Name && Namespace == other.Namespace;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ClrTypeReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                //var hashCode = (typeName != null ? typeName.GetHashCode() : 0);
                //hashCode = (hashCode * 397) ^ (typeNs != null ? typeNs.GetHashCode() : 0);
                //hashCode = (hashCode * 397) ^ (clrName != null ? clrName.GetHashCode() : 0);
                //hashCode = (hashCode * 397) ^ (clrFullTypeName != null ? clrFullTypeName.GetHashCode() : 0);
                //hashCode = (hashCode * 397) ^ (typeCodeString != null ? typeCodeString.GetHashCode() : 0);

                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Namespace != null ? Namespace.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ClrTypeReference left, ClrTypeReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ClrTypeReference left, ClrTypeReference right)
        {
            return !Equals(left, right);
        }
    }
}