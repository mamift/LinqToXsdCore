using System.CodeDom;
using System.Reflection;
using Xml.Schema.Linq;

namespace XObjects
{
    public static class GeneralExtensionMethods
    {
        /// <summary>
        /// Converts <see cref="GeneratedTypesVisibility"/> to an appropriate <see cref="TypeAttributes"/> instance.
        /// </summary>
        /// <param name="gtv"></param>
        /// <returns></returns>
        public static TypeAttributes ToTypeAttribute(this GeneratedTypesVisibility gtv) => 
            gtv.HasFlag(GeneratedTypesVisibility.Internal) || gtv == GeneratedTypesVisibility.Internal ? TypeAttributes.NestedAssembly : TypeAttributes.Public;

        /// <summary>
        /// Converts <see cref="GeneratedTypesVisibility"/> to an appropriate <see cref="MemberAttributes"/> instance.
        /// </summary>
        /// <param name="gtv"></param>
        /// <returns></returns>
        public static MemberAttributes ToMemberAttribute(this GeneratedTypesVisibility gtv) =>
            gtv.HasFlag(GeneratedTypesVisibility.Internal) || gtv == GeneratedTypesVisibility.Internal ? MemberAttributes.Assembly : MemberAttributes.Public;

        /// <summary>
        /// Converts <see cref="GeneratedTypesVisibility"/> to a keyword for use in code-generation.
        /// </summary>
        /// <param name="gtv"></param>
        /// <returns></returns>
        public static string ToKeyword(this GeneratedTypesVisibility gtv) =>
            gtv.HasFlag(GeneratedTypesVisibility.Internal) || gtv == GeneratedTypesVisibility.Internal ? "internal" : "public";

        public static GeneratedTypesVisibility ToGeneratedTypesVisibility(this MemberAttributes ma)
        {
            if (ma.HasFlag(MemberAttributes.Family) || 
                ma.HasFlag(MemberAttributes.FamilyAndAssembly) ||
                ma.HasFlag(MemberAttributes.FamilyOrAssembly) ||
                ma.HasFlag(MemberAttributes.Private))
                return GeneratedTypesVisibility.Internal;

            return GeneratedTypesVisibility.Public;
        }
    }
}
