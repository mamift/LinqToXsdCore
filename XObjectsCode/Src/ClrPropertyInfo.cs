//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq.CodeGen
{
    public partial class ClrPropertyInfo : ClrBasePropertyInfo
    {
        LinqToXsdSettings settings;

        ClrTypeReference typeRef;
        PropertyFlags propertyFlags;
        SchemaOrigin propertyOrigin;

        CodeExpression xNameExpression;
        string parentTypeFullName;
        string clrTypeName;
        string clrNamespace;
        string fixedDefaultValue;
        string simpleTypeClrTypeName;

#nullable enable
        /// <summary>
        /// The enclosing <see cref="CodeTypeDeclaration"/> that this <see cref="ClrPropertyInfo"/> instance is a part of. 
        /// </summary>
        public CodeTypeDeclaration? ParentTypeDeclaration { get; set; }

        public ClrPropertyInfo(string propertyName, string propertyNs, string schemaName, Occurs occursInSchema, LinqToXsdSettings settings)
        {
            this.settings = settings;
            this.contentType = ContentType.Property;
            this.propertyName = propertyName;
            this.propertyNs = propertyNs;
            this.schemaName = schemaName;
            this.hasSet = true;
            this.returnType = null;
            this.clrTypeName = null;
            this.occursInSchema = occursInSchema;
            if (this.occursInSchema > Occurs.ZeroOrOne)
            {
                this.propertyFlags |= PropertyFlags.IsList;
            }

            if (this.IsOptional)
            {
                this.propertyFlags |= PropertyFlags.CanBeAbsent;
            }

            this.xNameExpression = new CodeFieldReferenceExpression(null, NameGenerator.ChangeClrName(propertyName, NameOptions.MakeXName));
            #if DEBUG
            var xNameExpressionString = xNameExpression.ToCodeString();
            Debug.Assert(xNameExpressionString.IsNotEmpty());
            #endif
        }

        public void Reset()
        {
            this.returnType = null;
            this.clrTypeName = null;
            this.clrNamespace = null;
            this.fixedDefaultValue = null;
            this.propertyFlags = PropertyFlags.None;
        }

        public Type unionDefaultType;

        public string FixedValue
        {
            get
            {
                if ((propertyFlags & PropertyFlags.HasFixedValue) != 0) return fixedDefaultValue;
                else return null;
            }
            set
            {
                if (value != null)
                {
                    propertyFlags |= PropertyFlags.HasFixedValue;
                    fixedDefaultValue = value;
                }
            }
        }

        public string DefaultValue
        {
            get
            {
                if ((propertyFlags & PropertyFlags.HasDefaultValue) != 0) return fixedDefaultValue;
                else return null;
            }
            set
            {
                if (value != null)
                {
                    propertyFlags |= PropertyFlags.HasDefaultValue;
                    fixedDefaultValue = value;
                }
            }
        }

        public bool IsRef
        {
            get { return typeRef.IsTypeRef; }
        }

        public ClrTypeReference TypeReference
        {
            get { return typeRef; }
            set { typeRef = value; }
        }

        public List<XmlSchemaElement> SubstitutionMembers { get; set; }

        public bool IsSubstitutionHead => SubstitutionMembers != null;

        public SchemaOrigin Origin
        {
            get { return propertyOrigin; }
            set { propertyOrigin = value; }
        }

        public override string ClrTypeName
        {
            get { return clrTypeName; }
        }

        public string ClrNamespace
        {
            get { return clrNamespace; }
            set { clrNamespace = value; }
        }

        public override bool IsList
        {
            //This is for repeating elements, not schema list
            get { return (propertyFlags & PropertyFlags.IsList) != 0; }
            set
            {
                if (value)
                {
                    propertyFlags |= PropertyFlags.IsList;
                }
                else
                {
                    propertyFlags &= ~PropertyFlags.IsList;
                }
            }
        }

        public override bool IsNullable
        {
            get
            {
                return
                    // Elements can be absent and must be read as null even when they have a default value
                    // Absent attributes will take their default value when they have one
                    (CanBeAbsent && (propertyOrigin != SchemaOrigin.Attribute || fixedDefaultValue == null))
                    || IsNillable;
            }
        }

        public bool CanBeAbsent
        {
            get { return (propertyFlags & PropertyFlags.CanBeAbsent) != 0; }
            set
            {
                if (value)
                {
                    propertyFlags |= PropertyFlags.CanBeAbsent;
                }
                else
                {
                    propertyFlags &= ~PropertyFlags.CanBeAbsent;
                }
            }
        }

        public bool IsNillable
        {
            get { return (propertyFlags & PropertyFlags.IsNillable) != 0; }
            set
            {
                if (value)
                {
                    propertyFlags |= PropertyFlags.IsNillable;
                }
                else
                {
                    propertyFlags &= ~PropertyFlags.IsNillable;
                }
            }
        }

        public override bool IsSchemaList
        {
            get { return (this.typeRef?.IsSchemaList).GetValueOrDefault(); }
        }

        public override bool IsUnion
        {
            get { return (this.typeRef?.IsUnion).GetValueOrDefault(); }
        }

        public override bool IsEnum
        {
            get { return (this.typeRef?.IsEnum).GetValueOrDefault(); }
        }

        public bool Validation
        {
            get { return (this.typeRef?.Validate).GetValueOrDefault() && !IsRef; }
        }

        public override bool FromBaseType
        {
            get { return (propertyFlags & PropertyFlags.FromBaseType) != 0; }
            set
            {
                if (value)
                {
                    propertyFlags |= PropertyFlags.FromBaseType;
                }
                else
                {
                    propertyFlags &= ~PropertyFlags.FromBaseType;
                }
            }
        }

        public override bool IsDuplicate
        {
            get { return (propertyFlags & PropertyFlags.IsDuplicate) != 0; }
            set
            {
                if (value)
                {
                    propertyFlags |= PropertyFlags.IsDuplicate;
                }
                else
                {
                    propertyFlags &= ~PropertyFlags.IsDuplicate;
                }
            }
        }

        public override bool IsNew
        {
            get { return (propertyFlags & PropertyFlags.IsNew) != 0; }
            set
            {
                if (value)
                {
                    propertyFlags |= PropertyFlags.IsNew;
                }
                else
                {
                    propertyFlags &= ~PropertyFlags.IsNew;
                }
            }
        }

        public override bool VerifyRequired
        {
            get { return (propertyFlags & PropertyFlags.VerifyRequired) != 0; }
            set
            {
                if (value)
                {
                    propertyFlags |= PropertyFlags.VerifyRequired;
                }
                else
                {
                    propertyFlags &= ~PropertyFlags.VerifyRequired;
                }
            }
        }

        public override XCodeTypeReference ReturnType
            => returnType ??= CreateReturnType(IsEnum ? typeRef.ClrFullTypeName : clrTypeName);

        private string? returnTypeStr = null, returnTypeFqn = null;

        // TODO: rename after getting rid of CodeDom ReturnType property above
        public string ReturnTypeStr
        {
            get 
            {
                if (returnTypeStr == null)
                    (returnTypeStr, returnTypeFqn) = CreateReturnTypeStr(IsEnum ? typeRef.ClrFullTypeName : clrTypeName);
                return returnTypeStr;
            }
        }

        public string? ReturnTypeFqn
        {
            get 
            {
                if (returnTypeStr == null)
                    (returnTypeStr, returnTypeFqn) = CreateReturnTypeStr(IsEnum ? typeRef.ClrFullTypeName : clrTypeName);
                return returnTypeFqn;
            }            
        }

        private string? fixedOrDefaultBaseType = null;
        private bool isFixedOrDefaultList;

        public string FixedOrDefaultBaseType
        {
            get {
                if (fixedOrDefaultBaseType == null)
                    (fixedOrDefaultBaseType, isFixedOrDefaultList) = CreateFixedOrDefaultType(ReturnTypeStr);
                return fixedOrDefaultBaseType;
            }
        }

        public bool IsFixedOrDefaultList
        {
            get {
                if (fixedOrDefaultBaseType == null)
                    (fixedOrDefaultBaseType, isFixedOrDefaultList) = CreateFixedOrDefaultType(ReturnTypeStr);
                return isFixedOrDefaultList;
            }
        }

        private string QualifiedType => typeRef.IsLocalType && !typeRef.IsSimpleType
            ? parentTypeFullName + "." + clrTypeName
            : clrTypeName;

        public string NullableType => IsNillable && (settings.NullableReferences || typeRef.IsValueType)
            ? QualifiedType + "?"
            : QualifiedType;

        private (string, string?) CreateReturnTypeStr(string typeName)
        {
            if (IsList || !IsRef && IsSchemaList)
            {
                var listType = (IsEnum, IsNillable) switch
                {
                    (true, true) => typeRef.ClrFullTypeName + "?",
                    (true, false) => typeRef.ClrFullTypeName,
                    _ => NullableType,
                };
                return (hasSet ? $"IList<{listType}>" : $"IEnumerable<{listType}>", null);
            }

            string fullTypeName = typeRef.IsLocalType && !typeRef.IsSimpleType
                ? parentTypeFullName + "." + typeName
                : typeName; // For simple types, return type is always XSD -> CLR mapping

            if (!IsRef && IsNullable && (settings.NullableReferences || typeRef.IsValueType))
                return (fullTypeName + "?", null);

            return (typeName, fullTypeName);
        }

        private static bool RegexExtract(string text, string pattern, out string result)
        {
            var match = Regex.Match(text, pattern);
            result = match.Success ? match.Groups[1].Value : "";
            return match.Success;
        }

        private (string, bool) CreateFixedOrDefaultType(string typeName)
        {
            string baseType;

            if (RegexExtract(typeName, @"\bNullable<([^,>]+)>$", out baseType)) // Nullable<T>
                return (baseType, false);

            if (typeName.EndsWith("?")) // T?
                return (typeName[..^1], false);

            if (RegexExtract(typeName, @"^(.+)\[\]$", out baseType))  // T[]
                return (baseType, true);

            if (RegexExtract(typeName, @"\bI?List<([^,>]+)>$", out baseType))   // IList<T>
                return (baseType, true);

            return (typeName, false);
        }

        private XCodeTypeReference CreateReturnType(string typeName)
        {
            if (IsList || !IsRef && IsSchemaList)
            {
                var listType = (IsEnum, IsNillable) switch
                {
                    (true, true) => typeRef.ClrFullTypeName + "?",
                    (true, false) => typeRef.ClrFullTypeName,
                    _ => NullableType,
                };
                return CreateListReturnType(listType);
            }

            string fullTypeName = typeName;
            if (typeRef.IsLocalType && !typeRef.IsSimpleType)
            {
                //For simple types, return type is always XSD -> CLR mapping
                fullTypeName = parentTypeFullName + "." + typeName;
            }

            if (!IsRef && IsNullable && (settings.NullableReferences || typeRef.IsValueType))
            {
                return new XCodeTypeReference(fullTypeName + "?");
            }

            return new XCodeTypeReference(typeName) { fullTypeName = fullTypeName };
        }

        private XCodeTypeReference CreateListReturnType(string fullTypeName)
        {
            if (hasSet)
            {
                return new XCodeTypeReference("IList",
                    new CodeTypeReference(fullTypeName));
            }
            else
            {
                return new XCodeTypeReference("IEnumerable",
                    new CodeTypeReference(fullTypeName));
            }
        }

        public void UpdateTypeReference(
            string currentTypeScope,
            string currentNamespaceScope,
            Dictionary<XmlSchemaObject, string> nameMappings,
            Action<ClrTypeReference> createNestedEnumType)
        {
            var typeRef = this.TypeReference;
            if (typeRef.IsEnum)
            {
                if (string.IsNullOrEmpty(typeRef.Name))
                {
                    typeRef.Name = $"{this.PropertyName.ToUpperFirstInvariant()}{Constants.LocalEnumSuffix}";
                }
                if (ShouldGenerate && typeRef.IsLocalType && createNestedEnumType != null)
                {
                    // TODO: this is CodeDom manipulation
                    // createNestedEnumType(typeRef);
                }
            }

            this.clrTypeName = typeRef.GetClrFullTypeName(currentNamespaceScope, nameMappings, settings, out string refTypeName);

            if (Validation || IsUnion || IsEnum)
            {
                this.simpleTypeClrTypeName = typeRef.GetSimpleTypeClrTypeDefName(currentNamespaceScope, nameMappings);
            }

            this.parentTypeFullName = typeRef.IsEnum ? typeRef.UpdateClrFullEnumTypeName(this, currentTypeScope, currentNamespaceScope) : currentTypeScope;
        }

        public override CodeMemberProperty? AddToType(CodeTypeDeclaration parentTypeDecl,
            List<ClrAnnotation> annotations, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            // Scriban: done.
            if (parentTypeDecl == null) throw new ArgumentNullException(nameof(parentTypeDecl));
            if (!ShouldGenerate)
            {
                return null;
            }

            ParentTypeDeclaration ??= parentTypeDecl;
            
            // CreateXNameField(parentTypeDecl);
            // CreateFixedDefaultValue(parentTypeDecl);
            CodeMemberProperty clrProperty = CodeDomHelper.CreateProperty(ReturnType, hasSet, visibility.ToMemberAttribute());
            // clrProperty.Name = propertyName;
            // SetPropertyAttributes(clrProperty, visibility.ToMemberAttribute());
            // if (IsNew)
            // {
            //    clrProperty.Attributes |= MemberAttributes.New;
            //}

            // if (IsList)
            // {
                //Create collection type for list
                // CodeTypeReference listType = GetListType();
                // string listName = NameGenerator.ChangeClrName(propertyName, NameOptions.MakeField);
                // AddMemberField(listName, listType, parentTypeDecl);

                //GetStatements
                // AddListGetStatements(clrProperty.GetStatements, listType, listName);
                // if (hasSet)
                // {
                //     AddListSetStatements(clrProperty.SetStatements, listType, listName);
                // }

                // if (settings.NullableReferences)
                // {
                //     clrProperty.CustomAttributes.Add(new CodeAttributeDeclaration("System.Diagnostics.CodeAnalysis.AllowNull"));
                // }
            // }
            // else
            // {
                // AddGetStatements(clrProperty.GetStatements);
                // if (hasSet)
                // {
                //     AddSetStatements(clrProperty.SetStatements);
                // }
            // }

            //ApplyAnnotations(clrProperty, annotations);
            // parentTypeDecl.Members.Add(clrProperty);
            return clrProperty;
        }

        public override void AddToContentModel(CodeObjectCreateExpression contentModelExpression)
        {
            Debug.Assert(contentModelExpression != null && propertyOrigin == SchemaOrigin.Element);
            if (this.IsSubstitutionHead)
            {
                //Need to add member names to content model
                CodeExpression[] substParams = new CodeExpression[SubstitutionMembers.Count];
                int i = 0;
                foreach (XmlSchemaElement elem in SubstitutionMembers)
                {
                    substParams[i++] =
                        CodeDomHelper.XNameGetExpression(elem.QualifiedName.Name, elem.QualifiedName.Namespace);
                }

                contentModelExpression.Parameters.Add(
                    new CodeObjectCreateExpression(Constants.SubstitutedContentModelEntity,
                        substParams));
            }
            else
            {
                contentModelExpression.Parameters.Add(
                    new CodeObjectCreateExpression(Constants.NamedContentModelEntity, xNameExpression));
            }
        }

        public override void AddToConstructor(CodeConstructor functionalConstructor)
        {
            if (IsList)
            {
                functionalConstructor.Parameters.Add(new CodeParameterDeclarationExpression(
                    new CodeTypeReference("IEnumerable", new CodeTypeReference(clrTypeName)),
                    propertyName));
                if (FromBaseType)
                {
                    functionalConstructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression(propertyName));
                }
                else
                {
                    CodeTypeReference listType = GetListType();
                    functionalConstructor.Statements.Add(
                        new CodeAssignStatement(
                            CodeDomHelper.CreateFieldReference("this",
                                NameGenerator.ChangeClrName(propertyName, NameOptions.MakeField)),
                            new CodeMethodInvokeExpression(
                                new CodeTypeReferenceExpression(listType),
                                IsNillable ? Constants.InitializeNillable : Constants.Initialize,
                                GetListParameters(true /*set*/, true /*constructor*/))
                        ));
                }
            }
            else
            {
                functionalConstructor.Parameters.Add(new CodeParameterDeclarationExpression(ReturnType, propertyName));
                if (FromBaseType)
                {
                    functionalConstructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression(propertyName));
                }
                else
                {
                    functionalConstructor.Statements.Add(
                        new CodeAssignStatement(
                            CodeDomHelper.CreateFieldReference("this", propertyName),
                            new CodeVariableReferenceExpression(propertyName)
                        ));
                }
            }
        }

        private void AddGetStatements(CodeStatementCollection getStatements)
        {
            // TODO:
            if (IsSubstitutionHead)
            {
                AddSubstGetStatements(getStatements);
                return;
            }
        }

        private void CheckOccurrence(CodeStatementCollection getStatements)
        {
            // Scriban: done
            // Debug.Assert(!this.IsList);
            // CodeStatement returnStatement = null;
            // if (CanBeAbsent)
            // {
            //     // Absent attributes return their default value (if any).
            //     // Note that absent elements return null, only empty elements return their default value (per xsd specs).
            //     if (DefaultValue != null && propertyOrigin == SchemaOrigin.Attribute)
            //     {
            //         returnStatement = new CodeMethodReturnStatement(
            //             new CodeFieldReferenceExpression(
            //                 null,
            //                 NameGenerator.ChangeClrName(propertyName, NameOptions.MakeDefaultValueField)
            //             )
            //         );
            //     }
            //     else
            //     {
            //         // For value types, this is needed to return T?, since ParseValue return T.
            //         // It's not mandatory for ref types but it's more consistent and performant to do it always.
            //         returnStatement = new CodeMethodReturnStatement(new CodePrimitiveExpression(null));
            //     }
            // }
            // else if (VerifyRequired)
            // {
            //     Debug.Assert(this.occursInSchema == Occurs.One);
            //     string origin = this.propertyOrigin == SchemaOrigin.Element ? "Element" :
            //         this.propertyOrigin == SchemaOrigin.Attribute ? "Attribute" : null;
            //     returnStatement = new CodeThrowExceptionStatement(new CodeObjectCreateExpression(
            //         Constants.LinqToXsdException, new CodePrimitiveExpression("Missing required " + origin)));
            // }

            // if (returnStatement != null)
            // {
            //     getStatements.Add(
            //         new CodeConditionStatement(
            //             new CodeBinaryOperatorExpression(
            //                 new CodeVariableReferenceExpression("x"),
            //                 CodeBinaryOperatorType.IdentityEquality,
            //                 new CodePrimitiveExpression(null)),
            //             returnStatement));
            // }
        }

        private void AddSubstGetStatements(CodeStatementCollection getStatements)
        {
            Debug.Assert(propertyOrigin == SchemaOrigin.Element);
            CodeExpression[] substParams = new CodeExpression[SubstitutionMembers.Count + 2];
            substParams[0] = CodeDomHelper.This();
            substParams[1] = CodeDomHelper.SingletonTypeManager();
            int i = 2;
            foreach (XmlSchemaElement elem in SubstitutionMembers)
            {
                substParams[i++] =
                    CodeDomHelper.XNameGetExpression(elem.QualifiedName.Name, elem.QualifiedName.Namespace);
            }

            getStatements.Add(
                new CodeVariableDeclarationStatement(
                    Constants.XTypedElement,
                    "x",
                    CodeDomHelper.CreateMethodCall(
                        new CodeTypeReferenceExpression(Constants.XTypedServices),
                        Constants.ToSubstitutedXTypedElement,
                        substParams)));
            CheckOccurrence(getStatements);
            getStatements.Add(
                new CodeMethodReturnStatement(new CodeCastExpression(ReturnType,
                    new CodeVariableReferenceExpression("x"))));
        }

        private CodeTypeReference GetListType()
        {
            string listName;
            if (IsSubstitutionHead)
            {
                listName = Constants.XTypedSubstitutedList;
            }
            else if (typeRef.IsSimpleType)
            {
                listName = Constants.XSimpleList;
            }
            else
            {
                listName = Constants.XTypedList;
            }

            return new CodeTypeReference(listName, new CodeTypeReference(NullableType));
        }

        private CodeExpression[] GetListParameters(bool set, bool constructor)
        {
            CodeExpression[] listParameters = null;
            int paramCount = 2; //this, typeM/SD
            CodeExpression typeParam = null;
            CodeExpression nameOrValue = null;
            if (set)
            {
                //Value or propertyName in const
                paramCount++;
                if (constructor)
                {
                    nameOrValue = new CodeVariableReferenceExpression(propertyName);
                }
                else if (IsEnum)
                {
                    var lambdaExpr = new CodeSnippetExpression("item => item.ToString()");
                    nameOrValue = CodeDomHelper.CreateMethodCall(CodeDomHelper.SetValue(), "Select", lambdaExpr);
                }
                else
                {
                    nameOrValue = CodeDomHelper.SetValue();
                }
            }

            if (this.IsSubstitutionHead)
            {
                paramCount += SubstitutionMembers.Count;
                typeParam = CodeDomHelper.SingletonTypeManager();
            }
            else
            {
                paramCount++; //For XName of element
                if (typeRef.IsSimpleType)
                {
                    typeParam = GetSchemaDatatypeExpression();
                    if (fixedDefaultValue != null)
                        paramCount++;
                }
                else
                {
                    typeParam = CodeDomHelper.SingletonTypeManager();
                }
            }

            listParameters = new CodeExpression[paramCount];
            int paramIndex = 0;
            listParameters[paramIndex++] = CodeDomHelper.This();
            listParameters[paramIndex++] = typeParam;
            if (nameOrValue != null)
            {
                listParameters[paramIndex++] = nameOrValue;
            }

            if (this.IsSubstitutionHead)
            {
                foreach (XmlSchemaElement elem in SubstitutionMembers)
                {
                    listParameters[paramIndex++] =
                        CodeDomHelper.XNameGetExpression(elem.QualifiedName.Name, elem.QualifiedName.Namespace);
                }
            }
            else
            {
                listParameters[paramIndex++] = xNameExpression;
            }

            if (fixedDefaultValue != null)
            {
                if (FixedValue != null)
                {
                    listParameters[paramIndex++] = new CodeFieldReferenceExpression(null,
                        NameGenerator.ChangeClrName(this.propertyName, NameOptions.MakeFixedValueField));
                }
                else
                {
                    listParameters[paramIndex++] = new CodeFieldReferenceExpression(null,
                        NameGenerator.ChangeClrName(this.propertyName, NameOptions.MakeDefaultValueField));
                }
            }

            return listParameters;
        }

        protected CodeExpression GetSchemaDatatypeExpression()
        {
            return
                new CodeFieldReferenceExpression(CodeDomHelper.CreateMethodCall(
                        CodeDomHelper.CreateTypeReferenceExp(Constants.XmlSchemaType),
                        Constants.GetBuiltInSimpleType,
                        CodeDomHelper.CreateFieldReference(Constants.XmlTypeCode, typeRef.TypeCodeString)),
                    Constants.Datatype);
        }

        public string GetSimpleTypeDefinition(bool disambiguateProperty)
        {
            return disambiguateProperty && propertyName == simpleTypeClrTypeName
                ? $"global::{settings.GetClrNamespace(PropertyNs)}.{simpleTypeClrTypeName}.TypeDefinition"
                : $"{simpleTypeClrTypeName}.TypeDefinition";
        }

        public void CreateXNameField(CodeTypeDeclaration typeDecl)
        {
            // HACK: CodeDom doesn't model readonly fields... but it doesn't check the type either!
            var field = new CodeMemberField("readonly System.Xml.Linq.XName", NameGenerator.ChangeClrName(PropertyName, NameOptions.MakeXName))
            {
                Attributes = MemberAttributes.FamilyOrAssembly | MemberAttributes.Static | (IsNew ? MemberAttributes.New : 0),
                InitExpression = CodeDomHelper.XNameGetExpression(schemaName, propertyNs),
                CustomAttributes = {
                    new CodeAttributeDeclaration("DebuggerBrowsable", new CodeAttributeArgument(new CodeSnippetExpression("DebuggerBrowsableState.Never"))),
                    new CodeAttributeDeclaration("EditorBrowsable", new CodeAttributeArgument(new CodeSnippetExpression("EditorBrowsableState.Never"))),
                },
            };

            typeDecl.Members.Add(field);
        }

        public override CodeExpression GetXName()
        {
            return xNameExpression;
        }

        public void SetFixedDefaultValue(ClrWrapperTypeInfo typeInfo)
        {
            this.FixedValue = typeInfo.FixedValue;
            this.DefaultValue = typeInfo.DefaultValue;
        }

        protected void CreateFixedDefaultValue(CodeTypeDeclaration typeDecl)
        {
            if (fixedDefaultValue == null) return;
            // Add Fixed/Default value wrapping field

            CodeTypeReference returnType = unionDefaultType != null
                ? new CodeTypeReference(unionDefaultType.ToString())
                : ReturnType;

            var fieldName = NameGenerator.ChangeClrName(
                PropertyName,
                FixedValue != null ? NameOptions.MakeFixedValueField : NameOptions.MakeDefaultValueField /* DefaultValue != null */);

            var fixedOrDefaultField = new CodeMemberField(returnType, fieldName);
            CodeDomHelper.AddBrowseNever(fixedOrDefaultField);

            fixedOrDefaultField.Attributes =
                (fixedOrDefaultField.Attributes & ~MemberAttributes.AccessMask & ~MemberAttributes.ScopeMask)
                | MemberAttributes.Private
                | MemberAttributes.Static;

            fixedOrDefaultField.InitExpression =
                SimpleTypeCodeDomHelper.CreateFixedDefaultValueExpression(returnType, fixedDefaultValue, IsEnum);

            typeDecl.Members.Add(fixedOrDefaultField);
        }
    }
}