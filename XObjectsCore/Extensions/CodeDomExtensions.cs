using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSharp;

namespace Xml.Schema.Linq.Extensions
{
    public static class CodeDomExtensions
    {
        /// <summary>
        /// Generates code string from the current <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="ccu"></param>
        /// <returns></returns>
        public static StringWriter ToStringWriter(this CodeCompileUnit ccu)
        {
            var stringWriter = new StringWriter();

            var provider = new CSharpCodeProvider();
            var codeGeneratorOptions = new CodeGeneratorOptions();
            provider.GenerateCodeFromCompileUnit(ccu, stringWriter, codeGeneratorOptions);

            return stringWriter;
        }

        /// <summary>
        /// Determines if an enum <see cref="CodeTypeDeclaration"/> already exists in the current sequence.
        /// <para>Checks the <see cref="CodeTypeDeclaration.Members"/> as well.</para>
        /// </summary>
        /// <param name="possibleEnums"></param>
        /// <param name="enumDeclaration"></param>
        /// <returns></returns>
        public static bool EnumDeclarationExists(this IEnumerable<CodeTypeDeclaration> possibleEnums, CodeTypeDeclaration enumDeclaration)
        {
            if (enumDeclaration == null) throw new ArgumentNullException(nameof(enumDeclaration));
            if (!enumDeclaration.IsEnum) return false;

            var existingEnumExists = (from dec in possibleEnums
                where dec.IsEquivalentEnumDeclaration(enumDeclaration)
                select dec);

            return existingEnumExists.Any();
        }

        /// <summary>
        /// Gathers all enum <see cref="CodeTypeDeclaration"/> from types declared in the current namespace
        /// (but not enums directly in the namespace scope).
        /// </summary>
        /// <param name="namespace"></param>
        /// <returns></returns>
        public static List<CodeTypeDeclaration> DescendentTypeScopedEnumDeclarations(this CodeNamespace @namespace)
        {
            var codeTypeDeclarations = @namespace.Types.OfType<CodeTypeDeclaration>();
            var typesInNamespaceThatHaveEnums = codeTypeDeclarations.Where(t => t.Members.OfType<CodeTypeDeclaration>().Any(e => e.IsEnum));
            var enumsInOtherTypesInNamespace = typesInNamespaceThatHaveEnums.SelectMany(t => t.Members.OfType<CodeTypeDeclaration>().Where(e => e.IsEnum));

            return enumsInOtherTypesInNamespace.ToList();
        }
        
        /// <summary>
        /// Searches the current namespace for the same equivalent <see cref="CodeTypeDeclaration"/> of the given <paramref name="enum"/>
        /// and returns the type containing it.
        /// <para>Uses the <see cref="IsEquivalent(System.CodeDom.CodeTypeMember,System.CodeDom.CodeTypeMember)"/></para>
        /// </summary>
        /// <param name="namespace"></param>
        /// <param name="enum"></param>
        /// <returns></returns>
        public static CodeTypeDeclaration TypeWithEnumDeclaration(this CodeNamespace @namespace, CodeTypeDeclaration @enum) 
        {
            var codeTypeDeclarations = @namespace.Types.OfType<CodeTypeDeclaration>();
            var typesInNamespaceThatHaveEnums = codeTypeDeclarations.Where(t => t.Members.OfType<CodeTypeDeclaration>().Any(e => e.IsEnum));

            var typeWithTheSameEnum = from t in typesInNamespaceThatHaveEnums
                                      where t.Members.OfType<CodeTypeDeclaration>().Any(c => c.IsEquivalentEnumDeclaration(@enum)) 
                                      select t;

            return typeWithTheSameEnum.First();
        }

        /// <summary>
        /// Gathers all enum <see cref="CodeTypeDeclaration"/> from the current namespace.
        /// </summary>
        /// <param name="namespace"></param>
        /// <returns></returns>
        public static List<CodeTypeDeclaration> NamespaceScopedEnumDeclarations(this CodeNamespace @namespace)
        {
            var codeTypeDeclarations = @namespace.Types.OfType<CodeTypeDeclaration>().ToList();
            var namespaceScopedEnums = codeTypeDeclarations.Where(c => c.IsEnum);

            return namespaceScopedEnums.ToList();
        }

        public static bool IsEquivalent(this CodeObject x, CodeObject y)
        {
            if (x == null || y == null) return false;
            if (x.UserData == null || y.UserData == null) return false;

            var keys = x.UserData.Keys.Cast<string>().ToList();

            return keys.SequenceEqual(y.UserData.Keys.Cast<string>()) || x.Equals(y);
        }

        public static bool IsEquivalent(this CodeMemberField x, CodeMemberField y)
        {
            if (x == null || y == null) return false;

            return x.Name.Equals(y.Name) && (x.Type?.BaseType?.Equals(y.Type?.BaseType) ?? false);
        }

        public static bool IsEquivalent(this CodeLinePragma x, CodeLinePragma y)
        {
            if (x == null || y == null) return false;

            return x.FileName.Equals(y.FileName) && x.LineNumber == y.LineNumber;
        }

        public static bool IsEquivalent(this CodeTypeReference x, CodeTypeReference y)
        {
            if (x == null || y == null) return false;

            return x.Equals(y);
        }

        public static bool IsEquivalent(CodeAttributeDeclaration x, CodeAttributeDeclaration y)
        {
            if (x == null || y == null) return false;

            return x.Name == y.Name && x.AttributeType.IsEquivalent(y.AttributeType);
        }

        [Obsolete]
        public static bool IsEquivalent(this CodeTypeMember x, CodeTypeMember y)
        {
            if (x == null || y == null) return false;
            var sameAttributes = x.Attributes == y.Attributes;
            var sameName = x.Name.Equals(y.Name);
            var sameLinePragma = x.LinePragma.IsEquivalent(y.LinePragma);
            var sameCustomAttrs = x.CustomAttributes.Cast<CodeAttributeDeclaration>()
                .SequenceEqual(y.CustomAttributes.Cast<CodeAttributeDeclaration>(), CodeAttributeDeclarationEqualityComparer.Default);

            return sameAttributes && sameName && sameLinePragma && sameCustomAttrs;
        }

        public static bool IsEquivalentEnumDeclaration(this CodeTypeDeclaration x, CodeTypeDeclaration y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (!x.IsEnum || !y.IsEnum) return false;

            return x.Name.Equals(y.Name) && x.IsEnum == y.IsEnum && x.Members.Count == y.Members.Count &&
                   x.Members.OfType<CodeMemberField>()
                       .SequenceEqual(y.Members.OfType<CodeMemberField>(), CodeMemberFieldEqualityComparer.Default);
        }
    }
}