//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        ArrayList substitutionMembers;

        public ClrPropertyInfo(string propertyName, string propertyNs, string schemaName, Occurs occursInSchema, LinqToXsdSettings settings)
        {
            this.settings = settings;
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

        public ArrayList SubstitutionMembers
        {
            get { return substitutionMembers; }
            set { substitutionMembers = value; }
        }

        public bool IsSubstitutionHead
        {
            get { return substitutionMembers != null; }
        }

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
            get { return (CanBeAbsent && fixedDefaultValue == null) || IsNillable; }
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
            get { return this.typeRef.IsSchemaList; }
        }

        public override bool IsUnion
        {
            get { return this.typeRef.IsUnion; }
        }

        public override bool IsEnum
        {
            get { return this.typeRef.IsEnum; }
        }

        public bool Validation
        {
            get { return this.typeRef.Validate && !IsRef; }
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

        public override XCodeTypeReference DefaultValueType
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

        private string QualifiedType => typeRef.IsLocalType && !typeRef.IsSimpleType
            ? parentTypeFullName + "." + clrTypeName
            : clrTypeName;

        private string NullableType => IsNillable && (settings.NullableReferences || typeRef.IsValueType)
            ? QualifiedType + "?"
            : QualifiedType;

        private XCodeTypeReference CreateReturnType(string typeName)
        {
            if (IsList || !IsRef && IsSchemaList)
            {
                return CreateListReturnType(NullableType);
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
                    createNestedEnumType(typeRef);
                }
            }

            this.clrTypeName = typeRef.GetClrFullTypeName(currentNamespaceScope, nameMappings, settings, out string refTypeName);

            if (Validation || IsUnion)
            {
                this.simpleTypeClrTypeName = typeRef.GetSimpleTypeClrTypeDefName(currentNamespaceScope, nameMappings);
            }

            this.parentTypeFullName = typeRef.IsEnum ? typeRef.UpdateClrFullEnumTypeName(this, currentTypeScope, currentNamespaceScope) : currentTypeScope;
        }

        public void SetPropertyAttributes(CodeMemberProperty clrProperty, MemberAttributes visibility)
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

        public override CodeMemberProperty AddToType(CodeTypeDeclaration parentTypeDecl,
            List<ClrAnnotation> annotations, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            if (!ShouldGenerate)
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

                if (settings.NullableReferences)
                {
                    clrProperty.CustomAttributes.Add(new CodeAttributeDeclaration("System.Diagnostics.CodeAnalysis.AllowNull"));
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

        public override void AddToContentModel(CodeObjectCreateExpression contentModelExpression)
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
                var codeExpressionParams = new List<CodeExpression>() {
                    CodeDomHelper.SetValue(),
                    new CodePrimitiveExpression(this.propertyName),
                    CodeDomHelper.This(),
                    xNameParm ? xNameExpression : null,
                    GetSimpleTypeClassExpression(IsUnion)
                };

                var codeMethodInvokeExpression = CodeDomHelper.CreateMethodCall(
                    targetOBject: CodeDomHelper.This(),
                    methodName: setMethodName,
                    parameters: codeExpressionParams.ToNoDefaultArray());

                #if DEBUG
                var invokeExpressionString = codeMethodInvokeExpression.ToCodeString();
                Debug.Assert(invokeExpressionString != null);
                #endif
                setStatements.Add(codeMethodInvokeExpression);
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
                        new[] { new CodeExpressionStatement(CreatePlainSetCall(setMethodName, IsNillable ? "XNil.Value" : "null", xNameParm)) },
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
                string valueExpr = !IsEnum || propertyOrigin == SchemaOrigin.Element
                    ? "value"
                    : IsNullable 
                        ? "value?.ToString()"
                        : "value.ToString()";
                
                if (IsNillable)
                {
                    valueExpr += " ?? XNil.Value";
                }

                var setter = CreatePlainSetCall(setMethodName, valueExpr, xNameParm);
                setStatements.Add(setter);
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
                                    Constants.SetList + "<" + NullableType + ">",
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
            CheckNillable(getStatements);
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
                        GetSimpleTypeClassExpression(IsUnion));
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
                        if (IsEnum) {
                            if (TypeReference.SchemaObject is XmlSchemaSimpleType simpleSchemaType) {
                                parseType = new CodeTypeReference(simpleSchemaType.Datatype.ValueType);
                            }
                        }
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
            if (CanBeAbsent && DefaultValue == null)
            {
                // For value types, this is needed to return T?, since ParseValue return T.
                // It's not mandatory for ref types but it's more consistent and performant to do it always.
                returnStatement = new CodeMethodReturnStatement(new CodePrimitiveExpression(null));
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

        private void CheckNillable(CodeStatementCollection getStatements)
        {
            if (IsNillable)
            {
                getStatements.Add(
                    new CodeConditionStatement(
                        new CodeMethodInvokeExpression(
                            new CodeVariableReferenceExpression("x"),
                            "IsXsiNil"),
                        new CodeMethodReturnStatement(new CodePrimitiveExpression(null))
                    )
                );
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

        protected CodeExpression GetFullyQualifiedSimpleTypeClassExpression(string namespacePrefix)
        {
            throw new NotImplementedException();
            // if (namespacePrefix == null) throw new ArgumentNullException(nameof(namespacePrefix));
            // Debug.Assert(this.simpleTypeClrTypeName != null);

            // return CodeDomHelper.CreateFieldReference(
            //     this.simpleTypeClrTypeName, Constants.SimpleTypeDefInnerType);
        }

        protected CodeExpression GetSimpleTypeClassExpression(bool disambiguateWhenPropertyAndTypeNameAreTheSame = false)
        {
            Debug.Assert(this.simpleTypeClrTypeName != null);

            var areTheSameAndShouldDisambiguate = false;
            if (disambiguateWhenPropertyAndTypeNameAreTheSame) {
                if (this.propertyName == this.simpleTypeClrTypeName) {
                    areTheSameAndShouldDisambiguate = true;
                }
            }

            var typeName = areTheSameAndShouldDisambiguate
                ? $"global::{this.settings.GetClrNamespace(PropertyNs)}.{this.simpleTypeClrTypeName}"
                : this.simpleTypeClrTypeName;
            var codeFieldReferenceExpression = CodeDomHelper.CreateFieldReference(typeName, Constants.SimpleTypeDefInnerType);

            #if DEBUG
            var str = codeFieldReferenceExpression.ToCodeString();
            Debug.Assert(str != null);
            #endif

            return codeFieldReferenceExpression;
        }

        public void CreateXNameField(CodeTypeDeclaration typeDecl)
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

        public virtual void InsertDefaultFixedValueInDefaultCtor(CodeConstructor ctor)
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
    }
}