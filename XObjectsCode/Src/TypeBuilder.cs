//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml.Schema;
using System.Collections.Generic;
using System.CodeDom;
using System.Reflection;
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

        internal CodeTypeDeclaration TypeDeclaration => decl;

        internal virtual void CreateDefaultConstructor(List<ClrAnnotation> annotations)
        { }

        internal virtual CodeConstructor CreateFunctionalConstructor(List<ClrAnnotation> annotations)
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

        internal void ImplementInterfaces(bool enableServiceReference)
        {
            ImplementIXMetaData();
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

        protected virtual void ImplementContentModelMetaData()
        {
            decl.Members.Add(DefaultContentModel());
        }

        protected virtual string InnerType
        {
            get { return null; }
        }

        internal void CreateTypeDeclaration(ClrTypeInfo clrTypeInfo, CodeNamespace parentNamespace)
        {
            this.clrTypeInfo = clrTypeInfo;
            SetElementWildCardFlag(clrTypeInfo.HasElementWildCard);

            decl =  new(clrTypeInfo.clrtypeName);
        }

        protected virtual void ImplementCommonIXMetaData()
        {
            //Do nothing, this will inherit the LocalElementDictionary from XTypedElement which returns empty dict and Content which returns null
        }

        private void ImplementIXMetaData()
        {
            // TODO: content-related metadata
            // ImplementCommonIXMetaData();
            // if (clrTypeInfo.HasElementWildCard) ImplementFSMMetaData();
            // else ImplementContentModelMetaData();
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
            // Fully implemented in simple-type.scriban-cs
            
            string typeName = typeInfo is EnumSimpleTypeInfo ? typeInfo.clrtypeName + Constants.EnumValidator : typeInfo.clrtypeName;
            var simpleTypeDecl = new CodeTypeDeclaration(typeName);
            // might need special handling when typeInfo.clrtypeNs is null, but returning default Visibility (public) when clrtypeNs is null works for now
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

        /// <summary>
        /// Allows logic to query adjacent types in the same namespace for the existence of other types.
        /// </summary>
        protected CodeNamespace ParentNamespace { get; }

        internal XTypedElementBuilder(LinqToXsdSettings settings, CodeNamespace parentNamespace): base(settings)
        {
            ParentNamespace = parentNamespace;
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
            propertyBuilder = TypePropertyBuilder.Create(propertyBuilder as ContentModelPropertyBuilder, groupingInfo, decl, declItemsInfo, DefaultVisibility, ParentNamespace);
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
            propertyBuilder = TypePropertyBuilder.Create(decl, declItemsInfo, DefaultVisibility, ParentNamespace);
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

        protected override void ImplementContentModelMetaData()
        {
// Scriban: done
//             CodeMemberMethod getContentModelMethod = null;

//             if (HasElementProperties)
//             {
//                 if (declItemsInfo.contentModelExpression != null)
//                 {
//                     //Create static constr for the content model of the type
//                     CodeTypeReference cmType = new CodeTypeReference(Constants.ContentModelType);

//                     declItemsInfo.staticConstructor.Statements
//                                  .Add( // contentModel = new Sequence/Choice/AllContentModel(...);
//                                      new CodeAssignStatement(
//                                          new CodeVariableReferenceExpression(Constants.ContentModelMember),
//                                          declItemsInfo.contentModelExpression));

//                     //Add static field to store the constructed content model
//                     CodeMemberField contentModelField = new CodeMemberField(cmType, Constants.ContentModelMember);
//                     CodeDomHelper.AddBrowseNever(contentModelField);
//                     contentModelField.Attributes = MemberAttributes.Private | MemberAttributes.Static;

//                     decl.Members.Add(contentModelField);

//                     //Create Method impl
//                     getContentModelMethod = CodeDomHelper.CreateInterfaceImplMethod(Constants.GetContentModel,
//                         Constants.IXMetaData, cmType, Constants.ContentModelMember);
// #if DEBUG
//                     var str = declItemsInfo.contentModelExpression.ToCodeString();
//                     Debug.Assert(str.IsNotEmpty());
// #endif
//                 }
//                 else
//                 {
//                     //Return Default content model
//                     getContentModelMethod = DefaultContentModel();
//                 }
//             }
//             else
//             {
//                 //No element children per schema
//                 if (this.clrTypeInfo.IsDerived)
//                 {
//                     //Probably derived by restriction, use base content model
//                     return;
//                 }
//                 else
//                 {
//                     //Return Default content model
//                     getContentModelMethod = DefaultContentModel();
//                 }
//             }

//             decl.Members.Add(getContentModelMethod);
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

    internal class XWrapperTypedElementBuilder(LinqToXsdSettings settings) : TypeBuilder(settings)
    {
        string innerTypeName;
        string memberName = NameGenerator.ChangeClrName(Constants.CInnerTypePropertyName, NameOptions.MakeField);

        protected override string InnerType
        {
            get { return innerTypeName; }
        }

        internal override void CreateProperty(ClrBasePropertyInfo propertyInfo, List<ClrAnnotation> annotations)
        {
            ((ClrWrappingPropertyInfo) propertyInfo).WrappedFieldName = this.memberName;
            propertyInfo.AddToType(decl, annotations, DefaultVisibility);
        }
    }
}
