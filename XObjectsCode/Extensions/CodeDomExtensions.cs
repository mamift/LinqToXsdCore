#nullable enable
using Microsoft.CSharp;

using OneOf;

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq.Extensions
{
    public static class CodeDomExtensions
    {
        public static bool HasXNameFieldForProperty(this CodeTypeDeclaration typeDeclaration, ClrBasePropertyInfo property)
        {
            var hasXNameFieldForProperty = false;

#if DEBUG
            var str = typeDeclaration.ToCodeString();
            Debug.Assert(str.IsNotEmpty());
#endif

            foreach (var member in typeDeclaration.Members) {
                if (member is CodeMemberField field) {
                    var propertyXnameFieldName = property.PropertyName + "XName";
                    hasXNameFieldForProperty = string.Equals(field.Name, propertyXnameFieldName, StringComparison.CurrentCulture);
                    if (hasXNameFieldForProperty) break;
                }
            }

            return hasXNameFieldForProperty;
        }

        /// <summary>
        /// Compares that the current <see cref="ClrTypeReference"/> is equivalent to the given CodeDOM <see cref="CodeTypeReference"/>.
        /// <para>This methods assumes that the <paramref name="codeTypeReference"/> has the namespace in the <see cref="CodeTypeReference.BaseType"/>.</para>
        /// </summary>
        /// <param name="clrTypeReference"></param>
        /// <param name="codeTypeReference"></param>
        /// <returns></returns>
        internal static bool IsEquivalentTypeReference(this ClrTypeReference clrTypeReference, CodeTypeReference codeTypeReference)
        {
            if (codeTypeReference.BaseType.Contains('.')) {
                string[] baseTypeSplit = codeTypeReference.BaseType.Split('.');
                var typeNameFromNs = baseTypeSplit.LastOrDefault();
                var codeTypeRefNs = string.Join(".", baseTypeSplit.Take(baseTypeSplit.Length - 1));

                bool typeNameSame = string.Equals(typeNameFromNs, clrTypeReference.ClrName);
                bool nsNameSame = string.Equals(codeTypeRefNs, clrTypeReference.Namespace);
                return typeNameSame && nsNameSame;
            }

            return string.Equals(clrTypeReference.ClrFullTypeName, codeTypeReference.BaseType);
        }

        private static string ToCodeStringInternal(
            this OneOf<CodeCompileUnit, CodeExpression, CodeTypeMember, CodeNamespace, CodeStatement, CodeTypeDeclaration> codeDomObjects,
            CodeGeneratorOptions options = null)
        {
            var stringWriter = ToStringWriterInternal(codeDomObjects, options);
            var stringBuilder = stringWriter.GetStringBuilder();
            return stringBuilder.ToString();
        }
        
        private static StringWriter ToStringWriterInternal(
            this OneOf<CodeCompileUnit, CodeExpression, CodeTypeMember, CodeNamespace, CodeStatement, CodeTypeDeclaration> codeDomObjects,
            CodeGeneratorOptions options = null)
        {
            var stringWriter = new StringWriter();

            var provider = new CSharpCodeProvider();
            var theOptions = options ?? new CodeGeneratorOptions {
                VerbatimOrder = true
            };

            codeDomObjects.Match(
                codeCompileUnit => {
                    provider.GenerateCodeFromCompileUnit(codeCompileUnit, stringWriter, theOptions);
                    return stringWriter;
                },
                codeExpression => {
                    provider.GenerateCodeFromExpression(codeExpression, stringWriter, theOptions);
                    return stringWriter;
                },
                codeTypeMember => {
                    provider.GenerateCodeFromMember(codeTypeMember, stringWriter, theOptions);
                    return stringWriter;
                },
                codeNamespace => {
                    provider.GenerateCodeFromNamespace(codeNamespace, stringWriter, theOptions);
                    return stringWriter;
                },
                codeStatement => {
                    provider.GenerateCodeFromStatement(codeStatement, stringWriter, theOptions);
                    return stringWriter;
                },
                codeTypeDeclaration => {
                    provider.GenerateCodeFromType(codeTypeDeclaration, stringWriter, theOptions);
                    return stringWriter;
                });

            return stringWriter;
        }

        public static StringWriter ToStringWriter(this CodeTypeDeclaration codeTypeDeclaration) => ToStringWriterInternal(codeTypeDeclaration);
        public static string ToCodeString(this CodeTypeDeclaration codeTypeDeclaration) => ToCodeStringInternal(codeTypeDeclaration);

        /// <summary>
        /// Generates code string from the current <see cref="CodeExpression"/>.
        /// </summary>
        /// <param name="statement"></param>
        /// <returns></returns>
        public static StringWriter ToStringWriter(this CodeStatement statement) => ToStringWriterInternal(statement);
        public static string ToCodeString(this CodeStatement statement) => ToCodeStringInternal(statement);

        /// <summary>
        /// Generates code string from the current <see cref="CodeNamespace"/>.
        /// </summary>
        /// <param name="codeNamespace"></param>
        /// <returns></returns>
        public static StringWriter ToStringWriter(this CodeNamespace codeNamespace) => ToStringWriterInternal(codeNamespace);
        public static string ToCodeString(this CodeNamespace codeNamespace) => ToCodeStringInternal(codeNamespace);

        public static StringWriter ToStringWriter(this CodeTypeMember codeTypeMember) => ToStringWriterInternal(codeTypeMember);
        public static string ToCodeString(this CodeTypeMember codeTypeMember) => ToCodeStringInternal(codeTypeMember);

        /// <summary>
        /// Generates code string from the current <see cref="CodeExpression"/>.
        /// </summary>
        /// <param name="codeExpression"></param>
        /// <returns></returns>
        public static StringWriter ToStringWriter(this CodeExpression codeExpression) => ToStringWriterInternal(codeExpression);
        public static string ToCodeString(this CodeExpression codeExpression) => ToCodeStringInternal(codeExpression);

        /// <summary>
        /// Generates code string from the current <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="ccu"></param>
        /// <returns></returns>
        public static StringWriter ToStringWriter(this CodeCompileUnit ccu) => ToStringWriterInternal(ccu);
        public static string ToCodeString(this CodeCompileUnit ccu) => ToCodeStringInternal(ccu);

        public static string ToCodeString(this CompilerError ce) => ce.ToString();

        public static string ToCodeString(this CodeAttributeArgument ce) => ce.ToCodeStringInternal();
        private static string ToCodeStringInternal(this CodeAttributeArgument caa, CodeGeneratorOptions? options = null)
        {
            return $"{caa.Name} = {caa.Value.ToCodeString()}";
        }

        public static string ToCodeString(this CodeAttributeDeclaration ce) => ce.ToCodeStringInternal();
        private static string ToCodeStringInternal(this CodeAttributeDeclaration caa, CodeGeneratorOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public static string ToCodeString(this CodeCatchClause ce) => ce.ToCodeStringInternal();
        private static string ToCodeStringInternal(this CodeCatchClause caa, CodeGeneratorOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public static string ToCodeString(this CodeDirective ce) => ce.ToCodeStringInternal();
        private static string ToCodeStringInternal(this CodeDirective caa, CodeGeneratorOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public static string ToCodeString(this CodeTypeParameter ce) => ce.ToCodeStringInternal();
        private static string ToCodeStringInternal(this CodeTypeParameter caa, CodeGeneratorOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public static string ToCodeString(this CodeTypeReference ce) => ce.ToCodeStringInternal();
        private static string ToCodeStringInternal(this CodeTypeReference caa, CodeGeneratorOptions? options = null)
        {
            throw new NotImplementedException();
        }

        internal static string ToCodeStringFromCollections(this CollectionBase collectionBase)
        {
            string codeString = collectionBase switch {
                CompilerErrorCollection compilererrorcollection => compilererrorcollection.ToCodeString(),
                CodeAttributeArgumentCollection codeattributeargumentcollection => codeattributeargumentcollection.ToCodeString(),
                CodeAttributeDeclarationCollection codeattributedeclarationcollection => codeattributedeclarationcollection.ToCodeString(),
                CodeCatchClauseCollection codecatchclausecollection => codecatchclausecollection.ToCodeString(),
                CodeCommentStatementCollection codecommentstatementcollection => codecommentstatementcollection.ToCodeString(),
                CodeDirectiveCollection codedirectivecollection => codedirectivecollection.ToCodeString(),
                CodeExpressionCollection codeexpressioncollection => codeexpressioncollection.ToCodeString(),
                CodeNamespaceCollection codenamespacecollection => codenamespacecollection.ToCodeString(),
                CodeParameterDeclarationExpressionCollection codeparameterdeclarationexpressioncollection => codeparameterdeclarationexpressioncollection.ToCodeString(),
                CodeStatementCollection codestatementcollection => codestatementcollection.ToCodeString(),
                CodeTypeDeclarationCollection codetypedeclarationcollection => codetypedeclarationcollection.ToCodeString(),
                CodeTypeMemberCollection codetypemembercollection => codetypemembercollection.ToCodeString(),
                CodeTypeParameterCollection codetypeparametercollection => codetypeparametercollection.ToCodeString(),
                CodeTypeReferenceCollection codetypereferencecollection => codetypereferencecollection.ToCodeString(),
                _ => throw new NotSupportedException("Only CodeDOM collections supported")
            };

            return codeString;
        }

        public static string ToCodeString(this CompilerErrorCollection compilererrorcollection)
        {
            var str = "";
            foreach (CompilerError element in compilererrorcollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeAttributeArgumentCollection codeattributeargumentcollection)
        {
            var str = "";
            foreach (CodeAttributeArgument element in codeattributeargumentcollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeAttributeDeclarationCollection codeattributedeclarationcollection)
        {
            var str = "";
            foreach (CodeAttributeDeclaration element in codeattributedeclarationcollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeCatchClauseCollection codecatchclausecollection)
        {
            var str = "";
            foreach (CodeCatchClause element in codecatchclausecollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeCommentStatementCollection codecommentstatementcollection)
        {
            var str = "";
            foreach (CodeCommentStatement element in codecommentstatementcollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeDirectiveCollection codedirectivecollection)
        {
            var str = "";
            foreach (CodeDirective element in codedirectivecollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeExpressionCollection codeexpressioncollection)
        {
            var str = "";
            foreach (CodeExpression element in codeexpressioncollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeNamespaceCollection codenamespacecollection)
        {
            var str = "";
            foreach (CodeNamespace element in codenamespacecollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeParameterDeclarationExpressionCollection codeparameterdeclarationexpressioncollection)
        {
            var str = "";
            foreach (CodeParameterDeclarationExpression element in codeparameterdeclarationexpressioncollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeStatementCollection codestatementcollection)
        {
            var str = "";
            foreach (CodeStatement element in codestatementcollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeTypeDeclarationCollection codetypedeclarationcollection)
        {
            var str = "";
            foreach (CodeTypeDeclaration element in codetypedeclarationcollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeTypeMemberCollection codetypemembercollection)
        {
            var str = "";
            foreach (CodeTypeMember element in codetypemembercollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeTypeParameterCollection codetypeparametercollection)
        {
            var str = "";
            foreach (CodeTypeParameter element in codetypeparametercollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }
        public static string ToCodeString(this CodeTypeReferenceCollection codetypereferencecollection)
        {
            var str = "";
            foreach (CodeTypeReference element in codetypereferencecollection) {
                str += element.ToCodeString() + "\n";
            }

            return str;
        }

        /// <summary>
        /// Creates individual <see cref="StringWriter"/>s for each <see cref="CodeTypeDeclaration"/> in each
        /// <see cref="CodeNamespace"/> in the <paramref name="current"/> <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        public static IEnumerable<StringWriter> ToClassStringWriters(this CodeCompileUnit current)
        {
            var provider = new CSharpCodeProvider();
            var codeGeneratorOptions = new CodeGeneratorOptions() {
                VerbatimOrder = true
            };

            foreach (CodeNamespace ns in current.Namespaces.Cast<CodeNamespace>()) {
                var imports = ns.Imports.Cast<CodeNamespaceImport>().ToArray();
                var comments = ns.Comments.Cast<CodeCommentStatement>().ToArray();
                foreach (CodeTypeDeclaration type in ns.Types) {
                    var classStrWriter = new StringWriter();

                    var nsCopy = ns.ShallowClone();
                    nsCopy.Comments.AddRange(comments);
                    nsCopy.Imports.AddRange(imports);
                    nsCopy.Types.Add(type);

                    provider.GenerateCodeFromNamespace(nsCopy, classStrWriter, codeGeneratorOptions);

                    yield return classStrWriter;
                }
            }
        }

        public static CodeNamespace ShallowClone(this CodeNamespace current) => new CodeNamespace(current.Name);

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
        /// <param name="x">The current one.</param>
        /// <param name="y">The other one.</param>
        /// <returns></returns>
        public static bool IsEqualEnumDeclaration(this CodeTypeDeclaration x, CodeTypeDeclaration y)
        {
            var isEquivalentEnumDeclaration = x.IsEquivalentEnumDeclaration(y);
            bool isEqualEnumDeclarationByClrTypeRefs = false;
            bool isEqualEnumDeclarationByClrTypeInfos = false;

            var xUserDataClrTypeRefs = (from de in x.UserData.Cast<DictionaryEntry>()
                where de.Key.ToString() == nameof(ClrTypeReference)
                select de.ToKeyValuePair<string, ClrTypeReference>()).ToList();
            var yUserDataClrTypeRefs = (from de in y.UserData.Cast<DictionaryEntry>()
                where de.Key.ToString() == nameof(ClrTypeReference)
                select de.ToKeyValuePair<string, ClrTypeReference>()).ToList();

            var xUserDataClrTypeInfos = (from de in x.UserData.Cast<DictionaryEntry>()
                where de.Key.ToString() == nameof(ClrTypeInfo)
                select de.ToKeyValuePair<string, ClrTypeInfo>()).ToList();
            var yUserDataClrTypeInfos = (from de in y.UserData.Cast<DictionaryEntry>()
                where de.Key.ToString() == nameof(ClrTypeInfo)
                select de.ToKeyValuePair<string, ClrTypeInfo>()).ToList();

            if (!xUserDataClrTypeRefs.Any() && !yUserDataClrTypeRefs.Any() &&
                !xUserDataClrTypeInfos.Any() && !yUserDataClrTypeInfos.Any()) {
                return isEquivalentEnumDeclaration;
            }

            if (xUserDataClrTypeRefs.Any() || yUserDataClrTypeRefs.Any()) {
                var xDataTypeRef = xUserDataClrTypeRefs.FirstOrDefault();
                var yDataTypeRef = yUserDataClrTypeRefs.FirstOrDefault();

                if (xDataTypeRef.Value != default && yDataTypeRef.Value != default) {
                    isEqualEnumDeclarationByClrTypeRefs = xDataTypeRef.Value == yDataTypeRef.Value;
                }

                return isEqualEnumDeclarationByClrTypeRefs && isEquivalentEnumDeclaration;
            }

            if (xUserDataClrTypeInfos.Any() || yUserDataClrTypeInfos.Any()) {
                var xDataTypeInfo = xUserDataClrTypeInfos.FirstOrDefault();
                var yDataTypeInfo = yUserDataClrTypeInfos.FirstOrDefault();

                if (xDataTypeInfo.Value != default && yDataTypeInfo.Value != default) {
                    isEqualEnumDeclarationByClrTypeInfos = xDataTypeInfo.Value == yDataTypeInfo.Value;
                }

                return isEqualEnumDeclarationByClrTypeInfos && isEquivalentEnumDeclaration;
            }

            return isEquivalentEnumDeclaration;
        }

        /// <summary>
        /// Retrieves all members of the specified <see cref="CodeTypeDeclaration"/> and its base types.
        /// </summary>
        /// <remarks>This method combines the members of the provided <paramref
        /// name="typeDecl"/> with the members of its base types, traversing the inheritance hierarchy. The
        /// base type members are resolved using the provided <paramref name="getCodeNamespace"/> and <paramref
        /// name="getCodeTypeDeclaration"/> functions.</remarks>
        /// <param name="typeDecl">The <see cref="CodeTypeDeclaration"/> whose members and base type members are to be retrieved.</param>
        /// <param name="getCodeNamespace">A function that retrieves a <see cref="CodeNamespace"/> by its name. This is used to resolve namespaces for
        /// base types.</param>
        /// <param name="getCodeTypeDeclaration">A function that retrieves a <see cref="CodeTypeDeclaration"/> from a <see cref="CodeNamespace"/> by its
        /// name. This is used to resolve the type declarations of base types.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CodeTypeMember"/> containing the members of the specified type
        /// and its base types.</returns>
        public static IEnumerable<CodeTypeMember> GetSelfAndBaseMembers(this CodeTypeDeclaration typeDecl,
            Func<string, CodeNamespace> getCodeNamespace,
            Func<string, CodeNamespace, CodeTypeDeclaration> getCodeTypeDeclaration)
        {
            var members = GetSelfAndBaseClasses(typeDecl, getCodeNamespace, getCodeTypeDeclaration)
                .SelectMany(typeDecl => typeDecl.Members.Cast<CodeTypeMember>())
                .ToArray();
            return members;
        }

        /// <summary>
        /// Retrieves the specified type and all of its base classes in a hierarchical order, starting from the type
        /// itself.
        /// </summary>
        /// <remarks>This method uses the provided functions to resolve the base class of each type in the
        /// hierarchy.  If a type does not have a base class, the enumeration ends.</remarks>
        /// <param name="typeDecl">The <see cref="CodeTypeDeclaration"/> representing the type for which to retrieve the hierarchy.</param>
        /// <param name="getCodeNamespace">A function that takes a namespace name as input and returns the corresponding <see cref="CodeNamespace"/>.</param>
        /// <param name="getCodeTypeDeclaration">A function that takes a type name and a <see cref="CodeNamespace"/> as input and returns the corresponding
        /// <see cref="CodeTypeDeclaration"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CodeTypeDeclaration"/> objects, starting with the specified
        /// type and followed by its base classes in order of inheritance.</returns>
        public static IEnumerable<CodeTypeDeclaration> GetSelfAndBaseClasses(this CodeTypeDeclaration typeDecl,
            Func<string, CodeNamespace> getCodeNamespace,
            Func<string, CodeNamespace, CodeTypeDeclaration> getCodeTypeDeclaration)
        {
            yield return typeDecl;
            var baseType = typeDecl.GetBaseClass(getCodeNamespace, getCodeTypeDeclaration);
            while (baseType != null)
            {
                yield return baseType;
                baseType = baseType.GetBaseClass(getCodeNamespace, getCodeTypeDeclaration);
            }
        }

        /// <summary>
        /// Retrieves the base class of the specified <see cref="CodeTypeDeclaration"/> if it exists.
        /// </summary>
        /// <remarks>This method resolves the base class by examining the <see
        /// cref="CodeTypeDeclaration.BaseTypes"/> collection  and using the provided functions to locate the
        /// corresponding type declaration. Only base types that are classes  are considered.</remarks>
        /// <param name="codeTypeDeclaration">The <see cref="CodeTypeDeclaration"/> for which to find the base class.</param>
        /// <param name="getCodeNamespace">A function that takes a namespace name as input and returns the corresponding <see cref="CodeNamespace"/>.</param>
        /// <param name="getCodeTypeDeclaration">A function that takes a type name and a <see cref="CodeNamespace"/> as input and returns the corresponding
        /// <see cref="CodeTypeDeclaration"/>.</param>
        /// <returns>The <see cref="CodeTypeDeclaration"/> representing the base class of the specified <paramref
        /// name="codeTypeDeclaration"/>,  or <see langword="null"/> if no base class is found or the base type cannot
        /// be resolved.</returns>
        public static CodeTypeDeclaration GetBaseClass(this CodeTypeDeclaration codeTypeDeclaration,
            Func<string, CodeNamespace> getCodeNamespace,
            Func<string, CodeNamespace, CodeTypeDeclaration> getCodeTypeDeclaration)
        {
            var baseType = codeTypeDeclaration.BaseTypes
                .OfType<CodeTypeReference>()
                .Select(ToTypeDeclaration)
                .Where(typeDecl => typeDecl != null && typeDecl.IsClass)
                .FirstOrDefault();

            return baseType;

            CodeTypeDeclaration ToTypeDeclaration(CodeTypeReference typeRef)
            {
                var typeFullName = typeRef.BaseType;
                var lastDotPos = typeFullName.LastIndexOf('.');
                if (lastDotPos > 0)
                {
                    var globalSepPos  = typeFullName.LastIndexOf(':') + 1;
                    var @namespace    = typeFullName.Substring(globalSepPos, lastDotPos - globalSepPos);
                    var typeName      = typeFullName.Substring(lastDotPos + 1);
                    var codeNamespace = getCodeNamespace(@namespace);
                    var typeDecl      = getCodeTypeDeclaration(typeName, codeNamespace);
                    return typeDecl;
                }
                return null;
            }
        }

        /// <summary>
        /// Adds a new <see cref="CodeTypeDeclaration"/> to the current namespace and also retain a reference to the parent <see cref="CodeNamespace"/> in the given
        /// <paramref name="type"/>. This is set in the <see cref="CodeObject.UserData"/> dictionary; can be set with <see cref="SetParent{TCodeObject}"/>
        /// and retrieved with <see cref="GetParent{TCodeObject}"/>.
        /// </summary>
        /// <param name="codeNs"></param>
        /// <param name="type"></param>
        public static void AddTypeWithParentNamespace(this CodeNamespace codeNs, CodeTypeDeclaration type)
        {
            codeNs.Types.Add(type);
            type.SetParent(codeNs);
        }

        public static List<CodeTypeDeclaration> FlattenAllNestedTypesRecursively(this CodeTypeDeclaration type, List<CodeTypeDeclaration>? typesList = null)
        {
            typesList ??= new List<CodeTypeDeclaration>();
            List<CodeTypeDeclaration> nestedTypes = type.Members.OfType<CodeTypeDeclaration>().ToList();
            if (nestedTypes.Count > 0) {
                typesList.AddRange(nestedTypes);
                foreach (var nestedType in nestedTypes) {
                    nestedType.FlattenAllNestedTypesRecursively(typesList);
                }
            }

            return typesList;
        }

        /// <summary>
        /// Searches for a nested type with the specified name within the current <see cref="CodeTypeDeclaration"/> 
        /// and all its nested types recursively. Uses the LINQ method <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/> to
        /// return the possible match.
        /// </summary>
        /// <param name="type">The current type.</param>
        /// <param name="predicate">A searching predicate</param>
        /// <param name="orderByPredicate">Pass another predicate to order the results if you anticipate <paramref name="predicate"/> will return more than one
        /// result.</param>
        /// <returns>
        /// The <see cref="CodeTypeMember"/> representing the nested type with the specified name, 
        /// or <c>null</c> if no such type is found.
        /// </returns>
        public static CodeTypeMember? SearchForMemberRecursively(this CodeTypeDeclaration type, Func<CodeTypeMember, bool> predicate,
            Func<CodeTypeMember, bool>? orderByPredicate = null)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            CodeTypeMember? thePossibleTypeMember = null;
            IEnumerable<CodeTypeMember> candidates = type.Members.Cast<CodeTypeMember>().Where(predicate);
            if (orderByPredicate is not null) {
                candidates = candidates.OrderByDescending(orderByPredicate);
            }
            thePossibleTypeMember = candidates.FirstOrDefault();

            if (thePossibleTypeMember is null) {
                IEnumerable<CodeTypeDeclaration> nestedTypes = type.Members.OfType<CodeTypeDeclaration>();
                foreach (var nestedType in nestedTypes) {
                    thePossibleTypeMember = nestedType.SearchForMemberRecursively(predicate, orderByPredicate);
                    if (thePossibleTypeMember is not null) break;
                }
            }

            return thePossibleTypeMember;
        }

        /// <summary>
        /// At the namespace level, searches for a <see cref="CodeTypeMember"/> to match the given <paramref name="predicate"/> within all
        /// child <see cref="CodeTypeDeclaration"/>s.
        /// For each type, this invokes <see cref="SearchForMemberRecursively"/>
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="predicate"></param>
        /// <param name="orderByPredicate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static CodeTypeMember? SearchForMemberRecursively(this CodeNamespace ns, Func<CodeTypeMember, bool> predicate,
            Func<CodeTypeMember, bool>? orderByPredicate = null)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            foreach (var type in ns.Types.Cast<CodeTypeDeclaration>()) {
                var possibleTypeMember = type.SearchForMemberRecursively(predicate, orderByPredicate);
                if (possibleTypeMember is not null) return possibleTypeMember;
            }

            return null;
        }

        extension<TCodeObject>(CodeObject co) where TCodeObject: CodeObject
        {
            /// <summary>
            /// This is a strongly-typed convenience method to allow setting a parent for the current <see cref="CodeObject"/>.
            /// For things like <see cref="CodeTypeMember"/>s, you can use this to retain a reference
            /// to the parent <see cref="CodeNamespace"/> for instance.
            /// </summary>
            /// <param name="parent"></param>
            /// <exception cref="ArgumentNullException"></exception>
            public void SetParent(TCodeObject parent) 
            {
                co.UserData["Parent"] = parent ?? throw new ArgumentNullException(nameof(parent));
            }

            /// <summary>
            /// This is a strongly-typed convenience method to allow getting a parent for the current <see cref="CodeObject"/>.
            /// For things like <see cref="CodeTypeMember"/>s, you can use this to retain a reference
            /// to the parent <see cref="CodeNamespace"/> for instance.
            /// </summary>
            /// <exception cref="ArgumentNullException"></exception>
            public TCodeObject? GetParent() 
            {
                return co.UserData["Parent"] as TCodeObject;
            }

            public bool HasParent()
            {
                return co.UserData["Parent"] is TCodeObject;
            }
        }

        extension(CodeTypeDeclaration type)
        {
            /// <summary>
            /// Retain a reference to the parent <see cref="CodeNamespace"/> this type is meant to be enclosed in.
            /// </summary>
            public CodeNamespace? ParentNamespace
            {
                get => type.GetParent<CodeNamespace>();
                set => type.SetParent(value!);
            }

            public IEnumerable<CodeMemberProperty> ChildProperties
            {
                get {
                    return type.Members.OfType<CodeMemberProperty>();
                }
            }

            public IEnumerable<CodeTypeDeclaration> ChildTypes
            {
                get {
                    return type.Members.OfType<CodeTypeDeclaration>();
                }
            }
            
            public IEnumerable<CodeTypeDeclaration> ChildEnumDeclarations
            {
                get {
                    return type.Members.OfType<CodeTypeDeclaration>().Where(e => e.IsEnum);
                }
            }
            
            public IEnumerable<CodeTypeDeclaration> ChildClassDeclarations
            {
                get {
                    return type.Members.OfType<CodeTypeDeclaration>().Where(e => e.IsClass);
                }
            }

            public void ChangeVisibility(TypeAttributes visibility)
            {
                if (!visibility.HasVisibilityMask()) {
                    throw new InvalidOperationException("Requires the use of an enum value that affects type visibility!");
                }

                type.TypeAttributes = (type.TypeAttributes & ~TypeAttributes.VisibilityMask) | visibility;
            }
        }

        public static bool HasVisibilityMask(this TypeAttributes ta)
        {
            TypeAttributes visibility = ta & TypeAttributes.VisibilityMask;
            switch (visibility) {
                case TypeAttributes.NotPublic:
                case TypeAttributes.Public:
                case TypeAttributes.NestedPublic:
                case TypeAttributes.NestedPrivate:
                case TypeAttributes.NestedFamANDAssem:
                case TypeAttributes.NestedAssembly:
                case TypeAttributes.NestedFamily:
                case TypeAttributes.NestedFamORAssem:
                    return true;

                default: return false;
            }
        }
    }
}