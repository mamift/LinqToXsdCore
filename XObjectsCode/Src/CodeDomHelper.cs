//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.CodeDom;
using System.Reflection;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq.CodeGen
{
    internal static class CodeDomHelper
    {
        public static CodeMethodInvokeExpression XNameGetExpression(string name, string ns)
        {
            return XNameGetExpression(new CodePrimitiveExpression(name), new CodePrimitiveExpression(ns));
        }

        public static CodeMethodInvokeExpression XNameGetExpression(CodeExpression name, CodeExpression ns)
        {
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(Constants.XNameType),
                "Get",
                name,
                ns);
        }

        public static CodeMethodInvokeExpression CreateMethodCallFromField(string fieldName, string methodName,
            params CodeExpression[] parameters)
        {
            return new CodeMethodInvokeExpression(
                CreateFieldReference(null, fieldName),
                methodName,
                parameters);
        }

        public static CodeIndexerExpression CreateIndexerExpression(string target, string key)
        {
            return new CodeIndexerExpression(
                new CodeVariableReferenceExpression(target),
                new CodePrimitiveExpression(key));
        }

        public static CodeTypeDeclaration CreateTypeDeclaration(string clrTypeName, string innerType, 
            GeneratedTypesVisibility generatedTypesVisibility = GeneratedTypesVisibility.Public)
        {
            CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(clrTypeName);
            typeDecl.TypeAttributes = generatedTypesVisibility.ToTypeAttribute();
            typeDecl.IsPartial = true;
            return typeDecl;
        }

        public static CodeAttributeDeclaration SchemaProviderAttribute(string typeName)
        {
            CodeAttributeDeclaration customAtt = new CodeAttributeDeclaration("XmlSchemaProviderAttribute");
            customAtt.Arguments.Add(
                new CodeAttributeArgument(new CodePrimitiveExpression(typeName + "SchemaProvider")));
            return customAtt;
        }

        public static CodeTypeMember CreateStaticMethod(string methodName, string typeT, string typeT1,
            string parameterName, string parameterType, bool useAutoTyping, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeMemberMethod staticMethod = new CodeMemberMethod();
            staticMethod.Name = methodName;
            staticMethod.Attributes = MemberAttributes.Static | visibility.ToMemberAttribute();
            staticMethod.ReturnType = new CodeTypeReference(typeT);

            staticMethod.Parameters.Add(CreateParameter(parameterName, parameterType));
            CodeExpression parameterExp = new CodeVariableReferenceExpression(parameterName);

            if (useAutoTyping)
            {
                staticMethod.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeCastExpression(
                            staticMethod.ReturnType,
                            CreateMethodCall(
                                new CodeTypeReferenceExpression(Constants.XTypedServices),
                                Constants.ToXTypedElement,
                                CreateMethodCall(new CodeTypeReferenceExpression(Constants.XElement),
                                    methodName, parameterExp),
                                CodeDomHelper.SingletonTypeManager()))));
            }
            else
            {
                CodeMethodInvokeExpression methodCall = CreateMethodCall(
                    new CodeTypeReferenceExpression(Constants.XTypedServices),
                    methodName + "<" + GetInnerType(typeT, typeT1) + ">", parameterExp);
                if (typeT1 != null)
                {
                    methodCall.Parameters.Add(CodeDomHelper.SingletonTypeManager());
                }

                staticMethod.Statements.Add(new CodeMethodReturnStatement(methodCall));
            }

            return staticMethod;
        }

        public static CodeTypeMember CreateSave(string paramName, string paramType, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeMemberMethod saveMethod = new CodeMemberMethod();
            saveMethod.Name = "Save";
            saveMethod.Attributes = (saveMethod.Attributes & ~MemberAttributes.AccessMask) | visibility.ToMemberAttribute();

            saveMethod.Parameters.Add(CreateParameter(paramName, paramType));
            saveMethod.Statements.Add(
                CreateMethodCall(
                    new CodeTypeReferenceExpression(Constants.XTypedServices),
                    "Save",
                    new CodeVariableReferenceExpression(paramName),
                    new CodePropertyReferenceExpression(null, Constants.Untyped)));

            return saveMethod;
        }

        public static CodeParameterDeclarationExpression CreateParameter(string paramName, string paramType)
        {
            return new CodeParameterDeclarationExpression(new CodeTypeReference(paramType), paramName);
        }

        public static CodeConstructor CreateConstructor(MemberAttributes memAttributes)
        {
            //Create empty constructor
            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = memAttributes;
            return constructor;
        }

        public static CodeMemberProperty CreateProperty(string propertyName, string propertyType, CodeMemberField field,
            MemberAttributes attributes, bool hasSet)
        {
            //Build simple get set that returns and accepts fieldName
            CodeTypeReference returnType = null;
            if (propertyType != null)
            {
                returnType = new CodeTypeReference(propertyType);
            }
            else
            {
                returnType = field.Type;
            }

            CodeMemberProperty valueProperty = CreateProperty(propertyName, returnType, attributes);
            valueProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    CreateFieldReference(null, field.Name)));

            if (hasSet)
            {
                //Set field = value
                CodeExpression rightExpression = null;
                if (field.Type.BaseType != returnType.BaseType)
                {
                    //cast RHS to field's type
                    rightExpression = new CodeCastExpression(
                        field.Type,
                        SetValue());
                }
                else
                {
                    rightExpression = SetValue();
                }

                valueProperty.SetStatements.Add(
                    new CodeAssignStatement(
                        CreateFieldReference("this", field.Name),
                        rightExpression));
            }

            return valueProperty;
        }

        public static CodeMemberProperty CreateProperty(string propertyName, CodeTypeReference propertyType,
            MemberAttributes attributes)
        {
            CodeMemberProperty clrProperty = new CodeMemberProperty();
            clrProperty.Attributes = attributes;
            clrProperty.HasGet = true;

            clrProperty.Name = propertyName;
            clrProperty.Type = propertyType;
            return clrProperty;
        }

        public static CodeMemberProperty CreateProperty(CodeTypeReference returnType, bool hasSet, 
            MemberAttributes attributes)
        {
            CodeMemberProperty clrProperty = new CodeMemberProperty();
            clrProperty.Attributes = attributes;
            clrProperty.HasGet = true;
            clrProperty.HasSet = hasSet;
            clrProperty.Type = returnType;
            return clrProperty;
        }

        public static CodeMemberProperty CreateInterfaceImplProperty(string propertyName, string interfaceName,
            CodeTypeReference returnType, string fieldName, MemberAttributes attributes = MemberAttributes.Public)
        {
            CodeMemberProperty interfaceProperty = CreateInterfaceImplProperty(propertyName, interfaceName, returnType, attributes);
            interfaceProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeVariableReferenceExpression(fieldName)));
            return interfaceProperty;
        }

        public static CodeMemberProperty CreateInterfaceImplProperty(string propertyName, string interfaceName,
            CodeTypeReference returnType, MemberAttributes attributes = MemberAttributes.Public)
        {
            CodeMemberProperty interfaceProperty = CreateProperty(propertyName, returnType, attributes);
            interfaceProperty.PrivateImplementationType = new CodeTypeReference(interfaceName);
            interfaceProperty.ImplementationTypes.Add(new CodeTypeReference(interfaceName));
            return interfaceProperty;
        }

        public static CodeMemberMethod CreateInterfaceImplMethod(string methodName, string interfaceName,
            MemberAttributes attributes = MemberAttributes.Public)
        {
            CodeMemberMethod interfaceMethod = CreateMethod(methodName, null, attributes);
            CodeTypeReference interfaceType = new CodeTypeReference(interfaceName);
            interfaceMethod.PrivateImplementationType = interfaceType;
            interfaceMethod.ImplementationTypes.Add(interfaceType);
            return interfaceMethod;
        }

        public static CodeMemberProperty CreateTypeManagerProperty(MemberAttributes attributes)
        {
            CodeMemberProperty property = CreateInterfaceImplProperty(Constants.TypeManager, Constants.IXMetaData,
                new CodeTypeReference(Constants.ILinqToXsdTypeManager));
            property.Attributes = attributes;
            property.GetStatements.Add(new CodeMethodReturnStatement(SingletonTypeManager()));
            return property;
        }

        public static CodeMemberProperty CreateSchemaNameProperty(string schemaName, string schemaNs,
            MemberAttributes attributes)
        {
            CodeMemberProperty property = CreateInterfaceImplProperty(Constants.SchemaName, Constants.IXMetaData,
                new CodeTypeReference(Constants.XNameType), attributes);
            property.GetStatements.Add(new CodeMethodReturnStatement(XNameGetExpression(schemaName, schemaNs)));
            return property;
        }

        public static CodeMemberProperty CreateTypeOriginProperty(SchemaOrigin typeOrigin,
            MemberAttributes visibility)
        {
            CodeTypeReference originType = new CodeTypeReference(Constants.Origin);
            CodeMemberProperty property =
                CreateInterfaceImplProperty(Constants.TypeOrigin, Constants.IXMetaData, originType, visibility);
            property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(originType),
                typeOrigin == SchemaOrigin.Element ? "Element" : "Fragment")));
            return property;
        }

        public static CodeMemberMethod CreateMethod(string methodName,
            CodeTypeReference returnType, MemberAttributes methodAttributes)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = methodName;
            method.Attributes = methodAttributes;
            method.ReturnType = returnType;
            return method;
        }


        public static CodeMemberMethod CreateInterfaceImplMethod(string methodName, string interfaceName,
            CodeTypeReference returnType, string fieldName, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeMemberMethod interfaceMethod = CreateMethod(methodName, returnType, visibility.ToMemberAttribute());
            interfaceMethod.PrivateImplementationType = new CodeTypeReference(interfaceName);
            interfaceMethod.ImplementationTypes.Add(new CodeTypeReference(interfaceName));

            interfaceMethod.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeVariableReferenceExpression(fieldName)));
            return interfaceMethod;
        }

        public static CodeMemberMethod CreateInterfaceImplMethod(string methodName, string interfaceName,
            CodeTypeReference returnType, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeMemberMethod interfaceMethod = CreateMethod(methodName, returnType, visibility.ToMemberAttribute());
            interfaceMethod.PrivateImplementationType = new CodeTypeReference(interfaceName);
            interfaceMethod.ImplementationTypes.Add(new CodeTypeReference(interfaceName));
            return interfaceMethod;
        }


        public static CodeMethodInvokeExpression CreateMethodCall(CodeExpression targetOBject, string methodName,
            params CodeExpression[] parameters)
        {
            if (parameters == null)
            {
                return new CodeMethodInvokeExpression(targetOBject, methodName);
            }
            else
            {
                return new CodeMethodInvokeExpression(targetOBject, methodName, parameters);
            }
        }

        public static CodeMethodInvokeExpression CreateGenericMethodCall(CodeExpression targetOBject, string methodName,
            CodeTypeReference typeParam1, params CodeExpression[] parameters)
        {
            return new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(targetOBject, methodName, typeParam1),
                parameters);
        }

        public static CodeMemberField CreateDictionaryField(string dictionaryName, string keyType, string valueType, 
            MemberAttributes attributes)
        {
            CodeMemberField staticDictionary =
                new CodeMemberField(CreateDictionaryType(keyType, valueType), dictionaryName);
            staticDictionary.Attributes = MemberAttributes.Static | attributes;
            staticDictionary.InitExpression = new CodeObjectCreateExpression(CreateDictionaryType(keyType, valueType));
            return staticDictionary;
        }

        public static CodeMemberField CreateMemberField(string memberName, string typeName,
            bool init, MemberAttributes attributes)
        {
            CodeMemberField field = new CodeMemberField(typeName, memberName);
            AddBrowseNever(field);
            field.Attributes = attributes;
            if (init)
            {
                field.InitExpression = new CodeObjectCreateExpression(typeName);
            }

            return field;
        }

        public static CodeMemberField CreateGenericMemberField(string memberName,
            string typeName,
            string[] typeStrParams,
            MemberAttributes attributes,
            bool init)
        {
            CodeTypeReference[] typeParams = new CodeTypeReference[typeStrParams.Length];
            int index = 0;
            foreach (string str in typeStrParams)
            {
                typeParams[index++] = new CodeTypeReference(str);
            }

            return CreateGenericMemberField(memberName, typeName, typeParams, attributes, init);
        }


        public static CodeMemberField CreateGenericMemberField(string memberName,
            string typeName,
            CodeTypeReference[] typeParams,
            MemberAttributes attributes,
            bool init)
        {
            CodeTypeReference typeRef = new CodeTypeReference(typeName, typeParams);
            CodeMemberField field = new CodeMemberField(typeRef, memberName);
            AddBrowseNever(field);
            field.Attributes = attributes;
            if (init)
            {
                field.InitExpression = new CodeObjectCreateExpression(typeRef);
            }

            return field;
        }

        public static CodeTypeOfExpression Typeof(string typeName)
        {
            return new CodeTypeOfExpression(typeName);
        }

        public static CodeThisReferenceExpression This()
        {
            return new CodeThisReferenceExpression();
        }

        public static CodePropertySetValueReferenceExpression SetValue()
        {
            return new CodePropertySetValueReferenceExpression();
        }

        public static CodeTypeReference CreateDictionaryType(string keyType, string valueType)
        {
            return new CodeTypeReference("Dictionary", new CodeTypeReference(keyType),
                new CodeTypeReference(valueType));
        }

        public static CodeTypeReference CreateTypeReference(string type)
        {
            return new CodeTypeReference(type);
        }

        public static CodeTypeReferenceExpression CreateTypeReferenceExp(string type)
        {
            return new CodeTypeReferenceExpression(type);
        }

        public static CodeTypeReference CreateGenericTypeReference(string type, string[] typeStrParams)
        {
            CodeTypeReference[] typeParams = new CodeTypeReference[typeStrParams.Length];
            int index = 0;
            foreach (string str in typeStrParams)
            {
                typeParams[index++] = new CodeTypeReference(str);
            }

            return CreateGenericTypeReference(type, typeParams);
        }

        public static CodeTypeReference CreateGenericTypeReference(string type, CodeTypeReference[] typeArgs)
        {
            return new CodeTypeReference(type, typeArgs);
        }

        public static CodeVariableDeclarationStatement CreateCastToInterface(string interfaceName, string variableName,
            string propertyToCast)
        {
            return new CodeVariableDeclarationStatement(
                interfaceName,
                variableName,
                new CodeCastExpression(
                    interfaceName,
                    new CodePropertyReferenceExpression(
                        new CodeThisReferenceExpression(),
                        propertyToCast)));
        }

        public static CodePropertyReferenceExpression SingletonTypeManager()
        {
            return new CodePropertyReferenceExpression(
                new CodeTypeReferenceExpression(NameGenerator.GetServicesClassName()), Constants.TypeManagerInstance);
        }


        public static CodeTypeMember AddBrowseNever(CodeTypeMember member)
        {
            CodeAttributeDeclaration browsableNever = new CodeAttributeDeclaration("DebuggerBrowsable",
                new CodeAttributeArgument(CreateFieldReference("DebuggerBrowsableState", "Never")));

            if (member.CustomAttributes == null)
            {
                member.CustomAttributes = new CodeAttributeDeclarationCollection();
            }

            member.CustomAttributes.Add(browsableNever);

            return member;
        }

        public static CodeFieldReferenceExpression CreateFieldReference(string typeName, string fieldName)
        {
            CodeExpression targetObject = null;
            if (typeName == "this")
            {
                targetObject = new CodeThisReferenceExpression();
            }
            else if (typeName != null)
            {
                targetObject = new CodeTypeReferenceExpression(typeName);
            }

            return new CodeFieldReferenceExpression(targetObject, fieldName);
        }

        public static string CreateGenericMethodName(string methodName, string typeName)
        {
            return String.Concat(methodName, "<", typeName, ">");
        }

        public static CodeSnippetTypeMember CreateCast(string typeT, string typeT1, bool useAutoTyping, string @namespace = "",
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeSnippetTypeMember castMember = new CodeSnippetTypeMember();
            @namespace = @namespace.IsNotEmpty() ? $"{@namespace}." : "";
            var visibilityKeyword = visibility.ToKeyword();
            var servicesClassName = @namespace + NameGenerator.GetServicesClassName();
            if (useAutoTyping)
            {
                castMember.Text = String.Concat($"\t\t{visibilityKeyword} static explicit operator ", typeT, "(XElement xe) {  ",
                    "return (", typeT, ")", Constants.XTypedServices, ".ToXTypedElement(xe,",
                    servicesClassName, ".Instance as ILinqToXsdTypeManager); }");
            }
            else
            {
                castMember.Text = String.Concat($"\t\t{visibilityKeyword} static explicit operator ", typeT,
                    "(XElement xe) { return ", Constants.XTypedServices, ".ToXTypedElement<",
                    GetInnerType(typeT, typeT1), ">(xe,", servicesClassName,
                    ".Instance as ILinqToXsdTypeManager); }");
            }

            return castMember;
        }


        public static CodeSnippetTypeMember CreateXRootGetter(string typeName, string fqTypeName, LocalSymbolTable lst, 
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            string symbolName = lst.AddMember(typeName);
            CodeSnippetTypeMember castMember = new CodeSnippetTypeMember();
            
            castMember.Text = String.Concat("\r\n", $"\t\t{visibility.ToKeyword()} ", fqTypeName, " ", symbolName, " {  get {",
                "return rootObject as ", fqTypeName, "; } }");
            return castMember;
        }

        public static CodeMemberMethod CreateXRootMethod(string returnType, string methodName, string[][] paramList, 
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeTypeReference xRootType = new CodeTypeReference(returnType);

            CodeMemberMethod staticMethod = new CodeMemberMethod();
            staticMethod.Name = methodName;
            staticMethod.Attributes = visibility.ToMemberAttribute() | MemberAttributes.Static;
            staticMethod.ReturnType = xRootType;
            CodeExpression[] parameterExp = new CodeExpression[paramList.Length];

            for (int i = 0; i < paramList.Length; i++)
            {
                string[] paramRef = paramList[i];
                // index 0 is the type name and index 1 is the parameter name
                staticMethod.Parameters.Add(CreateParameter(paramRef[1], paramRef[0]));
                parameterExp[i] = new CodeVariableReferenceExpression(paramRef[1]);
            }

            CodeExpression rootExp = new CodeVariableReferenceExpression("root");
            CodeExpression doc = new CodeFieldReferenceExpression(rootExp, "doc");

            staticMethod.Statements.Add( //XRoot root = new XRoot;
                new CodeVariableDeclarationStatement(xRootType, "root",
                    new CodeObjectCreateExpression(xRootType)));

            staticMethod.Statements.Add( //root.doc = XDocument.Load(xmlFile);
                new CodeAssignStatement(
                    doc,
                    CreateMethodCall(new CodeTypeReferenceExpression("XDocument"), methodName,
                        parameterExp)));

            staticMethod.Statements.Add( //XTypedElement typedRoot = XTypedServices.ToXTypedElement(....)
                new CodeVariableDeclarationStatement(
                    Constants.XTypedElement,
                    "typedRoot",
                    CreateMethodCall(
                        new CodeTypeReferenceExpression(Constants.XTypedServices),
                        Constants.ToXTypedElement,
                        new CodePropertyReferenceExpression(doc, "Root"),
                        CodeDomHelper.SingletonTypeManager())));

            staticMethod.Statements.Add( //if(typedRoot == null)
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(
                        new CodeVariableReferenceExpression("typedRoot"),
                        CodeBinaryOperatorType.IdentityEquality,
                        new CodePrimitiveExpression(null)
                    ),
                    new CodeThrowExceptionStatement(
                        new CodeObjectCreateExpression(Constants.LinqToXsdException,
                            new CodePrimitiveExpression("Invalid root element in xml document."))
                    )));

            staticMethod.Statements.Add( //root.rootObject = typedRoot
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(rootExp, "rootObject"),
                    new CodeVariableReferenceExpression("typedRoot")));

            staticMethod.Statements.Add( //return root;
                new CodeMethodReturnStatement(rootExp));

            return staticMethod;
        }

        public static CodeMemberMethod CreateXRootSave(string[][] paramList, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeMemberMethod staticMethod = new CodeMemberMethod();
            staticMethod.Name = "Save";
            staticMethod.Attributes = visibility.ToMemberAttribute();
            CodeExpression[] parameterExp = new CodeExpression[paramList.Length];

            for (int i = 0; i < paramList.Length; i++)
            {
                string[] paramRef = paramList[i];
                // index 0 is the type name and index 1 is the parameter name
                staticMethod.Parameters.Add(CreateParameter(paramRef[1], paramRef[0]));
                parameterExp[i] = new CodeVariableReferenceExpression(paramRef[1]);
            }

            CodeExpression doc = new CodeVariableReferenceExpression("doc");

            staticMethod.Statements.Add( //root.doc = XDocument.Save(...);
                CreateMethodCall(doc, "Save", parameterExp));

            return staticMethod;
        }


        public static CodeConstructor CreateXRootFunctionalConstructor(string typeName, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            CodeConstructor constructor = CodeDomHelper.CreateConstructor(visibility.ToMemberAttribute());
            constructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeName), "root"));

            constructor.Statements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(This(), "doc"),
                    new CodeObjectCreateExpression(
                        "XDocument",
                        new CodePropertyReferenceExpression(
                            new CodeVariableReferenceExpression("root"),
                            Constants.Untyped))));

            constructor.Statements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(This(), "rootObject"),
                    new CodeVariableReferenceExpression("root")));

            return constructor;
        }

        public static string GetInnerType(string wrappingType, string wrappedType)
        {
            if (wrappedType == null)
            {
                return wrappingType;
            }

            return string.Concat(wrappingType, ", ", wrappedType);
        }
    }
}