//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.CodeDom;
using System.Globalization;
using XObjects;

namespace Xml.Schema.Linq.CodeGen
{
    internal class XCodeTypeReference : CodeTypeReference
    {
        public string fullTypeName;

        public XCodeTypeReference(string typeName) : base(typeName)
        {
        }

        public XCodeTypeReference(string typeName, params CodeTypeReference[] typeArguments) : base(typeName,
            typeArguments)
        {
        }
    }

    internal abstract class ClrBasePropertyInfo : ContentInfo
    {
        protected string propertyName;
        protected string schemaName;
        protected string propertyNs;

        protected bool hasSet;
        protected XCodeTypeReference returnType;
        protected XCodeTypeReference defaultValueType;
        protected bool isVirtual;
        protected bool isOverride;

        //Intellisense type information
        protected List<ClrAnnotation> annotations;

        public ClrBasePropertyInfo()
        {
            this.IsVirtual = false;
            this.isOverride = false;
            this.returnType = null;
            this.defaultValueType = null;
            annotations = new List<ClrAnnotation>();
        }

        internal string PropertyName
        {
            get { return propertyName; }
            set { propertyName = value; }
        }

        internal string SchemaName
        {
            get { return schemaName; }
            set { schemaName = value; }
        }

        internal string PropertyNs
        {
            get { return propertyNs; }
            set { propertyNs = value; }
        }

        internal virtual bool IsList
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        internal bool HasSet
        {
            get { return hasSet; }
            set { hasSet = value; }
        }

        internal virtual bool FromBaseType
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        internal virtual bool IsNew
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        internal virtual bool IsDuplicate
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        internal virtual bool IsNullable
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        internal virtual bool VerifyRequired
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        internal virtual XCodeTypeReference ReturnType
        {
            get { return returnType; }
            set { returnType = value; }
        }

        internal virtual XCodeTypeReference DefaultValueType
        {
            get { return defaultValueType; }
            set { defaultValueType = value; }
        }

        internal virtual string ClrTypeName
        {
            get { return null; }
        }

        internal bool IsVirtual
        {
            get { return this.isVirtual; }
            set { this.isVirtual = value; }
        }

        internal bool IsOverride
        {
            get { return this.isOverride; }
            set { this.isOverride = value; }
        }

        internal virtual bool IsUnion
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        internal virtual bool IsEnum
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        internal virtual bool IsSchemaList
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        internal List<ClrAnnotation> Annotations
        {
            get { return annotations; }
        }


        internal abstract CodeMemberProperty AddToType(CodeTypeDeclaration decl, List<ClrAnnotation> annotations, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public);
        internal abstract void AddToContentModel(CodeObjectCreateExpression contentModelExpression);
        internal abstract void AddToConstructor(CodeConstructor functionalConstructor);

        internal virtual void ApplyAnnotations(CodeMemberProperty propDecl, List<ClrAnnotation> typeAnnotations)
        {
            TypeBuilder.ApplyAnnotations(propDecl, this, typeAnnotations);
        }

        internal virtual CodeExpression GetXName()
        {
            return CodeDomHelper.XNameGetExpression(SchemaName, PropertyNs);
        }
    }

    internal partial class ClrWildCardPropertyInfo : ClrBasePropertyInfo
    {
        string namespaces;
        string targetNamespace;

        bool addToTypeDef;

        public bool AddToTypeDef
        {
            get { return addToTypeDef; }
            set { addToTypeDef = value; }
        }


        internal string Namespaces
        {
            get { return namespaces; }
        }

        internal string TargetNamespace
        {
            get { return targetNamespace; }
        }

        internal override XCodeTypeReference ReturnType
        {
            get { return new XCodeTypeReference("IEnumerable", new CodeTypeReference(Constants.XElement)); }
        }

        internal ClrWildCardPropertyInfo(string ns, string targetNs, bool addToType, Occurs schemaOccurs)
        {
            namespaces = ns;
            targetNamespace = targetNs;
            this.contentType = ContentType.WildCardProperty;
            this.addToTypeDef = addToType;
            this.occursInSchema = schemaOccurs;
        }

        internal override CodeMemberProperty AddToType(CodeTypeDeclaration decl, List<ClrAnnotation> annotations,
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            if (!addToTypeDef) return null;

            CodeMemberProperty property = CodeDomHelper.CreateProperty(ReturnType, false, visibility.ToMemberAttribute());
            property.Name = PropertyName;
            //property.Attributes = (property.Attributes & ~MemberAttributes.AccessMask) | visibility.ToMemberAttribute();

            AddGetStatements(property.GetStatements);

            ApplyAnnotations(property, annotations);

            decl.Members.Add(property);
            return property;
        }

        internal void AddGetStatements(CodeStatementCollection getStatements)
        {
            getStatements.Add(
                new CodeMethodReturnStatement(CodeDomHelper.CreateMethodCall(CodeDomHelper.This(), "GetWildCards",
                    CodeDomHelper.CreateFieldReference(Constants.WildCard, "DefaultWildCard")))
            );
        }

        internal override void AddToConstructor(CodeConstructor functionalConstructor)
        {
            throw new InvalidOperationException();
        }

        internal override void AddToContentModel(CodeObjectCreateExpression contentModelExpression)
        {
            throw new InvalidOperationException();
        }
    }

    internal class ClrWrappingPropertyInfo : ClrBasePropertyInfo
    {
        string wrappedFieldName;
        const int propertySuffixIndex = 1;
        MemberAttributes wrappedPropertyAttributes;
        CodeCommentStatementCollection codeCommentStatementCollection;

        public ClrWrappingPropertyInfo()
        {
            this.contentType = ContentType.Property;
        }

        public void Init(CodeMemberProperty property)
        {
            propertyName = property.Name;
            returnType = (XCodeTypeReference) property.Type;
            if (returnType.fullTypeName != null && returnType.fullTypeName != returnType.BaseType)
            {
                returnType = new XCodeTypeReference(returnType.fullTypeName);
            }

            hasSet = property.HasSet;
            wrappedPropertyAttributes = property.Attributes;

            codeCommentStatementCollection = property.Comments;
        }

        internal string WrappedFieldName
        {
            get { return wrappedFieldName; }
            set { wrappedFieldName = value; }
        }

        internal override CodeMemberProperty AddToType(CodeTypeDeclaration typeDecl, List<ClrAnnotation> annotations, 
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeMemberProperty wrapperProperty = CodeDomHelper.CreateProperty(this.returnType, this.hasSet, visibility.ToMemberAttribute());
            wrapperProperty.Name = CheckPropertyName(typeDecl.Name);
            wrapperProperty.Attributes = this.wrappedPropertyAttributes;

            AddGetStatements(wrapperProperty.GetStatements);

            if (this.hasSet)
            {
                AddSetStatements(wrapperProperty.SetStatements);
            }

            ApplyAnnotations(wrapperProperty, annotations);

            typeDecl.Members.Add(wrapperProperty);
            return wrapperProperty;
        }

        private string CheckPropertyName(string className)
        {
            if (this.propertyName.Equals(className))
            {
                //This can happen as property names are checked against the wrapped complex type name and not against the global element name
                return this.propertyName + propertySuffixIndex.ToString(CultureInfo.InvariantCulture.NumberFormat);
            }

            return this.propertyName;
        }

        private void AddGetStatements(CodeStatementCollection getStatements)
        {
            getStatements.Add(new CodeMethodReturnStatement
            (new CodePropertyReferenceExpression(
                new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(),
                    this.wrappedFieldName),
                this.propertyName)));
        }

        private void AddSetStatements(CodeStatementCollection setStatements)
        {
            setStatements.Add(new CodeAssignStatement(
                new CodePropertyReferenceExpression(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        this.wrappedFieldName),
                    this.propertyName),
                CodeDomHelper.SetValue()));
        }

        internal override void AddToConstructor(CodeConstructor functionalConstructor)
        {
            //Do nothing for now
            //If wrapped property has setter, then add it to func const
        }

        internal override void AddToContentModel(CodeObjectCreateExpression contentModelExpression)
        {
            throw new InvalidOperationException();
        }

        internal override void ApplyAnnotations(CodeMemberProperty propDecl, List<ClrAnnotation> annotations)
        {
            foreach (CodeCommentStatement comm in codeCommentStatementCollection)
            {
                propDecl.Comments.Add(new CodeCommentStatement(comm.Comment.Text, comm.Comment.DocComment));
            }
        }
    }

    internal partial class ClrPropertyInfo : ClrBasePropertyInfo
    {
        ClrTypeReference typeRef;
        PropertyFlags propertyFlags;
        SchemaOrigin propertyOrigin;

        CodeExpression xNameExpression;
        string parentTypeFullName;
        string clrTypeName;
        string clrNamespace;
        string fixedDefaultValue;
        string simpleTypeClrTypeName;

        ArrayList substitutionMembers;

        internal ClrPropertyInfo(string propertyName, string propertyNs, string schemaName, Occurs occursInSchema)
        {
            this.contentType = ContentType.Property;
            this.propertyName = propertyName;
            this.propertyNs = propertyNs;
            this.schemaName = schemaName;            
            this.hasSet = true;
            this.returnType = null;
            this.defaultValueType = null;
            this.clrTypeName = null;
            this.occursInSchema = occursInSchema;
            if (this.occursInSchema > Occurs.ZeroOrOne)
            {
                this.propertyFlags |= PropertyFlags.IsList;
            }

            if (this.IsOptional)
            {
                this.propertyFlags |= PropertyFlags.IsNullable;
            }

            this.xNameExpression = new CodeFieldReferenceExpression(null, NameGenerator.ChangeClrName(propertyName, NameOptions.MakeXName));
        }

        internal void Reset()
        {
            this.returnType = null;
            this.clrTypeName = null;
            this.clrNamespace = null;
            this.fixedDefaultValue = null;
            this.propertyFlags = PropertyFlags.None;
        }

        internal Type unionDefaultType;

        internal string FixedValue
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

        internal string DefaultValue
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

        internal bool IsRef
        {
            get { return typeRef.IsTypeRef; }
        }

        internal ClrTypeReference TypeReference
        {
            get { return typeRef; }
            set { typeRef = value; }
        }

        internal ArrayList SubstitutionMembers
        {
            get { return substitutionMembers; }
            set { substitutionMembers = value; }
        }

        internal bool IsSubstitutionHead
        {
            get { return substitutionMembers != null; }
        }

        internal SchemaOrigin Origin
        {
            get { return propertyOrigin; }
            set { propertyOrigin = value; }
        }

        internal override string ClrTypeName
        {
            get { return clrTypeName; }
        }

        internal string ClrNamespace
        {
            get { return clrNamespace; }
            set { clrNamespace = value; }
        }

        internal override bool IsList
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

        internal override bool IsNullable
        {
            get { return (propertyFlags & PropertyFlags.IsNullable) != 0 && fixedDefaultValue == null; }
            set
            {
                if (value)
                {
                    propertyFlags |= PropertyFlags.IsNullable;
                }
                else
                {
                    propertyFlags &= ~PropertyFlags.IsNullable;
                }
            }
        }

        internal override bool IsSchemaList
        {
            get { return this.typeRef.IsSchemaList; }
        }

        internal override bool IsUnion
        {
            get { return this.typeRef.IsUnion; }
        }

        internal override bool IsEnum
        {
            get { return this.typeRef.IsEnum; }
        }

        internal bool Validation
        {
            get { return this.typeRef.Validate && !IsRef; }
        }

        internal override bool FromBaseType
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

        internal override bool IsDuplicate
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

        internal override bool IsNew
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

        internal override bool VerifyRequired
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

        internal override XCodeTypeReference ReturnType
        {
            get
            {
                if (returnType == null)
                {
                    if (IsEnum)
                    {
                        returnType = CreateReturnType(typeRef.ClrFullTypeName);

                    }
                    else
                    {
                        returnType = CreateReturnType(clrTypeName);
                    }
                }

                return returnType;
            }
        }

        internal override XCodeTypeReference DefaultValueType
        {
            get
            {
                if (defaultValueType == null)
                {
                    defaultValueType = CreateReturnType(clrTypeName);
                }
                return defaultValueType;
            }
        }

        private XCodeTypeReference CreateReturnType(string typeName)
        {
            XCodeTypeReference returnType;
            string fullTypeName = typeName;
            if (typeRef.IsLocalType && !typeRef.IsSimpleType)
            {
                //For simple types, return type is always XSD -> CLR mapping
                fullTypeName = parentTypeFullName + "." + typeName;
            }

            if (IsList || !IsRef && IsSchemaList)
            {
                returnType = CreateListReturnType(fullTypeName);
            }
            else if (!IsRef && typeRef.IsValueType && IsNullable)
            {
                returnType = new XCodeTypeReference("System.Nullable", new CodeTypeReference(fullTypeName));
            }
            else
            {
                returnType = new XCodeTypeReference(typeName);
                returnType.fullTypeName = fullTypeName;
            }
            return returnType;
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

        internal void UpdateTypeReference(string clrFullTypeName, string currentNamespace,
            Dictionary<XmlSchemaObject, string> nameMappings)
        {
            string refTypeName = null;
            this.clrTypeName = typeRef.GetClrFullTypeName(currentNamespace, nameMappings, out refTypeName);
            if (Validation || IsUnion)
            {
                this.simpleTypeClrTypeName = typeRef.GetSimpleTypeClrTypeDefName(currentNamespace, nameMappings);
            }

            typeRef.UpdateClrFullTypeName(this, currentNamespace);

            this.parentTypeFullName = clrFullTypeName;
        }

        internal void SetPropertyAttributes(CodeMemberProperty clrProperty, MemberAttributes visibility)
        {
            if (isVirtual)
            {
                clrProperty.Attributes =
                    ((clrProperty.Attributes & ~MemberAttributes.ScopeMask & ~MemberAttributes.AccessMask) |
                     visibility);
            }
            else if (isOverride)
            {
                clrProperty.Attributes =
                    ((clrProperty.Attributes & ~MemberAttributes.ScopeMask & ~MemberAttributes.AccessMask) |
                     MemberAttributes.Public | MemberAttributes.Override);
            }
        }

        internal override CodeMemberProperty AddToType(CodeTypeDeclaration parentTypeDecl,
            List<ClrAnnotation> annotations, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            if (IsDuplicate || (FromBaseType && !IsNew))
            {
                return null;
            }

            CreateXNameField(parentTypeDecl);
            CreateFixedDefaultValue(parentTypeDecl);
            CodeMemberProperty clrProperty = CodeDomHelper.CreateProperty(ReturnType, hasSet, visibility.ToMemberAttribute());
            clrProperty.Name = propertyName;
            SetPropertyAttributes(clrProperty, visibility.ToMemberAttribute());
            if (IsNew)
            {
                clrProperty.Attributes |= MemberAttributes.New;
            }

            if (IsList)
            {
                //Create collection type for list
                CodeTypeReference listType = GetListType();
                string listName = NameGenerator.ChangeClrName(propertyName, NameOptions.MakeField);
                AddMemberField(listName, listType, parentTypeDecl);

                //GetStatements
                AddListGetStatements(clrProperty.GetStatements, listType, listName);
                if (hasSet)
                {
                    AddListSetStatements(clrProperty.SetStatements, listType, listName);
                }
            }
            else
            {
                AddGetStatements(clrProperty.GetStatements);
                if (hasSet)
                {
                    AddSetStatements(clrProperty.SetStatements);
                }
            }

            ApplyAnnotations(clrProperty, annotations);
            parentTypeDecl.Members.Add(clrProperty);
            return clrProperty;
        }

        internal override void AddToContentModel(CodeObjectCreateExpression contentModelExpression)
        {
            Debug.Assert(contentModelExpression != null && propertyOrigin == SchemaOrigin.Element);
            if (this.IsSubstitutionHead)
            {
                //Need to add member names to content model
                CodeExpression[] substParams = new CodeExpression[substitutionMembers.Count];
                int i = 0;
                foreach (XmlSchemaElement elem in substitutionMembers)
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
                    new CodeObjectCreateExpression(Constants.NamedContentModelEntity,
                        xNameExpression));
            }
        }

        internal override void AddToConstructor(CodeConstructor functionalConstructor)
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
                                Constants.Initialize,
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

        private CodeVariableDeclarationStatement GetValueMethodCall()
        {
            switch (propertyOrigin)
            {
                case SchemaOrigin.Element:
                    return new CodeVariableDeclarationStatement(
                        "XElement",
                        "x",
                        CodeDomHelper.CreateMethodCall(CodeDomHelper.This(), "GetElement", xNameExpression));

                case SchemaOrigin.Attribute:
                    return new CodeVariableDeclarationStatement(
                        "XAttribute",
                        "x",
                        CodeDomHelper.CreateMethodCall(CodeDomHelper.This(), "Attribute", xNameExpression));

                case SchemaOrigin.Text:
                    return new CodeVariableDeclarationStatement(
                        "XElement",
                        "x",
                        new CodePropertyReferenceExpression(CodeDomHelper.This(), Constants.Untyped));

                case SchemaOrigin.None:
                default:
                    throw new InvalidOperationException();
            }
        }

        private void AddSetValueMethodCall(CodeStatementCollection setStatements)
        {            
            string setMethodName = "Set";
            if (!IsRef && IsSchemaList)
            {
                setMethodName = "SetList";
            }
            else if (IsUnion)
            {
                setMethodName = "SetUnion";
            }

            bool validation = Validation;
            bool xNameParm = true;
            switch (propertyOrigin)
            {
                case SchemaOrigin.Element:
                    setMethodName += "Element";
                    break;

                case SchemaOrigin.Attribute:
                    validation = false;
                    setMethodName += "Attribute";
                    break;

                case SchemaOrigin.Text:
                    setMethodName += "Value";
                    xNameParm = false;
                    break;

                case SchemaOrigin.None:
                default:
                    throw new InvalidOperationException();
            }

            if (IsUnion)
            {
                if (xNameParm)
                {
                    setStatements.Add(
                        CodeDomHelper.CreateMethodCall(
                            CodeDomHelper.This(), 
                            setMethodName,
                            CodeDomHelper.SetValue(),
                            new CodePrimitiveExpression(this.propertyName),
                            CodeDomHelper.This(),
                            xNameExpression,
                            GetSimpleTypeClassExpression())
                    );
                }
                else
                {
                    setStatements.Add(
                        CodeDomHelper.CreateMethodCall(
                            CodeDomHelper.This(), 
                            setMethodName,
                            CodeDomHelper.SetValue(),
                            new CodePrimitiveExpression(this.propertyName),
                            CodeDomHelper.This(),
                            GetSimpleTypeClassExpression())
                    );
                }
            }
            else if (validation)
            {
                var valueExpr = new CodeSnippetExpression(IsEnum ? "value.ToString()" : "value");
                CodeMethodInvokeExpression setWithValidation;
                if (xNameParm)
                {
                    var setValue = CodeDomHelper.SetValue();
                    setWithValidation = CodeDomHelper.CreateMethodCall(
                        CodeDomHelper.This(), 
                        setMethodName + "WithValidation",
                        xNameExpression,
                        valueExpr,
                        new CodePrimitiveExpression(PropertyName),
                        GetSimpleTypeClassExpression());
                }
                else
                {
                    setWithValidation = CodeDomHelper.CreateMethodCall(
                        CodeDomHelper.This(), 
                        setMethodName + "WithValidation",
                        valueExpr,
                        new CodePrimitiveExpression(PropertyName),
                        GetSimpleTypeClassExpression());
                }

                // Skip validation when the set value is null. Also enum.ToString above would fail.
                // (Setting an optional element to null actually removes it from DOM.)
                if (IsNullable)
                {
                    setStatements.Add(new CodeConditionStatement(
                        new CodeSnippetExpression("value == null"),
                        new[] { new CodeExpressionStatement(CreatePlainSetCall(setMethodName, "null", xNameParm)) },
                        new[] { new CodeExpressionStatement(setWithValidation) }
                    ));
                }
                else
                {
                    setStatements.Add(setWithValidation);
                }
            }
            else
            {
                var valueExpr = !IsEnum ? "value" : IsNullable ? "value?.ToString()" : "value.ToString()";
                setStatements.Add(CreatePlainSetCall(setMethodName, valueExpr, xNameParm));
            }
        }

        private CodeExpression CreatePlainSetCall(string setMethodName, string valueExpr, bool xNameParm)
        {
            if (xNameParm)
            {
                var methodCall = CodeDomHelper.CreateMethodCall(
                    CodeDomHelper.This(), 
                    setMethodName,
                    xNameExpression,
                    new CodeSnippetExpression(valueExpr)
                );
                if (!IsRef && typeRef.IsSimpleType)
                {
                    methodCall.Parameters.Add(GetSchemaDatatypeExpression());
                }
                return methodCall;
            }
            else
            {
                return CodeDomHelper.CreateMethodCall(
                    CodeDomHelper.This(), 
                    setMethodName,
                    new CodeSnippetExpression(valueExpr),
                    GetSchemaDatatypeExpression()
                );
            }
        }

        private void AddListGetStatements(CodeStatementCollection getStatements, CodeTypeReference listType,
            string listName)
        {
            if (FixedValue != null)
            {
                getStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(null,
                            NameGenerator.ChangeClrName(this.propertyName, NameOptions.MakeFixedValueField)
                        ))
                );
                return;
            }

            var listFieldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), listName);

            getStatements.Add(
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(
                        listFieldRef,
                        CodeBinaryOperatorType.IdentityEquality,
                        new CodePrimitiveExpression(null)
                    ),
                    new CodeAssignStatement(
                        listFieldRef,
                        new CodeObjectCreateExpression(
                            listType,
                            GetListParameters(false /*set*/, false /*constructor*/)
                        ))));

            if (IsEnum)
            {
                var lambdaExpr = new CodeSnippetExpression($"item => ({this.TypeReference.ClrFullTypeName}) Enum.Parse(typeof({this.TypeReference.ClrFullTypeName}), item)");
                var selectExpr = CodeDomHelper.CreateMethodCall(listFieldRef, "Select", lambdaExpr);
                var toListExpr = new CodeMethodInvokeExpression(selectExpr, "ToList");
                getStatements.Add(
                    new CodeMethodReturnStatement(
                        toListExpr));
            }
            else
            {
                getStatements.Add(
                    new CodeMethodReturnStatement(
                        listFieldRef));
            }
        }


        private void AddListSetStatements(CodeStatementCollection setStatements, CodeTypeReference listType,
            string listName)
        {
            AddFixedValueChecking(setStatements);

            var listFieldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), listName);

            CodeStatement[] trueStatements =
                new CodeStatement[]
                {
                    new CodeAssignStatement( //True 1 Then
                        listFieldRef,
                        new CodePrimitiveExpression(null)
                    )
                };

            CodeStatement[] falseStatements =
                new CodeStatement[]
                {
                    new CodeConditionStatement( //False 1 Else if
                        new CodeBinaryOperatorExpression( // Condition2
                            listFieldRef,
                            CodeBinaryOperatorType.IdentityEquality,
                            new CodePrimitiveExpression(null)
                        ),
                        new CodeStatement[]
                        {
                            new CodeAssignStatement( //Then 2
                                listFieldRef,
                                new CodeMethodInvokeExpression(
                                    new CodeTypeReferenceExpression(listType),
                                    Constants.Initialize,
                                    GetListParameters(true /*set*/, false /*constructor*/))
                            )
                        },
                        new CodeStatement[]
                        {
                            new CodeExpressionStatement(
                                new CodeMethodInvokeExpression(
                                    new CodeTypeReferenceExpression("XTypedServices"),
                                    Constants.SetList + "<" + clrTypeName + ">",
                                    listFieldRef,
                                    GetListSetStatementValueExpr()))
                        })
                };

            setStatements.Add(
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression( //if
                        CodeDomHelper.SetValue(),
                        CodeBinaryOperatorType.IdentityEquality,
                        new CodePrimitiveExpression(null)
                    ),
                    trueStatements,
                    falseStatements));
        }

        private CodeExpression GetListSetStatementValueExpr()
        {
            if (IsEnum)
            {
                var lambdaExpr = new CodeSnippetExpression("item => item.ToString()");
                var selectExpr = CodeDomHelper.CreateMethodCall(CodeDomHelper.SetValue(), "Select", lambdaExpr);
                return new CodeMethodInvokeExpression(selectExpr, "ToList");
            }
            else
            {
                return CodeDomHelper.SetValue();
            }
        }

        private void AddGetStatements(CodeStatementCollection getStatements)
        {
            if (IsSubstitutionHead)
            {
                AddSubstGetStatements(getStatements);
                return;
            }

            CodeExpression returnExp = null;

            if (FixedValue != null)
            {
                getStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(null,
                            NameGenerator.ChangeClrName(this.propertyName, NameOptions.MakeFixedValueField)
                        ))
                );
                return;
            }

            getStatements.Add(GetValueMethodCall());
            CheckOccurrence(getStatements);
            CodeVariableReferenceExpression returnValueExp = new CodeVariableReferenceExpression("x");
            if (!IsRef && typeRef.IsSimpleType)
            {
                //for referencing properties, directly create the object of referenced type
                CodeTypeReference parseType = DefaultValueType;
                if (typeRef.IsValueType && IsNullable)
                {
                    parseType = new CodeTypeReference(clrTypeName);
                }

                if (IsUnion)
                {
                    returnExp = CodeDomHelper.CreateMethodCall(
                        CodeDomHelper.CreateTypeReferenceExp(Constants.XTypedServices),
                        Constants.ParseUnionValue,
                        returnValueExp,
                        GetSimpleTypeClassExpression());
                }
                else
                {
                    string parseMethodName = null;
                    CodeExpression simpleTypeExpression = GetSchemaDatatypeExpression();
                    if (IsSchemaList)
                    {
                        parseMethodName = Constants.ParseListValue;
                        parseType = new CodeTypeReference(clrTypeName);
                    }
                    else
                    {
                        parseMethodName = Constants.ParseValue;
                    }

                    returnExp = CodeDomHelper.CreateGenericMethodCall(
                        CodeDomHelper.CreateTypeReferenceExp(Constants.XTypedServices),
                        parseMethodName,
                        parseType,
                        returnValueExp,
                        simpleTypeExpression);

                    if (DefaultValue != null)
                    {
                        ((CodeMethodInvokeExpression) returnExp).Parameters.Add(
                            new CodeFieldReferenceExpression(null,
                                NameGenerator.ChangeClrName(this.propertyName,
                                    NameOptions.MakeDefaultValueField)));
                    }

                    if (this.IsEnum)
                    {
                        // (EnumType) Enum.Parse(typeof(EnumType), returnExp)
                        returnExp = CodeDomHelper.CreateParseEnumCall(this.TypeReference.ClrFullTypeName, returnExp);
                    }
                }
            }
            else
            {
                returnExp = new CodeCastExpression(ReturnType, returnValueExp);
            }

            getStatements.Add(new CodeMethodReturnStatement(returnExp));
        }

        private void CheckOccurrence(CodeStatementCollection getStatements)
        {
            Debug.Assert(!this.IsList);
            CodeStatement returnStatement = null;
            if (IsNullable && DefaultValue == null)
            {
                if (typeRef.IsValueType)
                {
                    //Need to return T?, since parseValue handles only T, checking for null
                    returnStatement = new CodeMethodReturnStatement(new CodePrimitiveExpression(null));
                }
            }
            else if (VerifyRequired)
            {
                Debug.Assert(this.occursInSchema == Occurs.One);
                string origin = this.propertyOrigin == SchemaOrigin.Element ? "Element" :
                    this.propertyOrigin == SchemaOrigin.Attribute ? "Attribute" : null;
                returnStatement = new CodeThrowExceptionStatement(new CodeObjectCreateExpression(
                    Constants.LinqToXsdException, new CodePrimitiveExpression("Missing required " + origin)));
            }

            if (returnStatement != null)
            {
                getStatements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodeVariableReferenceExpression("x"),
                            CodeBinaryOperatorType.IdentityEquality,
                            new CodePrimitiveExpression(null)),
                        returnStatement));
            }
        }

        private void AddSetStatements(CodeStatementCollection setStatements)
        {
            AddFixedValueChecking(setStatements);
            AddSetValueMethodCall(setStatements);
        }

        private void AddSubstGetStatements(CodeStatementCollection getStatements)
        {
            Debug.Assert(propertyOrigin == SchemaOrigin.Element);
            CodeExpression[] substParams = new CodeExpression[substitutionMembers.Count + 2];
            substParams[0] = CodeDomHelper.This();
            substParams[1] = CodeDomHelper.SingletonTypeManager();
            int i = 2;
            foreach (XmlSchemaElement elem in substitutionMembers)
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

        private void AddMemberField(string memberName, CodeTypeReference memberType, CodeTypeDeclaration parentType)
        {
            // Construct private field
            CodeMemberField mem = new CodeMemberField(memberType, memberName);
            mem.Attributes = MemberAttributes.Private;
            CodeDomHelper.AddBrowseNever(mem);
            parentType.Members.Add(mem);
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

            return new CodeTypeReference(listName, new CodeTypeReference(clrTypeName));
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
                paramCount += substitutionMembers.Count;
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
                foreach (XmlSchemaElement elem in substitutionMembers)
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

        protected CodeExpression GetSimpleTypeClassExpression()
        {
            Debug.Assert(this.simpleTypeClrTypeName != null);

            return CodeDomHelper.CreateFieldReference(
                this.simpleTypeClrTypeName, Constants.SimpleTypeDefInnerType);
        }

        private void CreateXNameField(CodeTypeDeclaration typeDecl)
        {
            // HACK: CodeDom doesn't model readonly fields... but it doesn't check the type either!
            var field = new CodeMemberField("readonly System.Xml.Linq.XName", NameGenerator.ChangeClrName(PropertyName, NameOptions.MakeXName))
            {
                Attributes = MemberAttributes.Assembly | MemberAttributes.Static | (IsNew ? MemberAttributes.New : 0),
                InitExpression = CodeDomHelper.XNameGetExpression(schemaName, propertyNs),
                CustomAttributes = {
                    new CodeAttributeDeclaration("DebuggerBrowsable", new CodeAttributeArgument(new CodeSnippetExpression("DebuggerBrowsableState.Never"))),
                    new CodeAttributeDeclaration("EditorBrowsable", new CodeAttributeArgument(new CodeSnippetExpression("EditorBrowsableState.Never"))),
                },
            };
            
            typeDecl.Members.Add(field);
        }

        internal override CodeExpression GetXName()
        {
            return xNameExpression;
        }

        internal void SetFixedDefaultValue(ClrWrapperTypeInfo typeInfo)
        {
            this.FixedValue = typeInfo.FixedValue;
            this.DefaultValue = typeInfo.DefaultValue;
        }

        protected void CreateFixedDefaultValue(CodeTypeDeclaration typeDecl)
        {
            if (fixedDefaultValue != null)
            {
                //Add Fixed/Default value wrapping field
                CodeMemberField fixedOrDefaultField = null;
                CodeTypeReference returnType = DefaultValueType;
                if (this.unionDefaultType != null)
                {
                    returnType = new CodeTypeReference(unionDefaultType.ToString());
                }

                if (FixedValue != null)
                {
                    fixedOrDefaultField = new CodeMemberField(returnType,
                        NameGenerator.ChangeClrName(PropertyName, NameOptions.MakeFixedValueField));
                }
                else // if (DefaultValue != null)
                {
                    fixedOrDefaultField = new CodeMemberField(returnType,
                        NameGenerator.ChangeClrName(PropertyName, NameOptions.MakeDefaultValueField));
                }

                CodeDomHelper.AddBrowseNever(fixedOrDefaultField);
                fixedOrDefaultField.Attributes = (fixedOrDefaultField.Attributes & ~MemberAttributes.AccessMask &
                                                  ~MemberAttributes.ScopeMask)
                                                 | MemberAttributes.Private | MemberAttributes.Static;

                fixedOrDefaultField.InitExpression =
                    SimpleTypeCodeDomHelper.CreateFixedDefaultValueExpression(returnType, fixedDefaultValue);
                typeDecl.Members.Add(fixedOrDefaultField);
            }
        }

        protected void AddFixedValueChecking(CodeStatementCollection setStatements)
        {
            if (FixedValue != null)
            {
                CodeExpression fixedValueExpr =
                    new CodeFieldReferenceExpression(null,
                        NameGenerator.ChangeClrName(this.propertyName, NameOptions.MakeFixedValueField));
                setStatements.Add(
                    new CodeConditionStatement(
                        CodeDomHelper.CreateMethodCall(
                            new CodePropertySetValueReferenceExpression(),
                            Constants.EqualityCheck,
                            fixedValueExpr
                        ),
                        new CodeStatement[] { },
                        new CodeStatement[]
                        {
                            new CodeThrowExceptionStatement(
                                new CodeObjectCreateExpression(typeof(LinqToXsdFixedValueException),
                                    new CodePropertySetValueReferenceExpression(),
                                    fixedValueExpr))
                        }
                    )
                );
            }
        }

        internal virtual void InsertDefaultFixedValueInDefaultCtor(CodeConstructor ctor)
        {
            if (this.FixedValue != null)
            {
                ctor.Statements.Add(
                    new CodeAssignStatement(
                        CodeDomHelper.CreateFieldReference(null, propertyName),
                        CodeDomHelper.CreateFieldReference(null,
                            NameGenerator.ChangeClrName(propertyName, NameOptions.MakeFixedValueField))));
            }
            else if (DefaultValue != null)
            {
                ctor.Statements.Add(
                    new CodeAssignStatement(
                        CodeDomHelper.CreateFieldReference(null, propertyName),
                        CodeDomHelper.CreateFieldReference(null,
                            NameGenerator.ChangeClrName(propertyName, NameOptions.MakeDefaultValueField))));
            }
        }

        private CodeStatement CreateDefaultValueAssignStmt(object value)
        {
            return new CodeAssignStatement(
                CodeDomHelper.CreateFieldReference(null, propertyName),
                CodeDomHelper.CreateFieldReference(null,
                    NameGenerator.ChangeClrName(propertyName, NameOptions.MakeDefaultValueField)));
        }

        private CodeStatement[] ToStmtArray(CodeStatementCollection collection)
        {
            CodeStatement[] stmts = new CodeStatement[collection.Count];

            for (int i = 0; i < collection.Count; i++)
            {
                stmts[i] = collection[i];
            }

            return stmts;
        }
    }
}