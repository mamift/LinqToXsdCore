//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Diagnostics;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.CodeGen
{
    internal abstract class TypePropertyBuilder
    {
        protected CodeTypeDeclItems declItems;
        protected CodeTypeDeclaration decl;

        protected GeneratedTypesVisibility visibility;
        
        protected CodeNamespace ParentNamespace { get; }

        public TypePropertyBuilder(CodeTypeDeclaration decl, CodeTypeDeclItems declItems, GeneratedTypesVisibility visibility, 
            CodeNamespace parentNamespace = null)
        {
            this.decl = decl;
            this.declItems = declItems;
            this.visibility = visibility;
            ParentNamespace = parentNamespace;
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

        public static TypePropertyBuilder Create(ContentModelPropertyBuilder parentBuilder, GroupingInfo groupingInfo, CodeTypeDeclaration decl,
            CodeTypeDeclItems declItems, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public, CodeNamespace parentNs = null)
        {
            switch (groupingInfo.ContentModelType)
            {
                case ContentModelType.None:
                case ContentModelType.All:
                    return new DefaultPropertyBuilder(decl, declItems, visibility, parentNs);

                // case ContentModelType.Sequence:
                //     return new SequencePropertyBuilder(parentBuilder, groupingInfo, decl, declItems, visibility, parentNs);

                // case ContentModelType.Choice:
                //     return new ChoicePropertyBuilder(parentBuilder, groupingInfo, decl, declItems, visibility, parentNs);

                default:
                    throw new InvalidOperationException();
            }
        }

        public static TypePropertyBuilder Create(CodeTypeDeclaration decl, CodeTypeDeclItems declItems,
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public, CodeNamespace parentNs = null)
        {
            return new DefaultPropertyBuilder(decl, declItems, visibility, parentNs);
        }
    }

    internal abstract class ContentModelPropertyBuilder : TypePropertyBuilder
    {
        protected GroupingInfo grouping;
        protected CodeObjectCreateExpression contentModelExpression;

        public ContentModelPropertyBuilder(ContentModelPropertyBuilder parentBuilder, GroupingInfo grouping, CodeTypeDeclaration decl, CodeTypeDeclItems declItems,
            GeneratedTypesVisibility visibility, CodeNamespace parentNs)
            : base(decl, declItems, visibility, parentNs)
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
            if (!declItems.hasElementWildCards)
            {
                if (property is ClrPropertyInfo prop)
                {
                    // Checks if the type has an XName field for the property and will create it if it does not exist.
                    // Properties inherited don't need a declaration as there's an accessible declaration in parent class.
                    if (!prop.FromBaseType && !decl.HasXNameFieldForProperty(property))
                    {
                        prop.CreateXNameField(decl);

                        Debug.Assert(decl.HasXNameFieldForProperty(prop));
                    }
                }
                property.AddToContentModel(contentModelExpression);
            }
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

#if DEBUG
                var str = parentContentModelExp.ToCodeString();
                Debug.Assert(str.IsNotEmpty());
#endif
            }
        }
    }

    internal class DefaultPropertyBuilder : TypePropertyBuilder
    {
        internal DefaultPropertyBuilder(CodeTypeDeclaration decl, CodeTypeDeclItems declItems,
            GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public, CodeNamespace parentNs = null) : base(decl, declItems, visibility, parentNs)
        {
        }
    }
}