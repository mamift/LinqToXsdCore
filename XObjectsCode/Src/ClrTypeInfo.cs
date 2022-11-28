//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.CodeDom;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.CodeGen
{
    public class ClrMappingInfo
    {
        List<ClrTypeInfo> types;
        Dictionary<XmlSchemaObject, string> nameMappings;

        internal List<ClrTypeInfo> Types
        {
            get
            {
                if (types == null)
                {
                    types = new List<ClrTypeInfo>();
                }

                return types;
            }
        }

        internal Dictionary<XmlSchemaObject, string> NameMappings
        {
            get
            {
                if (nameMappings == null)
                {
                    nameMappings = new Dictionary<XmlSchemaObject, string>();
                }

                return nameMappings;
            }
            set { nameMappings = value; }
        }
    }

    internal abstract partial class ClrTypeInfo
    {
        //Names
        internal string clrtypeName;
        internal string clrtypeNs;
        internal string schemaName;
        internal string schemaNs;
        internal string clrFullTypeName;

        internal string contentModelRegEx;

        internal XmlSchemaObject baseType;
        internal string baseTypeClrName;
        internal string baseTypeClrNs;

        internal ClrTypeInfo Parent { get; set; }

        //Type properties 
        protected ClrTypeFlags clrTypeFlags;
        internal SchemaOrigin typeOrigin;

        //Intellisense type information
        protected List<ClrAnnotation> annotations;

        public ClrTypeInfo()
        {
            Init();
        }

        /// <summary>
        /// If this instance is nested under another <see cref="ClrTypeInfo"/> instance,
        /// this method will resolve all the parent references and return a string that can be used
        /// in a fully-qualified type reference string.
        /// <para>Intended for type references that are themselves defined inside another.</para>
        /// </summary>
        /// <param name="includeNs">Optionally set to true to include the namespace.</param>
        /// <returns></returns>
        internal string GetNestedTypeScopedResolutionString(bool includeNs = true)
        {
            var scopeSb = includeNs ? new StringBuilder(this.clrtypeNs) : new StringBuilder();

            var theParent = Parent;
            var iterationCount = 0;
            do {
                if (theParent?.clrtypeName.IsNullOrEmpty() ?? true) {
                    if (iterationCount == 0) goto appendThis;
                    else continue;
                }

                scopeSb.Append("." + theParent.clrtypeName);
                theParent = theParent.Parent;
                iterationCount++;
                appendThis:
                if (theParent == null) {
                    // add this instance typename after exhausting all parent refs
                    scopeSb.Append("." + this.clrtypeName);
                }
            } while (theParent != null);

            // this happens when both this.clrTypeName is null and Parent is null
            if (scopeSb.Length == 1) return string.Empty;

            return scopeSb.ToString();
        }

        private void Init()
        {
            clrtypeName = null;
            clrtypeNs = null;
            schemaName = null;
            schemaNs = null;
            baseType = null;

            clrTypeFlags = ClrTypeFlags.None;
            typeOrigin = SchemaOrigin.None;

            annotations = new List<ClrAnnotation>();
        }

        internal bool IsDerived
        {
            get { return baseType != null; }
        }

        internal XmlQualifiedName BaseTypeName
        {
            get
            {
                if (baseType == null)
                {
                    return XmlQualifiedName.Empty;
                }

                XmlSchemaType schemaType = baseType as XmlSchemaType;
                if (schemaType != null)
                {
                    return schemaType.QualifiedName;
                }
                else
                {
                    XmlSchemaElement schemaElement = baseType as XmlSchemaElement; //For subst groups
                    return schemaElement.QualifiedName;
                }
            }
        }

        internal bool IsAbstract
        {
            get { return (clrTypeFlags & ClrTypeFlags.IsAbstract) != 0; }
            set
            {
                if (value)
                {
                    clrTypeFlags |= ClrTypeFlags.IsAbstract;
                }
                else
                {
                    clrTypeFlags &= ~ClrTypeFlags.IsAbstract;
                }
            }
        }

        internal bool IsSealed
        {
            get { return (clrTypeFlags & ClrTypeFlags.IsSealed) != 0; }
            set
            {
                if (value)
                {
                    clrTypeFlags |= ClrTypeFlags.IsSealed;
                }
                else
                {
                    clrTypeFlags &= ~ClrTypeFlags.IsSealed;
                }
            }
        }

        internal bool IsRoot
        {
            get { return (clrTypeFlags & ClrTypeFlags.IsRoot) != 0; }
            set
            {
                if (value)
                {
                    clrTypeFlags |= ClrTypeFlags.IsRoot;
                }
                else
                {
                    clrTypeFlags &= ~ClrTypeFlags.IsRoot;
                }
            }
        }

        internal bool IsNested
        {
            get { return (clrTypeFlags & ClrTypeFlags.IsNested) != 0; }
            set
            {
                if (value)
                {
                    clrTypeFlags |= ClrTypeFlags.IsNested;
                }
                else
                {
                    clrTypeFlags &= ~ClrTypeFlags.IsNested;
                }
            }
        }

        internal bool InlineBaseType
        {
            get { return (clrTypeFlags & ClrTypeFlags.InlineBaseType) != 0; }
            set
            {
                if (value)
                {
                    clrTypeFlags |= ClrTypeFlags.InlineBaseType;
                }
                else
                {
                    clrTypeFlags &= ~ClrTypeFlags.InlineBaseType;
                }
            }
        }

        internal bool IsSubstitutionHead
        {
            get { return (clrTypeFlags & ClrTypeFlags.IsSubstitutionHead) != 0; }
            set
            {
                if (value)
                {
                    clrTypeFlags |= ClrTypeFlags.IsSubstitutionHead;
                }
                else
                {
                    clrTypeFlags &= ~ClrTypeFlags.IsSubstitutionHead;
                }
            }
        }

        internal bool HasElementWildCard
        {
            get { return (clrTypeFlags & ClrTypeFlags.HasElementWildCard) != 0; }
            set
            {
                if (value)
                {
                    clrTypeFlags |= ClrTypeFlags.HasElementWildCard;
                }
                else
                {
                    clrTypeFlags &= ~ClrTypeFlags.HasElementWildCard;
                }
            }
        }

        internal bool IsRootElement
        {
            get { return typeOrigin == SchemaOrigin.Element; }
        }


        internal bool IsSubstitutionMember()
        {
            //types whose origin is element, If they have a base type its from being a member of a subst group
            if (typeOrigin == SchemaOrigin.Element && baseType != null && !IsHeadAnyType())
            {
                //skip if the head element is xs:anyType
                return true;
            }

            return false;
        }

        internal virtual bool IsWrapper
        {
            get { return false; }
        }

        internal virtual bool HasBaseContentType
        {
            //For wrappers that are substitutionGroup members, if the type is the same as that of the head, this property returns true
            get { return false; }
        }

        internal List<ClrAnnotation> Annotations
        {
            get { return annotations; }
        }

        internal string ContentModelRegEx
        {
            get { return contentModelRegEx; }

            set { contentModelRegEx = value; }
        }

        private bool IsHeadAnyType()
        {
            Debug.Assert(baseType != null);
            XmlSchemaElement headElem = baseType as XmlSchemaElement;
            Debug.Assert(headElem != null);
            return headElem.ElementSchemaType.TypeCode == XmlTypeCode.Item;
        }

        internal virtual FSM CreateFSM(StateNameSource stateNames)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => this.clrtypeName;
    }


    internal class ClrContentTypeInfo : ClrTypeInfo
    {
        //Group/Properties
        internal ContentInfo lastTypeMember;

        //Nested types
        internal List<ClrTypeInfo> nestedTypes;

        internal ClrContentTypeInfo()
        {
        }

        internal IEnumerable<ContentInfo> Content
        {
            get
            {
                ContentInfo current = lastTypeMember;
                while (current != null)
                {
                    current = current.nextSibling;
                    yield return current;
                    if (current == lastTypeMember)
                    {
                        yield break;
                    }
                }
            }
        }

        internal void AddMember(ContentInfo member)
        {
            if (lastTypeMember == null)
            {
                member.nextSibling = member; //Point to the same item as first
            }
            else
            {
                member.nextSibling = lastTypeMember.nextSibling;
                lastTypeMember.nextSibling = member;
            }

            lastTypeMember = member;
        }

        internal List<ClrTypeInfo> NestedTypes
        {
            get
            {
                if (nestedTypes == null)
                {
                    nestedTypes = new List<ClrTypeInfo>();
                }

                return nestedTypes;
            }
        }

        internal override FSM CreateFSM(StateNameSource stateNames)
        {
            //Should have only one top-level grouping content info
            foreach (ContentInfo content in Content)
            {
                GroupingInfo group = content as GroupingInfo;
                if (group != null)
                {
                    FSM fsm = group.MakeFSM(stateNames);
                    return fsm;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return $"{base.ToString()} {{ {string.Join(", ", this.Content.Select(x => x.ToString()))} }}";
        }
    }


    internal class ClrWrapperTypeInfo : ClrTypeInfo
    {
        ClrTypeReference innerType;
        internal string fixedDefaultValue;
        bool hasBaseContentType;

        internal ClrWrapperTypeInfo()
        {
        }

        internal ClrWrapperTypeInfo(bool hasBaseContentType)
        {
            this.hasBaseContentType = hasBaseContentType;
        }

        internal override bool IsWrapper
        {
            get { return true; }
        }

        internal override bool HasBaseContentType
        {
            get { return hasBaseContentType; }
        }

        internal ClrTypeReference InnerType
        {
            get { return innerType; }
            set { innerType = value; }
        }

        internal string FixedValue
        {
            get
            {
                if ((clrTypeFlags & ClrTypeFlags.HasFixedValue) != 0) return fixedDefaultValue;
                else return null;
            }
            set
            {
                if (value != null)
                {
                    clrTypeFlags |= ClrTypeFlags.HasFixedValue;
                    fixedDefaultValue = value;
                }
            }
        }


        internal string DefaultValue
        {
            get
            {
                if ((clrTypeFlags & ClrTypeFlags.HasDefaultValue) != 0) return fixedDefaultValue;
                else return null;
            }
            set
            {
                if (value != null)
                {
                    clrTypeFlags |= ClrTypeFlags.HasDefaultValue;
                    fixedDefaultValue = value;
                }
            }
        }
    }

    internal abstract partial class ContentInfo
    {
        internal ContentInfo lastChild;
        internal ContentInfo nextSibling;
        protected ContentType contentType;
        protected Occurs occursInSchema; //The original occurence information in the XML schema

        internal Occurs OccursInSchema
        {
            get { return occursInSchema; }
        }

        internal bool IsOptional
        {
            get { return IsQMark || IsStar; }
        }

        internal bool IsStar
        {
            get { return this.occursInSchema == Occurs.ZeroOrMore; }
        }

        internal bool IsPlus
        {
            get { return this.occursInSchema == Occurs.OneOrMore; }
        }

        internal bool IsQMark
        {
            get { return this.occursInSchema == Occurs.ZeroOrOne; }
        }


        internal ContentType ContentType
        {
            get { return contentType; }
        }


        internal IEnumerable<ContentInfo> Children
        {
            get
            {
                ContentInfo current = lastChild;
                while (current != null)
                {
                    current = current.nextSibling;
                    yield return current;
                    if (current == lastChild)
                    {
                        yield break;
                    }
                }
            }
        }

        internal void AddChild(ContentInfo content)
        {
            if (lastChild == null)
            {
                content.nextSibling = content;
            }
            else
            {
                content.nextSibling = lastChild.nextSibling;
                lastChild.nextSibling = content;
            }

            lastChild = content;
        }

        internal string OccurenceString
        {
            get
            {
                if (this.IsStar)
                {
                    return "*";
                }
                else if (this.IsPlus)
                {
                    return "+";
                }
                else if (this.IsQMark)
                {
                    return "?";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public override string ToString()
        {
            return this.Children.Any() ? $"{{ {string.Join(", ", this.Children.Select(x => x.ToString()))} }}{this.OccurenceString}" : string.Empty;
        }
    }

    internal partial class GroupingInfo : ContentInfo
    {
        ContentModelType contentModelType;
        GroupingFlags groupingFlags;

        internal GroupingInfo(ContentModelType cmType, Occurs occursInSchema)
        {
            this.contentModelType = cmType;
            this.occursInSchema = occursInSchema;
            this.contentType = ContentType.Grouping;
            if ((int) occursInSchema > (int) Occurs.ZeroOrOne)
            {
                groupingFlags |= GroupingFlags.Repeating;
            }
        }

        internal bool IsRepeating
        {
            get { return (groupingFlags & GroupingFlags.Repeating) != 0; }
            set
            {
                if (value)
                {
                    groupingFlags |= GroupingFlags.Repeating;
                }
                else
                {
                    groupingFlags &= ~GroupingFlags.Repeating;
                }
            }
        }

        internal bool HasChildGroups
        {
            get { return (groupingFlags & GroupingFlags.HasChildGroups) != 0; }
            set
            {
                if (value)
                {
                    groupingFlags |= GroupingFlags.HasChildGroups;
                }
                else
                {
                    groupingFlags &= ~GroupingFlags.HasChildGroups;
                }
            }
        }

        internal bool HasRepeatingGroups
        {
            get { return (groupingFlags & GroupingFlags.HasRepeatingGroups) != 0; }
            set
            {
                if (value)
                {
                    groupingFlags |= GroupingFlags.HasRepeatingGroups;
                }
                else
                {
                    groupingFlags &= ~GroupingFlags.HasRepeatingGroups;
                }
            }
        }

        internal bool IsNested
        {
            get { return (groupingFlags & GroupingFlags.Nested) != 0; }
            set
            {
                if (value)
                {
                    groupingFlags |= GroupingFlags.Nested;
                }
                else
                {
                    groupingFlags &= ~GroupingFlags.Nested;
                }
            }
        }

        internal bool HasRecurrentElements
        {
            get { return (groupingFlags & GroupingFlags.HasRecurrentElements) != 0; }
            set
            {
                if (value)
                {
                    groupingFlags |= GroupingFlags.HasRecurrentElements;
                }
                else
                {
                    groupingFlags &= ~GroupingFlags.HasRecurrentElements;
                }
            }
        }

        internal bool IsComplex
        {
            get
            {
                return
                    HasChildGroups | IsRepeating |
                    HasRecurrentElements; //Earlier we used to disable DML for the repeating and nested part of the content model
                //Now, we turn the whole content model to AppendOnly mode
            }
        }

        internal ContentModelType ContentModelType
        {
            get { return contentModelType; }
        }

        public override string ToString() => $"{this.contentModelType} {base.ToString()}";
    }
}
