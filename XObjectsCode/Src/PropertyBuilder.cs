//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.CodeDom;
using XObjects;

namespace Xml.Schema.Linq.CodeGen
{
    internal abstract class TypePropertyBuilder
    {
        protected CodeTypeDeclItems declItems;
        protected CodeTypeDeclaration decl;

        protected GeneratedTypesVisibility visibility;

        public TypePropertyBuilder(CodeTypeDeclaration decl, CodeTypeDeclItems declItems, GeneratedTypesVisibility visibility)
        {
            this.decl = decl;
            this.declItems = declItems;
            this.visibility = visibility;
        }

        public virtual void StartCodeGen()
        {
        }

        public virtual void GenerateCode(ClrBasePropertyInfo property, List<ClrAnnotation> annotations)
        {
            property.AddToType(decl, annotations, visibility);
        }

        public virtual void EndCodeGen()
        {
            //Do Nothing
        }

        public virtual bool IsRepeating
        {
            get { return false; }
        }

        public static TypePropertyBuilder Create(ContentModelPropertyBuilder parentBuilder, GroupingInfo groupingInfo, CodeTypeDeclaration decl,
            CodeTypeDeclItems declItems, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            switch (groupingInfo.ContentModelType)
            {
                case ContentModelType.None:
                case ContentModelType.All:
                    return new DefaultPropertyBuilder(decl, declItems, visibility);

                case ContentModelType.Sequence:
                    return new SequencePropertyBuilder(parentBuilder, groupingInfo, decl, declItems, visibility);

                case ContentModelType.Choice:
                    return new ChoicePropertyBuilder(parentBuilder, groupingInfo, decl, declItems, visibility);

                default:
                    throw new InvalidOperationException();
            }
        }

        public static TypePropertyBuilder Create(CodeTypeDeclaration decl, CodeTypeDeclItems declItems,
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
        {
            return new DefaultPropertyBuilder(decl, declItems, visibility);
        }
    }

    internal abstract class ContentModelPropertyBuilder : TypePropertyBuilder
    {
        protected GroupingInfo grouping;
        protected CodeObjectCreateExpression contentModelExpression;

        public ContentModelPropertyBuilder(ContentModelPropertyBuilder parentBuilder, GroupingInfo grouping, CodeTypeDeclaration decl, CodeTypeDeclItems declItems,
            GeneratedTypesVisibility visibility)
            : base(decl, declItems, visibility)
        {
            this.ParentBuilder = parentBuilder;
            this.grouping = grouping; //The grouping the contentmodelbuilder works on
        }

        public ContentModelPropertyBuilder ParentBuilder { get; }

        public abstract CodeObjectCreateExpression CreateContentModelExpression();

        public virtual void GenerateConstructorCode(ClrBasePropertyInfo property)
        {
            //Do nothing for sequences and all
        }

        public override void StartCodeGen()
        {
            AddToContentModel();
        }

        public override void GenerateCode(ClrBasePropertyInfo property, List<ClrAnnotation> annotations)
        {
            GenerateConstructorCode(property);
            property.AddToType(decl, annotations, visibility);
            if (!declItems.hasElementWildCards) property.AddToContentModel(contentModelExpression);
        }

        public override string ToString()
        {
            return $"{nameof(ContentModelPropertyBuilder)} ({this.grouping})";
        }

        private void AddToContentModel()
        {
            contentModelExpression = CreateContentModelExpression();
            if (this.ParentBuilder == null)
            {
                declItems.contentModelExpression = contentModelExpression;
            }
            else
            {
                var parentContentModelExp = this.ParentBuilder.contentModelExpression;
                parentContentModelExp.Parameters.Add(contentModelExpression);
            }
        }
    }

    internal class SequencePropertyBuilder : ContentModelPropertyBuilder
    {
        public SequencePropertyBuilder(ContentModelPropertyBuilder parentBuilder, GroupingInfo grouping, CodeTypeDeclaration decl, CodeTypeDeclItems declItems,
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public) :
            base(parentBuilder, grouping, decl, declItems, visibility)
        {
        }

        public override CodeObjectCreateExpression CreateContentModelExpression()
        {
            return new CodeObjectCreateExpression(new CodeTypeReference(Constants.SequenceContentModelEntity));
        }
    }

    internal class ChoicePropertyBuilder : ContentModelPropertyBuilder
    {
        List<CodeConstructor> choiceConstructors;
        bool flatChoice; //No nested groups, no child groups and not repeating
        bool hasDuplicateType;
        Dictionary<string, ClrBasePropertyInfo> propertyTypeNameTable;

        public ChoicePropertyBuilder(ContentModelPropertyBuilder parentBuilder, GroupingInfo grouping, CodeTypeDeclaration decl, CodeTypeDeclItems declItems,
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public) :
            base(parentBuilder, grouping, decl, declItems, visibility)
        {
            flatChoice = !grouping.IsNested && !grouping.IsRepeating && !grouping.HasChildGroups;
            hasDuplicateType = false;
            if (flatChoice)
            {
                propertyTypeNameTable = new Dictionary<string, ClrBasePropertyInfo>();
            }
        }

        public override void GenerateConstructorCode(ClrBasePropertyInfo property)
        {
            if (flatChoice && !hasDuplicateType && property.ContentType != ContentType.WildCardProperty)
            {
                ClrBasePropertyInfo prevProperty = null;
                string propertyReturnType = property.ClrTypeName;
                if (propertyTypeNameTable.TryGetValue(propertyReturnType, out prevProperty))
                {
                    hasDuplicateType = true;
                    return;
                }
                else
                {
                    propertyTypeNameTable.Add(propertyReturnType, property);
                }

                if (choiceConstructors == null)
                {
                    choiceConstructors = new List<CodeConstructor>();
                }


                CodeConstructor choiceConstructor = CodeDomHelper.CreateConstructor(visibility.ToMemberAttribute());
                property.AddToConstructor(choiceConstructor);
                choiceConstructors.Add(choiceConstructor);
            }
        }

        public override void EndCodeGen()
        {
            if (choiceConstructors != null && !hasDuplicateType)
            {
                foreach (CodeConstructor choiceConst in choiceConstructors)
                {
                    decl.Members.Add(choiceConst);
                }
            }
        }

        public override CodeObjectCreateExpression CreateContentModelExpression()
        {
            return new CodeObjectCreateExpression(new CodeTypeReference(Constants.ChoiceContentModelEntity));
        }
    }

    internal class DefaultPropertyBuilder : TypePropertyBuilder
    {
        internal DefaultPropertyBuilder(CodeTypeDeclaration decl, CodeTypeDeclItems declItems, 
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public) : base(decl, declItems, visibility)
        {
        }
    }
}