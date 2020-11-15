using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Xml.Schema.Linq.CodeGen;

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
        /// Determines if an enum equivalent <see cref="CodeTypeDeclaration"/> already exists in the current sequence.
        /// <para>Checks the <see cref="CodeTypeDeclaration.Members"/> as well.</para>
        /// </summary>
        /// <param name="possibleEnums"></param>
        /// <param name="enumDeclaration"></param>
        /// <returns></returns>
        public static bool EquivalentEnumDeclarationExists(this IEnumerable<CodeTypeDeclaration> possibleEnums, CodeTypeDeclaration enumDeclaration)
        {
            if (enumDeclaration == null) throw new ArgumentNullException(nameof(enumDeclaration));
            if (!enumDeclaration.IsEnum) return false;

            var existingEnumExists = (from dec in possibleEnums
                where dec.IsEquivalentEnumDeclaration(enumDeclaration)
                select dec);

            return existingEnumExists.Any();
        }

        /// <summary>
        /// Determines if an equal enum <see cref="CodeTypeDeclaration"/> already exists in the current sequence.
        /// <para>Checks the <see cref="CodeTypeDeclaration.Members"/> as well.</para>
        /// </summary>
        /// <param name="possibleEnums"></param>
        /// <param name="enumDeclaration"></param>
        /// <returns></returns>
        public static bool EqualEnumDeclarationExists(this IEnumerable<CodeTypeDeclaration> possibleEnums, CodeTypeDeclaration enumDeclaration)
        {
            if (enumDeclaration == null) throw new ArgumentNullException(nameof(enumDeclaration));
            if (!enumDeclaration.IsEnum) return false;

            var existingEnumExists = (from dec in possibleEnums
                where dec.IsEqualEnumDeclaration(enumDeclaration)
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

        /// <summary>
        /// Determines if the current <see cref="CodeMemberField"/> and another are the same in terms of their
        /// name and type.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool IsEquivalent(this CodeMemberField x, CodeMemberField y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            
            return x.Name.Equals(y.Name) && x.Attributes == y.Attributes &&
                   (x.Type?.BaseType?.Equals(y.Type?.BaseType) ?? false);
        }

        /// <summary>
        /// Determines if two <see cref="CodeTypeDeclaration"/> (where the <see cref="CodeTypeDeclaration.IsEnum"/> is true)
        /// are the same in terms of their name and <see cref="CodeTypeDeclaration.Members"/> (whereby each <see cref="CodeMemberField"/>
        /// is compared for their name and value).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool IsEquivalentEnumDeclaration(this CodeTypeDeclaration x, CodeTypeDeclaration y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (!x.IsEnum || !y.IsEnum) return false;

            return x.Name.Equals(y.Name) &&
                   x.IsEnum == y.IsEnum &&
                   x.Members.Count == y.Members.Count &&
                   x.Members.OfType<CodeMemberField>()
                       .SequenceEqual(y.Members.OfType<CodeMemberField>(),
                           CodeMemberFieldEqualityComparer.Default);
        }

        /// <summary>
        /// Determines if two <see cref="CodeTypeDeclaration"/> (where the <see cref="CodeTypeDeclaration.IsEnum"/> is true)
        /// are the same in terms of their name and <see cref="CodeTypeDeclaration.Members"/> (whereby each <see cref="CodeMemberField"/>
        /// is compared for their name and value) and also their namespace and type declaration as stored in the 
        /// <see cref="CodeObject.UserData"/> property.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool IsEqualEnumDeclaration(this CodeTypeDeclaration x, CodeTypeDeclaration y)
        {
            var isEquivalentEnumDeclaration = x.IsEquivalentEnumDeclaration(y);
            bool isEqualEnumDeclaration = false;

            var xUserData = (from de in x.UserData.Cast<DictionaryEntry>()
                            select de.ToKeyValuePair<string, ClrTypeReference>()).ToList();
            var yUserData = (from de in y.UserData.Cast<DictionaryEntry>()
                            select de.ToKeyValuePair<string, ClrTypeReference>()).ToList();

            if (!xUserData.Any() && !yUserData.Any()) {
                return isEquivalentEnumDeclaration;
            }

            var xData = xUserData.FirstOrDefault();
            var yData = yUserData.FirstOrDefault();

            if (xData.Value != default && yData.Value != default) {
                isEqualEnumDeclaration = xData.Value == yData.Value;
            }

            return isEqualEnumDeclaration && isEquivalentEnumDeclaration;
        }
    }
}