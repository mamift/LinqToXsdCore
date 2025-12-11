//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.CodeDom;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq.CodeGen
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal abstract class TypeBuilder
    {
        protected CodeTypeDeclaration decl;
        protected ClrTypeInfo clrTypeInfo;

        protected StateNameSource fsmNameSource;
        // this type is reused. Be sure to clear any state in Init();

        static CodeMemberMethod defaultContentModel;
        
        protected LinqToXsdSettings Settings { get; set; }

        protected GeneratedTypesVisibility DefaultVisibility
        {
            get
            {
                var typeNamespace = clrTypeInfo?.clrtypeNs ?? throw new InvalidOperationException();
                return Settings.NamespaceTypesVisibilityMap.ValueForKey(typeNamespace);
            }
        }

        protected TypeBuilder(LinqToXsdSettings settings)
        {
            Settings = settings;
        }

        internal CodeTypeDeclaration TypeDeclaration
        {
            get { return decl; }
        }

        internal virtual void CreateDefaultConstructor(List<ClrAnnotation> annotations)
        {
            decl.Members.Add(ApplyAnnotations(CodeDomHelper.CreateConstructor(DefaultVisibility.ToMemberAttribute()), annotations,
                null));
        }

        internal virtual CodeConstructor CreateFunctionalConstructor(List<ClrAnnotation> annotations)
        {
            throw new InvalidOperationException();
        }


        internal virtual void CreateFunctionalConstructor(ClrBasePropertyInfo propertyInfo,
            List<ClrAnnotation> annotations)
        {
            throw new InvalidOperationException();
        }

        internal virtual void CreateStaticConstructor()
        {
            throw new InvalidOperationException();
        }

        internal virtual void CreateAttributeProperty(ClrBasePropertyInfo propertyInfo, List<ClrAnnotation> annotations)
        {
            throw new InvalidOperationException();
        }

        internal virtual void StartGrouping(GroupingInfo grouping)
        {
            throw new InvalidOperationException();
        }

        internal virtual void EndGrouping()
        {
            throw new InvalidOperationException();
        }

        internal virtual void CreateProperty(ClrBasePropertyInfo propertyInfo, List<ClrAnnotation> annotations)
        {
            throw new InvalidOperationException();
        }

        protected virtual void SetElementWildCardFlag(bool hasAny)
        {
            //Do nothing by default
        }

        internal void AddTypeToTypeManager(CodeStatementCollection dictionaryStatements, string dictionaryName)
        {
            string typeRef = "global::" + clrTypeInfo.clrFullTypeName;
            dictionaryStatements.Add(CodeDomHelper.CreateMethodCallFromField(dictionaryName, "Add",
                CodeDomHelper.XNameGetExpression(clrTypeInfo.schemaName, clrTypeInfo.schemaNs),
                CodeDomHelper.Typeof(typeRef)));
        }

        internal virtual void ImplementInterfaces(bool enableServiceReference)
        {
            ImplementIXMetaData();
            if (enableServiceReference)
            {
                ImplementIXmlSerializable();
            }
        }

        protected void InnerInit()
        {
            decl = null;
            clrTypeInfo = null;
        }

        internal virtual void Init()
        {
            InnerInit();
        }

        protected virtual void AddBaseType()
        {
            //Set basetype
            string baseTypeClrName = clrTypeInfo.baseTypeClrName;

            if (baseTypeClrName != null)
            {
                string baseTypeClrNs = clrTypeInfo.baseTypeClrNs;
                string baseTypeRef;
                if (baseTypeClrNs.IsNotEmpty())
                    baseTypeRef = "global::" + baseTypeClrNs + "." + baseTypeClrName;
                else
                    baseTypeRef = "global::" + baseTypeClrName;
                decl.BaseTypes.Add(baseTypeRef);
            }
            else
            {
                decl.BaseTypes.Add(Constants.XTypedElement);
            }
        }

        protected virtual void ImplementContentModelMetaData()
        {
            decl.Members.Add(DefaultContentModel());
        }

        protected virtual string InnerType
        {
            get { return null; }
        }

        internal void CreateTypeDeclaration(ClrTypeInfo clrTypeInfo)
        {
            this.clrTypeInfo = clrTypeInfo;
            SetElementWildCardFlag(clrTypeInfo.HasElementWildCard);

            string schemaName = clrTypeInfo.schemaName;
            string schemaNs = clrTypeInfo.schemaNs;
            string clrTypeName = clrTypeInfo.clrtypeName;
            SchemaOrigin typeOrigin = clrTypeInfo.typeOrigin;

            CodeTypeDeclaration typeDecl = CodeDomHelper.CreateTypeDeclaration(clrTypeName, InnerType, DefaultVisibility);

            if (clrTypeInfo.IsAbstract)
            {
                typeDecl.TypeAttributes |= TypeAttributes.Abstract;
            }
            else if (clrTypeInfo.IsSealed)
            {
                typeDecl.TypeAttributes |= TypeAttributes.Sealed;
            }

            decl = typeDecl;

            AddBaseType();
            CreateServicesMembers();
            CreateDefaultConstructor(clrTypeInfo.Annotations);
        }

        internal void CreateServicesMembers()
        {
            string innerType = InnerType;
            string clrTypeName = clrTypeInfo.clrtypeName;

            bool useAutoTyping = clrTypeInfo.IsAbstract || clrTypeInfo.IsSubstitutionHead;
            if (clrTypeInfo.typeOrigin == SchemaOrigin.Element)
            {
                //Disable load and parse for complex types
                CodeTypeMember load = CodeDomHelper.CreateStaticMethod(
                    "Load", clrTypeName, innerType, "xmlFile", "System.String", useAutoTyping, DefaultVisibility);
                // http://linqtoxsd.codeplex.com/WorkItem/View.aspx?WorkItemId=4093
                var loadReader = CodeDomHelper.CreateStaticMethod(
                    "Load", clrTypeName, innerType, "xmlFile", "System.IO.TextReader", useAutoTyping, DefaultVisibility);
                CodeTypeMember parse = CodeDomHelper.CreateStaticMethod("Parse", clrTypeName, innerType, "xml",
                    "System.String", useAutoTyping, DefaultVisibility);
                if (clrTypeInfo.IsDerived)
                {
                    load.Attributes |= MemberAttributes.New;
                    parse.Attributes |= MemberAttributes.New;
                }
                else
                {
                    decl.Members.Add(CodeDomHelper.CreateSave("xmlFile", "System.String", DefaultVisibility));
                    decl.Members.Add(CodeDomHelper.CreateSave("tw", "System.IO.TextWriter", DefaultVisibility));
                    decl.Members.Add(CodeDomHelper.CreateSave("xmlWriter", "System.Xml.XmlWriter", DefaultVisibility));
                }

                decl.Members.Add(load);
                decl.Members.Add(loadReader);
                decl.Members.Add(parse);
            }

            CodeTypeMember cast = CodeDomHelper.CreateCast(clrTypeName, innerType, useAutoTyping); // dont pass default visibility; as operators must be public and static
            decl.Members.Add(cast);

            if (!clrTypeInfo.IsAbstract)
            {
                //Add Clone for non-abstract types
                CodeMemberMethod clone = CodeDomHelper.CreateMethod("Clone",
                    new CodeTypeReference(Constants.XTypedElement), MemberAttributes.Public | MemberAttributes.Override);
                if (innerType == null)
                {
                    CodeMethodInvokeExpression callClone = CodeDomHelper.CreateMethodCall(
                        new CodeTypeReferenceExpression(Constants.XTypedServices),
                        "CloneXTypedElement<" + clrTypeName + ">", new CodeThisReferenceExpression());
                    clone.Statements.Add(new CodeMethodReturnStatement(callClone));
                }
                else
                {
                    CodeMethodInvokeExpression callClone = CodeDomHelper.CreateMethodCall(
                        new CodePropertyReferenceExpression(CodeDomHelper.This(), Constants.CInnerTypePropertyName),
                        "Clone");
                    clone.Statements.Add(
                        new CodeMethodReturnStatement(
                            new CodeObjectCreateExpression(
                                clrTypeName,
                                new CodeCastExpression(
                                    new CodeTypeReference(innerType),
                                    callClone))));
                }

                decl.Members.Add(clone);
            }
        }

        protected virtual void ImplementCommonIXMetaData()
        {
            //Do nothing, this will inherit the LocalElementDictionary from XTypedElement which returns empty dict and Content which returns null
        }

        public CodeMemberProperty CreateSchemaNameProperty(string schemaName, string schemaNs, MemberAttributes attributes)
        {
            // HACK: CodeDom doesn't model readonly fields... but it doesn't check the type either!
            var field = new CodeMemberField("readonly System.Xml.Linq.XName", "xName")
            {
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                InitExpression = CodeDomHelper.XNameGetExpression(schemaName, schemaNs),
            };
            decl.Members.Add(field);

            CodeMemberProperty property = CodeDomHelper.CreateInterfaceImplProperty(Constants.SchemaName, Constants.IXMetaData,
                new CodeTypeReference(Constants.XNameType), attributes);
            property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, "xName")));
            return property;
        }

        private void ImplementIXMetaData()
        {
            string interfaceName = Constants.IXMetaData;

            CodeMemberProperty schemaNameProperty =
                CreateSchemaNameProperty(clrTypeInfo.schemaName, clrTypeInfo.schemaNs, DefaultVisibility.ToMemberAttribute());

            ImplementCommonIXMetaData();
            if (clrTypeInfo.HasElementWildCard) ImplementFSMMetaData();
            else ImplementContentModelMetaData();


            CodeMemberProperty typeOriginProperty = 
                CodeDomHelper.CreateTypeOriginProperty(clrTypeInfo.typeOrigin, DefaultVisibility.ToMemberAttribute());

            CodeDomHelper.AddBrowseNever(schemaNameProperty);
            CodeDomHelper.AddBrowseNever(typeOriginProperty);

            decl.Members.Add(schemaNameProperty);
            decl.Members.Add(typeOriginProperty);
            var typeManagerProperty = CodeDomHelper.CreateTypeManagerProperty(DefaultVisibility.ToMemberAttribute());
            typeManagerProperty.Attributes = MemberAttributes.FamilyOrAssembly;
            decl.Members.Add(CodeDomHelper.AddBrowseNever(typeManagerProperty));
            decl.BaseTypes.Add(interfaceName);
        }

        private void ImplementIXmlSerializable()
        {
            string interfaceName = Constants.IXmlSerializable;
            string typeManagerName = NameGenerator.GetServicesClassName();
            string methodName = clrTypeInfo.clrtypeName + "SchemaProvider";
            CodeMemberMethod schemaProviderMethod =
                CodeDomHelper.CreateMethod(methodName, null, DefaultVisibility.ToMemberAttribute() | MemberAttributes.Static);

            schemaProviderMethod.Parameters.Add(new CodeParameterDeclarationExpression("XmlSchemaSet", "schemas"));
            schemaProviderMethod.Statements.Add(
                //LinqtoXsdTypeManager.AddSchemas(schemas)
                CodeDomHelper.CreateMethodCall(new CodeTypeReferenceExpression(typeManagerName),
                    "AddSchemas", new CodeVariableReferenceExpression("schemas")));

            CodeExpression qNameExp = new CodeObjectCreateExpression("XmlQualifiedName",
                new CodePrimitiveExpression(clrTypeInfo.schemaName), new CodePrimitiveExpression(clrTypeInfo.schemaNs));

            if (clrTypeInfo.typeOrigin == SchemaOrigin.Element)
            {
                schemaProviderMethod.Statements.Add(
                    //XmlSchemaElement element = (XmlSchemaElement)schemas.GlobalElements[new XmlQualifiedName("orders", "http://tempuri/Orders.org")];
                    new CodeVariableDeclarationStatement("XmlSchemaElement", "element",
                        new CodeCastExpression("XmlSchemaElement",
                            new CodeIndexerExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("schemas"),
                                    "GlobalElements"), qNameExp))));

                //if(element != null) { return element.ElementSchemaType; } else { return null;}
                schemaProviderMethod.Statements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodeVariableReferenceExpression("element"),
                            CodeBinaryOperatorType.IdentityInequality,
                            new CodePrimitiveExpression(null)),
                        new CodeMethodReturnStatement(
                            new CodePropertyReferenceExpression(
                                new CodeVariableReferenceExpression("element"),
                                "ElementSchemaType"))));

                schemaProviderMethod.Statements.Add(
                    new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));

                schemaProviderMethod.ReturnType = new CodeTypeReference("XmlSchemaType");
            }
            else
            {
                schemaProviderMethod.ReturnType = new CodeTypeReference("XmlQualifiedName");
                schemaProviderMethod.Statements.Add(
                    new CodeMethodReturnStatement(qNameExp));
            }

            decl.CustomAttributes.Add(CodeDomHelper.SchemaProviderAttribute(clrTypeInfo.clrtypeName));
            decl.BaseTypes.Add(interfaceName);
            decl.Members.Add(schemaProviderMethod);
        }

        protected virtual void ImplementFSMMetaData()
        {
            //Do nothing.
        }

        protected static CodeMemberMethod DefaultContentModel(GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            if (defaultContentModel == null)
            {
                CodeTypeReference cmType = new CodeTypeReference(Constants.ContentModelType);
                CodeMemberMethod getContentModelMethod =
                    CodeDomHelper.CreateInterfaceImplMethod(Constants.GetContentModel, Constants.IXMetaData, cmType, visibility);
                getContentModelMethod.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                            new CodeTypeReferenceExpression(Constants.ContentModelType),
                            Constants.Default)));
                Interlocked.CompareExchange<CodeMemberMethod>(ref defaultContentModel, getContentModelMethod, null);
            }

            return defaultContentModel;
        }

        internal static CodeTypeDeclaration CreateSimpleType(ClrSimpleTypeInfo typeInfo,
            Dictionary<XmlSchemaObject, string> nameMappings,
            LinqToXsdSettings settings)
        {
            string typeName = typeInfo is EnumSimpleTypeInfo ? typeInfo.clrtypeName + Constants.EnumValidator : typeInfo.clrtypeName;
            var simpleTypeDecl = new CodeTypeDeclaration(typeName);
            var typeVisibility = settings.NamespaceTypesVisibilityMap.ValueForKey(typeInfo.clrtypeNs).ToTypeAttribute();
            simpleTypeDecl.TypeAttributes = TypeAttributes.Sealed | typeVisibility;
            //simpleTypeDecl.TypeAttributes = TypeAttributes.Sealed | TypeAttributes.NestedAssembly;

            //Add private constructor so it cannot be instantiated
            var privateConst = new CodeConstructor { Attributes = MemberAttributes.Private };
            simpleTypeDecl.Members.Add(privateConst);

            //Create a static field for the XTypedSchemaSimpleType
            var memberVisibility = settings.NamespaceTypesVisibilityMap.ValueForKey(typeInfo.clrtypeNs).ToMemberAttribute();
            CodeMemberField typeField =
                CodeDomHelper.CreateMemberField(Constants.SimpleTypeDefInnerType, Constants.SimpleTypeValidator, false, memberVisibility | MemberAttributes.Static);
            typeField.InitExpression =
                SimpleTypeCodeDomHelper.CreateSimpleTypeDef(typeInfo, nameMappings, settings, false);

            simpleTypeDecl.Members.Add(typeField);

            // inconsistency w/ the wasy ApplyAnnotations are us
            ApplyAnnotations(simpleTypeDecl, typeInfo);

            return simpleTypeDecl;
        }

        internal static CodeTypeDeclaration CreateEnumType(EnumSimpleTypeInfo typeInfo,
            LinqToXsdSettings settings, ClrTypeInfo clrTypeInfo = null)
        {
            string typeName = typeInfo.clrtypeName;

            var enumTypeDecl = new CodeTypeDeclaration(typeName) { IsEnum = true };
            var typeVisibility = settings.NamespaceTypesVisibilityMap.ValueForKey(typeInfo.clrtypeNs).ToTypeAttribute();
            enumTypeDecl.TypeAttributes = TypeAttributes.Sealed | typeVisibility;
            foreach (var facet in typeInfo.InnerType.GetEnumFacets())
            {
                enumTypeDecl.Members.Add(new CodeMemberField(typeName, facet.Member));
            }

            if (clrTypeInfo != null)
                enumTypeDecl.UserData[nameof(ClrTypeInfo)] = clrTypeInfo;

            ApplyAnnotations(enumTypeDecl, typeInfo);

            return enumTypeDecl;
        }

        internal void ApplyAnnotations(ClrTypeInfo typeInfo)
        {
            ApplyAnnotations(decl, typeInfo);
        }

        internal static void ApplyAnnotations(CodeMemberProperty propDecl, ClrBasePropertyInfo propInfo,
            List<ClrAnnotation> typeAnnotations)
        {
            ApplyAnnotations(propDecl, propInfo.Annotations, typeAnnotations);
        }

        internal static void ApplyAnnotations(CodeTypeMember typeDecl, ClrTypeInfo typeInfo)
        {
            ApplyAnnotations(typeDecl, typeInfo.Annotations, null);
        }

        internal static CodeTypeMember ApplyAnnotations(CodeTypeMember typeDecl, List<ClrAnnotation> annotations,
            List<ClrAnnotation> typeAnnotations)
        {
            bool fSummaryOpened = false;

            if (annotations != null)
            {
                // Do summary tags
                foreach (ClrAnnotation ann in annotations)
                {
                    if (!fSummaryOpened)
                    {
                        typeDecl.Comments.Add(new CodeCommentStatement("<summary>", true));
                        fSummaryOpened = true;
                    }

                    typeDecl.Comments.Add(new CodeCommentStatement("<para>", true));
                    typeDecl.Comments.Add(new CodeCommentStatement(ann.Text, true));
                    typeDecl.Comments.Add(new CodeCommentStatement("</para>", true));
                }
            }

            // Append any inherited annotations
            if (typeAnnotations != null)
            {
                // Do summary tags
                foreach (ClrAnnotation ann in typeAnnotations)
                {
                    // if no filter has been specified, then put everything in the statements
                    // otherwise only put the section requested
                    if (ann.Section == "summaryRegEx")
                    {
                        if (!fSummaryOpened)
                        {
                            typeDecl.Comments.Add(new CodeCommentStatement("<summary>", true));
                            fSummaryOpened = true;
                        }

                        typeDecl.Comments.Add(new CodeCommentStatement("<para>", true));
                        typeDecl.Comments.Add(new CodeCommentStatement(ann.Text, true));
                        typeDecl.Comments.Add(new CodeCommentStatement("</para>", true));
                    }
                }
            }

            // if summary was opened, then it needs to be closed
            if (fSummaryOpened)
            {
                typeDecl.Comments.Add(new CodeCommentStatement("</summary>", true));
            }

            return typeDecl;
        }

        internal static CodeTypeDeclaration CreateTypeManager(XmlQualifiedName rootElementName,
            bool enableServiceReference,
            CodeStatementCollection typeDictionaryStatements,
            CodeStatementCollection elementDictionaryStatements,
            CodeStatementCollection wrapperDictionaryStatements,
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            //Create the services type class and add members
            string servicesClassName = NameGenerator.GetServicesClassName();
            var memberVisibility = visibility.ToMemberAttribute();
            CodeTypeDeclaration servicesTypeDecl = new CodeTypeDeclaration(servicesClassName) {
                TypeAttributes = visibility.ToTypeAttribute()
            };

            //Create singleton
            CodeMemberField singletonField = CodeDomHelper.CreateMemberField(Constants.TypeManagerSingletonField,
                servicesClassName, true, MemberAttributes.Static | MemberAttributes.Private);
            CodeMemberProperty singletonProperty = CodeDomHelper.CreateProperty(Constants.TypeManagerInstance, null,
                singletonField, MemberAttributes.Static | memberVisibility, false);

            MemberAttributes privateStatic = MemberAttributes.Private | MemberAttributes.Static;
            //Create static constructor
            CodeTypeConstructor staticServicesConstructor = new CodeTypeConstructor();

            CodeTypeReference returnType = CodeDomHelper.CreateDictionaryType(Constants.XNameType, Constants.SystemTypeName);
            CodeTypeReference wrapperReturnType = CodeDomHelper.CreateDictionaryType(Constants.SystemTypeName, Constants.SystemTypeName);

            //Add private constructor so it cannot be instantiated
            var privateConst = new CodeConstructor { Attributes = MemberAttributes.Private };
            servicesTypeDecl.Members.Add(privateConst);

            //Create a dictionary of TypeName vs System.Type and the method to create it
            CodeMemberProperty typeDictProperty = null;
            if (typeDictionaryStatements.Count > 0)
            {
                typeDictProperty = CodeDomHelper.CreateInterfaceImplProperty(Constants.GlobalTypeDictionary,
                    Constants.ILinqToXsdTypeManager, returnType, Constants.TypeDictionaryField, MemberAttributes.Private);

                CodeMemberField staticTypeDictionary =
                    CodeDomHelper.CreateDictionaryField(Constants.TypeDictionaryField, Constants.XNameType, Constants.SystemTypeName,
                        MemberAttributes.Private);
                CodeMemberMethod buildTypeDictionary =
                    CodeDomHelper.CreateMethod(Constants.BuildTypeDictionary, null, privateStatic);
                buildTypeDictionary.Statements.AddRange(typeDictionaryStatements);

                staticServicesConstructor.Statements.Add(
                    CodeDomHelper.CreateMethodCall(null, Constants.BuildTypeDictionary));
                servicesTypeDecl.Members.Add(staticTypeDictionary);
                servicesTypeDecl.Members.Add(buildTypeDictionary);
            }
            else
            {
                typeDictProperty = CodeDomHelper.CreateInterfaceImplProperty(Constants.GlobalTypeDictionary,
                    Constants.ILinqToXsdTypeManager, returnType, MemberAttributes.Private);
                typeDictProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                            new CodeTypeReferenceExpression(Constants.XTypedServices),
                            Constants.EmptyDictionaryField)));
            }

            //Create a dictionary of ElementName Vs System.Type - For Auto typing and substitutionGroups
            CodeMemberProperty elementDictProperty = null;
            if (elementDictionaryStatements.Count > 0)
            {
                elementDictProperty = CodeDomHelper.CreateInterfaceImplProperty(Constants.GlobalElementDictionary,
                    Constants.ILinqToXsdTypeManager, returnType, Constants.ElementDictionaryField, MemberAttributes.Private);

                CodeMemberField staticElementDictionary =
                    CodeDomHelper.CreateDictionaryField(Constants.ElementDictionaryField, Constants.XNameType, Constants.SystemTypeName,
                        MemberAttributes.Private);
                CodeMemberMethod buildElementDictionary =
                    CodeDomHelper.CreateMethod(Constants.BuildElementDictionary, null, privateStatic);
                buildElementDictionary.Statements.AddRange(elementDictionaryStatements);

                staticServicesConstructor.Statements.Add(
                    CodeDomHelper.CreateMethodCall(null, Constants.BuildElementDictionary));
                servicesTypeDecl.Members.Add(staticElementDictionary);
                servicesTypeDecl.Members.Add(buildElementDictionary);
            }
            else
            {
                elementDictProperty = CodeDomHelper.CreateInterfaceImplProperty(Constants.GlobalElementDictionary,
                    Constants.ILinqToXsdTypeManager, returnType, MemberAttributes.Private);
                elementDictProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                            new CodeTypeReferenceExpression(Constants.XTypedServices),
                            Constants.EmptyDictionaryField)));
            }

            //Create a dictionary of Wrapper Element Type Vs Wrapper Type - For Auto typing when casting from XElement to Type
            CodeMemberProperty wrapperDictProperty = null;
            if (wrapperDictionaryStatements.Count > 0)
            {
                wrapperDictProperty = CodeDomHelper.CreateInterfaceImplProperty(Constants.RootContentTypeMapping,
                    Constants.ILinqToXsdTypeManager, wrapperReturnType, Constants.WrapperDictionaryField);

                CodeMemberField staticWrapperDictionary =
                    CodeDomHelper.CreateDictionaryField(Constants.WrapperDictionaryField, Constants.SystemTypeName, Constants.SystemTypeName,
                        MemberAttributes.Private);
                CodeMemberMethod buildWrapperDictionary =
                    CodeDomHelper.CreateMethod(Constants.BuildWrapperDictionary, null, privateStatic);
                buildWrapperDictionary.Statements.AddRange(wrapperDictionaryStatements);

                staticServicesConstructor.Statements.Add(
                    CodeDomHelper.CreateMethodCall(null, Constants.BuildWrapperDictionary));
                servicesTypeDecl.Members.Add(staticWrapperDictionary);
                servicesTypeDecl.Members.Add(buildWrapperDictionary);
            }
            else
            {
                wrapperDictProperty = CodeDomHelper.CreateInterfaceImplProperty(Constants.RootContentTypeMapping,
                    Constants.ILinqToXsdTypeManager, wrapperReturnType);
                wrapperDictProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                            new CodeTypeReferenceExpression(Constants.XTypedServices),
                            Constants.EmptyTypeMappingDictionary)));
            }

            //Implement IXmlSerializable AddSchemas method for the XmlSchemaProvider method and Schemas get set property for runtime access to schemas
            //if (enableServiceReference) { //Since property is on the interface, it has to be implemented;
            string schemaSetFieldName = "schemaSet";
            CodeTypeReference schemaSetType = new CodeTypeReference("XmlSchemaSet");

            CodeMemberField schemaSetField = new CodeMemberField(schemaSetType, schemaSetFieldName);
            schemaSetField.Attributes = MemberAttributes.Private | MemberAttributes.Static;

            //AddSchemas method
            CodeMemberMethod addSchemasMethod = CodeDomHelper.CreateMethod("AddSchemas", null, MemberAttributes.FamilyOrAssembly | MemberAttributes.Static);
            addSchemasMethod.Parameters.Add(new CodeParameterDeclarationExpression("XmlSchemaSet", "schemas"));
            //schemas.Add(schemaSet);
            addSchemasMethod.Statements.Add(CodeDomHelper.CreateMethodCall(
                new CodeVariableReferenceExpression("schemas"), "Add",
                new CodeFieldReferenceExpression(null, schemaSetFieldName)));


            CodeTypeReferenceExpression interLockedType =
                new CodeTypeReferenceExpression("System.Threading.Interlocked");

            CodeMemberProperty schemaSetProperty =
                CodeDomHelper.CreateInterfaceImplProperty("Schemas", Constants.ILinqToXsdTypeManager, schemaSetType, memberVisibility);
            CodeFieldReferenceExpression schemaSetFieldRef = new CodeFieldReferenceExpression(null, schemaSetFieldName);

            CodeDirectionExpression schemaSetParam = new CodeDirectionExpression(FieldDirection.Ref, schemaSetFieldRef);

            schemaSetProperty.GetStatements.Add(
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(schemaSetFieldRef,
                        CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)),
                    new CodeVariableDeclarationStatement(schemaSetType, "tempSet",
                        new CodeObjectCreateExpression(schemaSetType)),
                    new CodeExpressionStatement(
                        CodeDomHelper.CreateMethodCall(interLockedType, "CompareExchange",
                            schemaSetParam,
                            new CodeVariableReferenceExpression("tempSet"),
                            new CodePrimitiveExpression(null)))));

            schemaSetProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeVariableReferenceExpression(schemaSetFieldName)));

            //Setter
            schemaSetProperty.SetStatements.Add(new CodeAssignStatement(schemaSetFieldRef,
                new CodePropertySetValueReferenceExpression()));

            servicesTypeDecl.Members.Add(schemaSetField);
            servicesTypeDecl.Members.Add(schemaSetProperty);
            servicesTypeDecl.Members.Add(addSchemasMethod);
            //}
            //Implement ILinqToXsdTypeManager
            servicesTypeDecl.Members.Add(typeDictProperty);
            servicesTypeDecl.Members.Add(elementDictProperty);
            servicesTypeDecl.Members.Add(wrapperDictProperty);
            servicesTypeDecl.BaseTypes.Add(Constants.ILinqToXsdTypeManager);


            //Add a getter that will get the root type name
            CodeMemberMethod getRootType = new CodeMemberMethod();
            getRootType.Attributes = MemberAttributes.Static | memberVisibility;
            getRootType.Name = Constants.GetRootType;
            getRootType.ReturnType = new CodeTypeReference(Constants.SystemTypeName);
            if (rootElementName.IsEmpty)
            {
                getRootType.Statements.Add(
                    new CodeMethodReturnStatement(
                        CodeDomHelper.Typeof("Xml.Schema.Linq.XTypedElement")));
            }
            else
            {
                getRootType.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeIndexerExpression(
                            CodeDomHelper.CreateFieldReference(null, Constants.ElementDictionaryField),
                            CodeDomHelper.XNameGetExpression(rootElementName.Name,
                                rootElementName.Namespace))));
            }

            servicesTypeDecl.Members.Add(staticServicesConstructor);
            servicesTypeDecl.Members.Add(getRootType);
            servicesTypeDecl.Members.Add(singletonField);
            servicesTypeDecl.Members.Add(singletonProperty);
            return servicesTypeDecl;
        }

        public override string ToString() => $"{nameof(TypeBuilder)} ({this.clrTypeInfo})";
    }


    internal class CodeTypeDeclItems
    {
        public CodeConstructor functionalConstructor;
        public CodeTypeConstructor staticConstructor;
        public CodeObjectCreateExpression contentModelExpression;
        public Dictionary<string, CodeMemberProperty> propertyNameTypeTable;
        public bool hasElementWildCards;

        public CodeTypeDeclItems()
        {
        }

        public void Init()
        {
            functionalConstructor = null;
            staticConstructor = null;
            hasElementWildCards = false;
            contentModelExpression = null;
            if (propertyNameTypeTable != null)
            {
                propertyNameTypeTable.Clear();
            }
        }
    }


    internal class XTypedElementBuilder : TypeBuilder
    {
        CodeTypeDeclItems declItemsInfo;
        Stack<TypePropertyBuilder> propertyBuilderStack;
        TypePropertyBuilder propertyBuilder;
        CodeStatementCollection propertyDictionaryAddStatements;


        internal XTypedElementBuilder(LinqToXsdSettings settings): base(settings)
        {
            InnerInit();
        }

        // InnerInit is a non-virtual function to
        // prevent virtual methods from being called
        // in the call stack of the constructor
        protected new void InnerInit()
        {
            base.InnerInit();
            propertyBuilder = null;
            if (propertyBuilderStack != null)
            {
                propertyBuilderStack.Clear();
            }

            if (propertyDictionaryAddStatements != null)
            {
                propertyDictionaryAddStatements.Clear();
            }

            if (declItemsInfo == null)
            {
                declItemsInfo = new CodeTypeDeclItems();
            }
            else
            {
                declItemsInfo.Init();
            }
        }

        internal override void Init()
        {
            InnerInit();
        }

        protected override void SetElementWildCardFlag(bool hasAny)
        {
            declItemsInfo.hasElementWildCards = hasAny;
        }

        internal override void StartGrouping(GroupingInfo groupingInfo)
        {
            InitializeTables();
            propertyBuilder = TypePropertyBuilder.Create(propertyBuilder as ContentModelPropertyBuilder, groupingInfo, decl, declItemsInfo, DefaultVisibility);
            propertyBuilder.StartCodeGen(); //Start the group's code gen, like setting up functional const etc
            propertyBuilderStack.Push(propertyBuilder);
        }

        internal override void CreateProperty(ClrBasePropertyInfo propertyInfo, List<ClrAnnotation> annotations)
        {
            if (clrTypeInfo.InlineBaseType && propertyInfo.FromBaseType)
            {
                propertyInfo.IsNew = true;
            }

            propertyBuilder.GenerateCode(propertyInfo, annotations);
            if ((propertyInfo.ContentType == ContentType.Property) && !propertyInfo.IsDuplicate)
            {
                //Do not add repeating properties to the LocalElementDictionary of type
                propertyDictionaryAddStatements.Add(CodeDomHelper.CreateMethodCallFromField(
                    Constants.LocalElementDictionaryField, "Add",
                    propertyInfo.GetXName(),
                    CodeDomHelper.Typeof(propertyInfo.ClrTypeName)));
            }
        }

        internal override void EndGrouping()
        {
            propertyBuilder.EndCodeGen();
            propertyBuilderStack.Pop(); //Remove current property builder
            if (propertyBuilderStack.Count > 0)
            {
                propertyBuilder =
                    propertyBuilderStack.Peek(); //Re-set property builder to parent group's property builder
            }
        }

        internal override void CreateAttributeProperty(ClrBasePropertyInfo propertyInfo,
            List<ClrAnnotation> annotations)
        {
            propertyBuilder = TypePropertyBuilder.Create(decl, declItemsInfo, DefaultVisibility);
            propertyBuilder.GenerateCode(propertyInfo, annotations);
        }

        internal override CodeConstructor CreateFunctionalConstructor(List<ClrAnnotation> annotations)
        {
            CodeConstructor functionalConstructor = declItemsInfo.functionalConstructor;
            if (functionalConstructor != null && functionalConstructor.Parameters.Count > 0)
            {
                ApplyAnnotations(functionalConstructor, annotations, null);
                decl.Members.Add(functionalConstructor);
            }

            return functionalConstructor;
        }

        internal override void CreateStaticConstructor()
        {
            if (declItemsInfo.staticConstructor == null)
            {
                declItemsInfo.staticConstructor = new CodeTypeConstructor();
                decl.Members.Add(declItemsInfo.staticConstructor);
            }
        }

        public override string ToString() => $"{nameof(XTypedElementBuilder)} ({this.clrTypeInfo})";

        protected override void ImplementCommonIXMetaData()
        {
            CodeMemberProperty localElementDictionary = null;
            if (HasElementProperties)
            {
                CreateStaticConstructor();
                localElementDictionary = BuildLocalElementDictionary();
                declItemsInfo.staticConstructor.Statements.Add(
                    CodeDomHelper.CreateMethodCall(null, "BuildElementDictionary"));
                decl.Members.Add(localElementDictionary);
            }
        }

        protected override void ImplementContentModelMetaData()
        {
            CodeMemberMethod getContentModelMethod = null;

            if (HasElementProperties)
            {
                if (declItemsInfo.contentModelExpression != null)
                {
                    //Create static constr for the content model of the type
                    CodeTypeReference cmType = new CodeTypeReference(Constants.ContentModelType);

                    declItemsInfo.staticConstructor.Statements
                                 .Add( // contentModel = new Sequence/Choice/AllContentModel(...);
                                     new CodeAssignStatement(
                                         new CodeVariableReferenceExpression(Constants.ContentModelMember),
                                         declItemsInfo.contentModelExpression));

                    //Add static field to store the constructed content model
                    CodeMemberField contentModelField = new CodeMemberField(cmType, Constants.ContentModelMember);
                    CodeDomHelper.AddBrowseNever(contentModelField);
                    contentModelField.Attributes = MemberAttributes.Private | MemberAttributes.Static;

                    decl.Members.Add(contentModelField);

                    //Create Method impl
                    getContentModelMethod = CodeDomHelper.CreateInterfaceImplMethod(Constants.GetContentModel,
                        Constants.IXMetaData, cmType, Constants.ContentModelMember);
#if DEBUG
                    var str = declItemsInfo.contentModelExpression.ToCodeString();
                    Debug.Assert(str.IsNotEmpty());
#endif
                }
                else
                {
                    //Return Default content model
                    getContentModelMethod = DefaultContentModel();
                }
            }
            else
            {
                //No element children per schema
                if (this.clrTypeInfo.IsDerived)
                {
                    //Probably derived by restriction, use base content model
                    return;
                }
                else
                {
                    //Return Default content model
                    getContentModelMethod = DefaultContentModel();
                }
            }

            decl.Members.Add(getContentModelMethod);
        }

        protected override void ImplementFSMMetaData()
        {
            Debug.Assert(clrTypeInfo.HasElementWildCard);

            if (fsmNameSource == null) fsmNameSource = new StateNameSource();
            else fsmNameSource.Reset();

            FSM fsm = clrTypeInfo.CreateFSM(fsmNameSource);

            //Add a member field: private static FSM fsm;
            decl.Members.Add(CodeDomHelper.CreateMemberField(Constants.FSMMember, Constants.FSMClass, false, MemberAttributes.Private | MemberAttributes.Static));

            //Add a function: FSM  FSM IXMetaData.GetFSM() {return fsm}
            CodeMemberMethod getFSM =
                CodeDomHelper.CreateInterfaceImplMethod(Constants.GetFSM, Constants.IXMetaData,
                    new CodeTypeReference(Constants.FSMClass));

            getFSM.Statements.Add(
                new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, Constants.FSMMember)));
            decl.Members.Add(getFSM);

            //Add InitFSM() and construct the FSM
            CodeMemberMethod initFSM =
                CodeDomHelper.CreateMethod(Constants.InitFSM,
                    new CodeTypeReference(), MemberAttributes.Private | MemberAttributes.Static);
            FSMCodeDomHelper.CreateFSMStmt(fsm, initFSM.Statements);
            decl.Members.Add(initFSM);

            CreateStaticConstructor();
            declItemsInfo.staticConstructor.Statements.Add(
                CodeDomHelper.CreateMethodCall(null, Constants.InitFSM, null));
        }

        private bool HasElementProperties
        {
            get { return propertyDictionaryAddStatements != null && propertyDictionaryAddStatements.Count > 0; }
        }

        private CodeMemberProperty BuildLocalElementDictionary()
        {
            CodeMemberProperty localDictionaryProperty = CodeDomHelper.CreateInterfaceImplProperty(
                Constants.LocalElementsDictionary, Constants.IXMetaData,
                CodeDomHelper.CreateDictionaryType(Constants.XNameType, Constants.SystemTypeName));

            //new override for derived classes
            CodeMemberField localDictionaryField =
                CodeDomHelper.CreateDictionaryField(Constants.LocalElementDictionaryField, Constants.XNameType, Constants.SystemTypeName, MemberAttributes.Private);
            CodeMemberMethod localDictionaryMethod = CodeDomHelper.CreateMethod(Constants.BuildElementDictionary, null, MemberAttributes.Private | MemberAttributes.Static);
            localDictionaryMethod.Statements.AddRange(propertyDictionaryAddStatements);

            decl.Members.Add(localDictionaryField);
            decl.Members.Add(localDictionaryMethod);
            localDictionaryProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    CodeDomHelper.CreateFieldReference(null,
                        Constants.LocalElementDictionaryField)));


            CodeDomHelper.AddBrowseNever(localDictionaryProperty);
            CodeDomHelper.AddBrowseNever(localDictionaryField);
            return localDictionaryProperty;
        }

        private void InitializeTables()
        {
            if (propertyBuilderStack == null)
            {
                propertyBuilderStack = new Stack<TypePropertyBuilder>();
            }

            if (propertyDictionaryAddStatements == null)
            {
                //Allocate this since the properies within a grouping will need to be added to the type's element dictionary
                propertyDictionaryAddStatements = new CodeStatementCollection();
            }

            if (declItemsInfo.propertyNameTypeTable == null)
            {
                declItemsInfo.propertyNameTypeTable = new Dictionary<string, CodeMemberProperty>();
            }
        }
    }

    internal class XEmptyTypedElementBuilder : TypeBuilder
    {
        public XEmptyTypedElementBuilder(LinqToXsdSettings settings) : base(settings) { }
    }

    internal class XSimpleTypedElementBuilder : TypeBuilder
    {
        string simpleTypeName;
        bool isSchemaList;

        public XSimpleTypedElementBuilder(LinqToXsdSettings settings) : base(settings) { }

        internal void Init(string simpleTypeName, bool isSchemaList)
        {
            base.InnerInit();
            this.simpleTypeName = simpleTypeName;
            this.isSchemaList = isSchemaList;
        }

        internal override CodeConstructor CreateFunctionalConstructor(List<ClrAnnotation> annotations)
        {
            //Create Constructor that takes type to wrap
            string parameterName = Constants.InnerTypeParamName;
            CodeConstructor constructor = CodeDomHelper.CreateConstructor(DefaultVisibility.ToMemberAttribute());
            CodeTypeReference returnType = null;
            if (isSchemaList)
            {
                returnType = new CodeTypeReference("IList", new CodeTypeReference(simpleTypeName));
            }
            else
            {
                returnType = new CodeTypeReference(simpleTypeName);
            }

            constructor.Parameters.Add(new CodeParameterDeclarationExpression(returnType, parameterName));

            var thisTypedValuePropRef = new CodePropertyReferenceExpression(CodeDomHelper.This(), Constants.SInnerTypePropertyName);
            var thisConstructorParamRef = new CodeVariableReferenceExpression(parameterName);
            var codeAssignStatement = new CodeAssignStatement(thisTypedValuePropRef, thisConstructorParamRef);

            if (clrTypeInfo is ClrWrapperTypeInfo wrapperTypeInfo) {
                if (wrapperTypeInfo.InnerType.IsEnum && !wrapperTypeInfo.InnerType.IsEquivalentTypeReference(returnType)) {
                    var castAsInnerTypeEnum =
                        CodeDomHelper.CreateParseEnumCall(wrapperTypeInfo.InnerType.ClrFullTypeName, thisConstructorParamRef);

                    codeAssignStatement = new CodeAssignStatement(thisTypedValuePropRef, castAsInnerTypeEnum);
                }
            }

            constructor.Statements.Add(codeAssignStatement);

            ApplyAnnotations(constructor, annotations, null);
            decl.Members.Add(constructor);
            return constructor;
        }

        internal override void CreateProperty(ClrBasePropertyInfo propertyInfo, List<ClrAnnotation> annotations)
        {
            propertyInfo.AddToType(this.decl, annotations, DefaultVisibility);
        }
    }

    internal class XWrapperTypedElementBuilder : TypeBuilder
    {
        string innerTypeName;
        string innerTypeNs;
        string memberName;
        TypeAttributes innerTypeAttributes;

        public XWrapperTypedElementBuilder(LinqToXsdSettings settings) : base(settings) { }

        internal void Init(string innerTypeFullName, string innerTypeNs, TypeAttributes innerTypeAttributes)
        {
            base.InnerInit();
            this.memberName = NameGenerator.ChangeClrName(Constants.CInnerTypePropertyName, NameOptions.MakeField);
            this.innerTypeName = innerTypeFullName;
            this.innerTypeNs = innerTypeNs;
            this.innerTypeAttributes = innerTypeAttributes;
        }

        protected override string InnerType
        {
            get { return innerTypeName; }
        }

        internal override void CreateDefaultConstructor(List<ClrAnnotation> annotations)
        {
            //create type field to wrap
            CodeMemberField typeField =
                CodeDomHelper.CreateMemberField(memberName, innerTypeName, false, MemberAttributes.Private);
            CodeFieldReferenceExpression fieldRef = CodeDomHelper.CreateFieldReference("this", memberName);

            //Create empty constructor
            CodeConstructor emptyConstructor = CodeDomHelper.CreateConstructor(DefaultVisibility.ToMemberAttribute());
            if ((innerTypeAttributes & TypeAttributes.Abstract) == 0)
            {
                //New up inner type in default constructor only if inner type is not abstract
                emptyConstructor.Statements.Add(
                    CodeDomHelper.CreateMethodCall(null, Constants.SetInnerType,
                        new CodeObjectCreateExpression(typeField.Type)));
            }
            else
            {
                //Cannot construct wrappers of abstract types using the default constructor
                emptyConstructor.Statements.Add(
                    new CodeThrowExceptionStatement(
                        new CodeObjectCreateExpression("InvalidOperationException")));
            }

            CodeConstructor dummyConstructor = null;
            if (clrTypeInfo.IsSubstitutionHead)
            {
                //Add dummy constructor that derived classes can call
                dummyConstructor = CodeDomHelper.CreateConstructor(MemberAttributes.Family);
                dummyConstructor.Parameters.Add(new CodeParameterDeclarationExpression("System.Boolean", "setNull"));
                decl.Members.Add(dummyConstructor);
            }

            if (clrTypeInfo.IsSubstitutionMember())
            {
                //Always call the dummy constructor of head from a member
                emptyConstructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(true));
                if (dummyConstructor != null)
                {
                    dummyConstructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(true));
                }
            }

            ApplyAnnotations(emptyConstructor, annotations, null);

            decl.Members.Add(typeField);
            decl.Members.Add(emptyConstructor);
            decl.Members.Add(CreateUntypedProperty(fieldRef));
            decl.Members.Add(InnerTypeProperty());
            decl.Members.Add(SetInnerType());
            if (clrTypeInfo.IsSubstitutionHead)
            {
                //Add method to set base type field in the head type from the derived members
                decl.Members.Add(SetSubstitutionMember());
            }
        }

        internal override CodeConstructor CreateFunctionalConstructor(List<ClrAnnotation> annotations)
        {
            //Create Constructor that takes type to wrap
            CodeConstructor constructor = CodeDomHelper.CreateConstructor(DefaultVisibility.ToMemberAttribute());
            if (clrTypeInfo.IsSubstitutionMember())
            {
                //If member of subst group, call dummy base constructor
                constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(true));
            }

            constructor.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(innerTypeName), Constants.InnerTypeParamName));

            constructor.Statements.Add(CodeDomHelper.CreateMethodCall(null, Constants.SetInnerType,
                new CodeVariableReferenceExpression(Constants.InnerTypeParamName))); //SetInnerType();
            ApplyAnnotations(constructor, annotations, null);
            decl.Members.Add(constructor);
            return constructor;
        }


        internal override void CreateProperty(ClrBasePropertyInfo propertyInfo, List<ClrAnnotation> annotations)
        {
            ((ClrWrappingPropertyInfo) propertyInfo).WrappedFieldName = this.memberName;
            propertyInfo.AddToType(decl, annotations, DefaultVisibility);
        }

        protected override void ImplementCommonIXMetaData()
        {
            CodeMemberProperty localElementDictionary = CodeDomHelper.CreateInterfaceImplProperty(
                Constants.LocalElementsDictionary, Constants.IXMetaData,
                CodeDomHelper.CreateDictionaryType(Constants.XNameType, Constants.SystemTypeName));
            localElementDictionary.GetStatements.Add(CodeDomHelper.CreateCastToInterface(Constants.IXMetaData,
                "schemaMetaData", Constants.CInnerTypePropertyName));
            localElementDictionary.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodePropertyReferenceExpression(
                        new CodeVariableReferenceExpression("schemaMetaData"),
                        Constants.LocalElementsDictionary)));

            CodeMemberProperty contentProperty = CodeDomHelper.CreateInterfaceImplProperty(
                Constants.CInnerTypePropertyName, Constants.IXMetaData, new CodeTypeReference(Constants.XTypedElement));
            contentProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodePropertyReferenceExpression(
                        new CodeThisReferenceExpression(),
                        Constants.CInnerTypePropertyName)));

            decl.Members.Add(localElementDictionary);
            decl.Members.Add(contentProperty);
        }

        protected override void ImplementContentModelMetaData()
        {
            decl.Members.Add(DefaultContentModel()); //No direct element children return Default content model
        }

        internal void AddTypeToTypeManager(CodeStatementCollection elementDictionaryStatements,
            CodeStatementCollection wrapperDictionaryStatements)
        {
            base.AddTypeToTypeManager(elementDictionaryStatements, Constants.ElementDictionaryField);
            var innerTypeFullName = innerTypeName.Contains(innerTypeNs)
                ? "global::" + innerTypeName
                : "global::" + innerTypeNs + "." + innerTypeName;

            wrapperDictionaryStatements.Add(CodeDomHelper.CreateMethodCallFromField(Constants.WrapperDictionaryField,
                "Add", CodeDomHelper.Typeof(clrTypeInfo.clrFullTypeName), CodeDomHelper.Typeof(innerTypeFullName)));
        }

        private CodeMethodInvokeExpression SetNameMethodCall()
        {
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(Constants.XTypedServices),
                "SetName",
                CodeDomHelper.This(),
                CodeDomHelper.CreateFieldReference("this", memberName));
        }

        private CodeMemberProperty CreateUntypedProperty(CodeFieldReferenceExpression fieldRef)
        {
            //Create new XElement property so that the setter can set the wrapped object XElement as well
            CodeMemberProperty xElementProperty =
                CodeDomHelper.CreateProperty(new CodeTypeReference(Constants.XElement), true, MemberAttributes.Public); // because this is an override, it should not obey DefaultVisibility
            xElementProperty.Name = Constants.Untyped;
            xElementProperty.Attributes |= MemberAttributes.Override;

            CodePropertyReferenceExpression baseUntyped =
                new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), Constants.Untyped);
            xElementProperty.GetStatements.Add(
                new CodeMethodReturnStatement(baseUntyped));

            xElementProperty.SetStatements.Add(
                new CodeAssignStatement(
                    baseUntyped,
                    CodeDomHelper.SetValue()));

            if (clrTypeInfo.IsSubstitutionHead)
            {
                xElementProperty.SetStatements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            fieldRef,
                            CodeBinaryOperatorType.IdentityInequality,
                            new CodePrimitiveExpression(null)),
                        new CodeAssignStatement(
                            new CodePropertyReferenceExpression(fieldRef, Constants.Untyped),
                            CodeDomHelper.SetValue())));
            }
            else
            {
                //Field will always be non-null
                xElementProperty.SetStatements.Add(
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(fieldRef, Constants.Untyped),
                        CodeDomHelper.SetValue()));
            }

            return xElementProperty;
        }

        private CodeMemberProperty InnerTypeProperty()
        {
            //Create InnerType Property of type T  to go with the inner type field
            CodeMemberProperty innerTypeProperty = CodeDomHelper.CreateProperty(Constants.CInnerTypePropertyName,
                new CodeTypeReference(innerTypeName), DefaultVisibility.ToMemberAttribute());
            innerTypeProperty.HasSet = false;
            if (clrTypeInfo.IsSubstitutionMember())
            {
                innerTypeProperty.Attributes |= MemberAttributes.New;
            }

            innerTypeProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    CodeDomHelper.CreateFieldReference(null, memberName)));
            return innerTypeProperty;
        }

        private CodeMemberMethod SetSubstitutionMember()
        {
            //This is for setting base type fields from types representing substitutionGroup members
            CodeMemberMethod setSubstMember =
                CodeDomHelper.CreateMethod(Constants.SetSubstitutionMember, null, MemberAttributes.Family);
            setSubstMember.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(innerTypeName), memberName));
            setSubstMember.Statements.Add(
                new CodeAssignStatement(
                    CodeDomHelper.CreateFieldReference("this", memberName),
                    new CodeVariableReferenceExpression(memberName)));

            if (clrTypeInfo.IsSubstitutionMember())
            {
                //Add base.SetSubstitutionMember() method if this class itself is a member of another subst group
                setSubstMember.Statements.Add(CodeDomHelper.CreateMethodCall(new CodeBaseReferenceExpression(),
                    Constants.SetSubstitutionMember, new CodeVariableReferenceExpression(memberName)));
            }

            return setSubstMember;
        }

        private CodeMemberMethod SetInnerType()
        {
            CodeMemberMethod setInnerType =
                CodeDomHelper.CreateMethod(Constants.SetInnerType, null, MemberAttributes.Private);
            setInnerType.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(innerTypeName), memberName));
            setInnerType.Statements.Add(
                new CodeAssignStatement(
                    CodeDomHelper.CreateFieldReference("this", memberName),
                    new CodeCastExpression(
                        innerTypeName,
                        new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(Constants.XTypedServices),
                            "GetCloneIfRooted",
                            new CodeVariableReferenceExpression(memberName)))));

            setInnerType.Statements.Add(SetNameMethodCall()); //SetName(); 
            if (clrTypeInfo.IsSubstitutionMember())
            {
                setInnerType.Statements.Add(CodeDomHelper.CreateMethodCall(new CodeBaseReferenceExpression(),
                    Constants.SetSubstitutionMember, new CodeVariableReferenceExpression(memberName)));
            }

            return setInnerType;
        }
    }
}
