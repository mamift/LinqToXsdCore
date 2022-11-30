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

        public List<ClrTypeInfo> Types
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

        public Dictionary<XmlSchemaObject, string> NameMappings
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

    public abstract partial class ClrTypeInfo
    {
        //Names
        public string clrtypeName;
        public string clrtypeNs;
        public string schemaName;
        public string schemaNs;
        public string clrFullTypeName;

        public string contentModelRegEx;

        public XmlSchemaObject baseType;
        public string baseTypeClrName;
        public string baseTypeClrNs;

        public ClrTypeInfo Parent { get; set; }

        //Type properties 
        protected ClrTypeFlags clrTypeFlags;
        public SchemaOrigin typeOrigin;

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
        public string GetNestedTypeScopedResolutionString(bool includeNs = true)
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

        public bool IsDerived
        {
            get { return baseType != null; }
        }

        public XmlQualifiedName BaseTypeName
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

        public bool IsAbstract
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

        public bool IsSealed
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

        public bool IsRoot
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

        public bool IsNested
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

        public bool InlineBaseType
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

        public bool IsSubstitutionHead
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

        public bool HasElementWildCard
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

        public bool IsRootElement
        {
            get { return typeOrigin == SchemaOrigin.Element; }
        }


        public bool IsSubstitutionMember()
        {
            //types whose origin is element, If they have a base type its from being a member of a subst group
            if (typeOrigin == SchemaOrigin.Element && baseType != null && !IsHeadAnyType())
            {
                //skip if the head element is xs:anyType
                return true;
            }

            return false;
        }

        public virtual bool IsWrapper
        {
            get { return false; }
        }

        public virtual bool HasBaseContentType
        {
            //For wrappers that are substitutionGroup members, if the type is the same as that of the head, this property returns true
            get { return false; }
        }

        public List<ClrAnnotation> Annotations
        {
            get { return annotations; }
        }

        public string ContentModelRegEx
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

        public virtual FSM CreateFSM(StateNameSource stateNames)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => this.clrtypeName;
    }


    public class ClrContentTypeInfo : ClrTypeInfo
    {
        //Group/Properties
        public ContentInfo lastTypeMember;

        //Nested types
        public List<ClrTypeInfo> nestedTypes;

        public ClrContentTypeInfo()
        {
        }

        public IEnumerable<ContentInfo> Content
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

        public void AddMember(ContentInfo member)
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

        public List<ClrTypeInfo> NestedTypes
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

        public override FSM CreateFSM(StateNameSource stateNames)
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


    public class ClrWrapperTypeInfo : ClrTypeInfo
    {
        ClrTypeReference innerType;
        public string fixedDefaultValue;
        bool hasBaseContentType;

        public ClrWrapperTypeInfo()
        {
        }

        public ClrWrapperTypeInfo(bool hasBaseContentType)
        {
            this.hasBaseContentType = hasBaseContentType;
        }

        public override bool IsWrapper
        {
            get { return true; }
        }

        public override bool HasBaseContentType
        {
            get { return hasBaseContentType; }
        }

        public ClrTypeReference InnerType
        {
            get { return innerType; }
            set { innerType = value; }
        }

        public string FixedValue
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


        public string DefaultValue
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

    public partial class GroupingInfo : ContentInfo
    {
        ContentModelType contentModelType;
        GroupingFlags groupingFlags;

        public GroupingInfo(ContentModelType cmType, Occurs occursInSchema)
        {
            this.contentModelType = cmType;
            this.occursInSchema = occursInSchema;
            this.contentType = ContentType.Grouping;
            if ((int) occursInSchema > (int) Occurs.ZeroOrOne)
            {
                groupingFlags |= GroupingFlags.Repeating;
            }
        }

        public bool IsRepeating
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

        public bool HasChildGroups
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

        public bool HasRepeatingGroups
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

        public bool IsNested
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

        public bool HasRecurrentElements
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

        public bool IsComplex
        {
            get
            {
                return
                    HasChildGroups | IsRepeating |
                    HasRecurrentElements; //Earlier we used to disable DML for the repeating and nested part of the content model
                //Now, we turn the whole content model to AppendOnly mode
            }
        }

        public ContentModelType ContentModelType
        {
            get { return contentModelType; }
        }

        public override string ToString() => $"{this.contentModelType} {base.ToString()}";
    }
}
