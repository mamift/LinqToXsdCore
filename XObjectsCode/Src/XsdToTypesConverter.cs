//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xml.Schema.Linq.Extensions;
using XObjects;
using XmlSchemaElement = System.Xml.Schema.XmlSchemaElement;

// using Xml.Fxt;

namespace Xml.Schema.Linq.CodeGen
{
    public partial class XsdToTypesConverter
    {
        XmlSchemaSet schemas;
        int schemaErrorCount;
        LinqToXsdSettings configSettings;
        GlobalSymbolTable symbolTable;
        LocalSymbolTable localSymbolTable;
        ClrMappingInfo binding;

        //Particle traversal tables
        Stack<ParticleData> particleStack;
        Dictionary<string, ClrPropertyInfo> propertyNameTypeTable;

        //The data structure keeping track of inheritance relationaship between
        //properties in an object derivation hierary.(Virtual OR Override)
        Dictionary<XmlSchemaType, ClrPropertyInfo> textPropInheritanceTracker;

        Dictionary<XmlQualifiedName, ArrayList> substitutionGroups;

        public XsdToTypesConverter(bool nameMangler2)
            : this(new LinqToXsdSettings(nameMangler2))
        {
        }

        public XsdToTypesConverter(LinqToXsdSettings configSettings)
        {
            this.configSettings = configSettings;
            symbolTable = new GlobalSymbolTable(configSettings);
            localSymbolTable = new LocalSymbolTable();
            binding = new ClrMappingInfo();
            textPropInheritanceTracker = new Dictionary<XmlSchemaType, ClrPropertyInfo>();
            substitutionGroups = new Dictionary<XmlQualifiedName, ArrayList>();
        }

        public ClrMappingInfo GenerateMapping(XmlSchemaSet schemas)
        {
            if (schemas == null)
            {
                throw new ArgumentNullException("schemas");
            }

            schemas.ValidationEventHandler += new ValidationEventHandler(Validationcallback);
            if (!schemas.IsCompiled) schemas.Compile();

            this.schemas = schemas;
            if (schemaErrorCount > 0)
            {
                Console.WriteLine("Schema cannot be compiled. Class generation aborted");
                return null;
            }

            // Execute transformations
            try
            {
                Xml.Fxt.FxtLinq2XsdInterpreter.Run(schemas, configSettings.trafo);
            }
            catch (Xml.Fxt.FxtException)
            {
                Console.WriteLine("Schema cannot be transformed. Class generation aborted");
                return null;
            }

            return GenerateMetaModel();
        }

        private ClrMappingInfo GenerateMetaModel()
        {
            BuildSubstitutionGroups();
            ElementsToTypes();
            AttributesToTypes();
            TypesToTypes();
            binding.NameMappings = symbolTable.schemaNameToIdentifiers;
            return binding;
        }

        internal void BuildSubstitutionGroups()
        {
            foreach (XmlSchemaElement element in schemas.GlobalElements.Values)
            {
                if (!element.SubstitutionGroup.IsEmpty)
                {
                    WalkSubstitutionGroup(element, element);
                }
            }
        }

        private void WalkSubstitutionGroup(XmlSchemaElement element, XmlSchemaElement leafElement)
        {
            //assuming there is no circular reference between substitutionGroups since we already compiled SchemaSet.
            XmlQualifiedName subsName = element.SubstitutionGroup;
            XmlSchemaElement head = schemas.GlobalElements[subsName] as XmlSchemaElement;
            if ((head.Block & XmlSchemaDerivationMethod.Substitution) == 0)
            {
                ArrayList groupMembers = null;
                if (!substitutionGroups.TryGetValue(subsName, out groupMembers))
                {
                    groupMembers = new ArrayList();
                    groupMembers.Add(head);
                    substitutionGroups.Add(subsName, groupMembers);
                }

                groupMembers.Add(leafElement);
            }

            if (!head.SubstitutionGroup.IsEmpty)
                WalkSubstitutionGroup(head, leafElement);
        }

        internal void ElementsToTypes()
        {
            bool isRoot = false;
            int rootElementsCount = schemas.GlobalElements.Count;
            foreach (XmlSchemaElement elem in schemas.GlobalElements.Values)
            {
                SymbolEntry symbol = symbolTable.AddElement(elem);
                XmlSchemaType schemaType = elem.ElementSchemaType;
                string xsdNamespace = elem.QualifiedName.Namespace;

                ClrTypeInfo typeInfo = null;
                XmlSchemaElement headElement = null;
                if (!elem.SubstitutionGroup.IsEmpty)
                {
                    headElement = (XmlSchemaElement) schemas.GlobalElements[elem.SubstitutionGroup];
                }

                if (schemaType.IsGlobal())
                {
                    //Global elem with global type, generate wrapper class for the element
                    bool hasBaseContentType = headElement != null && headElement.ElementSchemaType == schemaType;
                    ClrWrapperTypeInfo wtypeInfo = new ClrWrapperTypeInfo(hasBaseContentType);
                    ClrTypeReference typeDef = BuildTypeReference(schemaType, schemaType.QualifiedName, false, true);
                    //Save the fixed/default value of the element                    
                    wtypeInfo.InnerType = typeDef;
                    typeInfo = wtypeInfo;
                    typeInfo.baseType =
                        headElement; //If element is member of substitutionGroup, add derivation step                    
                }
                else
                {
                    ClrContentTypeInfo ctypeInfo = new ClrContentTypeInfo();
                    localSymbolTable.Init(symbol.identifierName);
                    ctypeInfo.baseType =
                        headElement; //If element is member of substitutionGroup, add derivation step                    
                    BuildProperties(elem, schemaType, ctypeInfo);
                    BuildNestedTypes(ctypeInfo);
                    typeInfo = ctypeInfo;
                }

                if (!isRoot)
                {
                    if (rootElementsCount == 1 || CheckUnhandledAttributes(elem))
                    {
                        typeInfo.IsRoot = true;
                        isRoot = true;
                    }
                }

                typeInfo.IsSubstitutionHead = IsSubstitutionGroupHead(elem) != null;
                typeInfo.IsAbstract = elem.IsAbstract;
                typeInfo.clrtypeName = symbol.identifierName;
                typeInfo.clrtypeNs = symbol.clrNamespace;
                typeInfo.schemaName = symbol.symbolName;
                typeInfo.schemaNs = xsdNamespace;

                typeInfo.typeOrigin = SchemaOrigin.Element;

                BuildAnnotationInformation(typeInfo, schemaType);
                binding.Types.Add(typeInfo);
            }
        }

        internal void AttributesToTypes()
        {
            foreach (XmlSchemaAttribute a in schemas.GlobalAttributes.Values)
            {
                if (
                    a.AttributeSchemaType.QualifiedName.IsEmpty &&
                    a.AttributeSchemaType.IsOrHasUnion())
                {
                    AddSimpleType(a.QualifiedName, a.AttributeSchemaType);
                }
            }
        }

        internal void AddSimpleType(XmlQualifiedName name, XmlSchemaSimpleType simpleType)
        {
            SymbolEntry symbol = symbolTable.AddType(name, simpleType);
            string xsdNamespace = simpleType.QualifiedName.Namespace;
            // Create corresponding simple type info objects
            ClrSimpleTypeInfo typeInfo = ClrSimpleTypeInfo.CreateSimpleTypeInfo(simpleType);
            typeInfo.IsAbstract = false;
            typeInfo.clrtypeName = symbol.identifierName;
            typeInfo.clrtypeNs = symbol.clrNamespace;
            typeInfo.schemaName = symbol.symbolName;
            typeInfo.schemaNs = xsdNamespace;
            typeInfo.typeOrigin = SchemaOrigin.Fragment;
            BuildAnnotationInformation(typeInfo, simpleType);
            binding.Types.Add(typeInfo);
        }

        internal void TypeToType(XmlSchemaType st)
        {
            XmlSchemaSimpleType simpleType = st as XmlSchemaSimpleType;
            if (simpleType != null)
            {
                this.AddSimpleType(simpleType.QualifiedName, simpleType);
            }
            else
            {
                XmlSchemaComplexType ct = st as XmlSchemaComplexType;
                if (ct != null && ct.TypeCode != XmlTypeCode.Item)
                {
                    SymbolEntry symbol = symbolTable.AddType(ct.QualifiedName, ct);
                    string xsdNamespace = ct.QualifiedName.Namespace;

                    localSymbolTable.Init(symbol.identifierName);

                    ClrContentTypeInfo typeInfo = new ClrContentTypeInfo();
                    typeInfo.IsAbstract = ct.IsAbstract;
                    typeInfo.IsSealed = ct.IsFinal();
                    typeInfo.clrtypeName = symbol.identifierName;
                    typeInfo.clrtypeNs = symbol.clrNamespace;
                    typeInfo.schemaName = symbol.symbolName;
                    typeInfo.schemaNs = xsdNamespace;

                    typeInfo.typeOrigin = SchemaOrigin.Fragment;
                    typeInfo.baseType = BaseType(ct);
                    BuildProperties(null, ct, typeInfo);
                    BuildNestedTypes(typeInfo);
                    BuildAnnotationInformation(typeInfo, ct);
                    binding.Types.Add(typeInfo);
                }
            }
        }

        internal void TypesToTypes()
        {
            foreach (XmlSchemaType st in schemas.GlobalTypes.Values)
            {
                TypeToType(st);
            }
        }

        private void BuildProperties(XmlSchemaElement parentElement, XmlSchemaType schemaType,
            ClrContentTypeInfo typeInfo)
        {
            XmlSchemaComplexType ct = schemaType as XmlSchemaComplexType;
            if (ct != null)
            {
                if (ct.TypeCode == XmlTypeCode.Item)
                {
                    return;
                }

                XmlSchemaParticle particleToProperties = ct.ContentTypeParticle;
                XmlSchemaComplexType baseType = ct.BaseXmlSchemaType as XmlSchemaComplexType;
                if (schemaType.GetContentType() == XmlSchemaContentType.TextOnly)
                {
                    //Try to create a text property for the simple content
                    ClrPropertyInfo property = BuildComplexTypeTextProperty(parentElement, ct);
                    if (property != null)
                    {
                        typeInfo.AddMember(property);
                    }

                    if (baseType == null)
                    {
                        //Derived from Simple type, first step simpleContent extension
                        TraverseAttributes(ct.AttributeUses, typeInfo);
                    }
                    else
                    {
                        //Derived from another complex type simple content, generate the content only if there are additional restrictions
                        if (!ct.IsDerivedByRestriction()) //process attributes only if not derived by restriction
                            TraverseAttributes(ct.AttributeUses, baseType.AttributeUses, typeInfo);
                    }
                }
                else
                {
                    Debug.Assert(
                        baseType != null); //ComplexType with complexContent is always derived from another complexType
                    if (ct.IsDerivedByRestriction())
                    {
                        //Do not handle restrictions on complex content?
                        return;
                    }

                    if (particleToProperties.GetParticleType() != ParticleType.Empty)
                    {
                        TraverseParticle(particleToProperties, baseType, typeInfo, ct.DerivedBy);
                    }

                    TraverseAttributes(ct.AttributeUses, baseType.AttributeUses, typeInfo);
                }
            }
            else
            {
                typeInfo.AddMember(BuildSimpleTypeTextProperty(parentElement, schemaType as XmlSchemaSimpleType));
            }
        }


        private void BuildNestedTypes(ClrContentTypeInfo typeInfo)
        {
            List<AnonymousType> anonymousTypes = localSymbolTable.GetAnonymousTypes();
            foreach (AnonymousType at in anonymousTypes)
            {
                XmlQualifiedName qname = null;
                XmlSchemaComplexType complexType = null;

                XmlSchemaElement elem = at.parentElement;
                if (elem == null)
                {
                    //case 1: "dummy" type for text content in a complex type, with restrictions
                    complexType = at.wrappingType;
                    qname = complexType.QualifiedName;
                }
                else
                {
                    //case 2: anonymous type can also be nested under an element
                    qname = elem.QualifiedName;
                    complexType = elem.ElementSchemaType as XmlSchemaComplexType;
                }

                if (complexType != null)
                {
                    if (complexType.GetContentType() == XmlSchemaContentType.TextOnly
                        && complexType.IsDerivedByRestriction())
                    {
                        //In this case, we take care of the content/text part only. No nesting types exist.
                        ClrSimpleTypeInfo
                            nestedTypeInfo =
                                ClrSimpleTypeInfo
                                    .CreateSimpleTypeInfo(
                                        complexType); //Generate its "simple type" version to save restrictions
                        nestedTypeInfo.clrtypeName = at.identifier;
                        nestedTypeInfo.clrtypeNs = configSettings.GetClrNamespace(qname.Namespace);
                        nestedTypeInfo.schemaName = qname.Name;
                        nestedTypeInfo.schemaNs = qname.Namespace;
                        nestedTypeInfo.typeOrigin = SchemaOrigin.Fragment;
                        nestedTypeInfo.IsNested = true;
                        BuildAnnotationInformation(nestedTypeInfo, complexType);
                        typeInfo.NestedTypes.Add(nestedTypeInfo);
                    }
                    else
                    {
                        ClrContentTypeInfo nestedTypeInfo = new ClrContentTypeInfo() {
                            Parent = typeInfo
                        };
                        localSymbolTable.Init(at.identifier);
                        nestedTypeInfo.clrtypeName = at.identifier;
                        nestedTypeInfo.clrtypeNs = configSettings.GetClrNamespace(qname.Namespace);
                        nestedTypeInfo.schemaName = qname.Name;
                        nestedTypeInfo.schemaNs = qname.Namespace;
                        nestedTypeInfo.typeOrigin = SchemaOrigin.Fragment;
                        nestedTypeInfo.IsNested = true;
                        nestedTypeInfo.baseType = BaseType(complexType);
                        BuildProperties(elem, complexType, nestedTypeInfo);
                        BuildNestedTypes(nestedTypeInfo);
                        BuildAnnotationInformation(nestedTypeInfo, complexType);
                        typeInfo.NestedTypes.Add(nestedTypeInfo);
                    }
                }

                //Also handle simple types
                XmlSchemaSimpleType simpleType = null;
                if (elem != null) simpleType = elem.ElementSchemaType as XmlSchemaSimpleType;

                if (simpleType != null)
                {
                    ClrSimpleTypeInfo nestedTypeInfo = ClrSimpleTypeInfo.CreateSimpleTypeInfo(simpleType);
                    nestedTypeInfo.clrtypeName = at.identifier;
                    nestedTypeInfo.clrtypeNs = configSettings.GetClrNamespace(qname.Namespace);
                    nestedTypeInfo.schemaName = qname.Name;
                    nestedTypeInfo.schemaNs = qname.Namespace;
                    nestedTypeInfo.typeOrigin = SchemaOrigin.Fragment;
                    nestedTypeInfo.IsNested = true;
                    BuildAnnotationInformation(nestedTypeInfo, simpleType);
                    typeInfo.NestedTypes.Add(nestedTypeInfo);
                }
            }
        }


        private void AppendMessage(List<ClrAnnotation> annotations, string section, string message)
        {
            Debug.Assert(message.Length != 0 && section.Length != 0);
            ClrAnnotation clrAnn = new ClrAnnotation();
            clrAnn.Section = section;
            clrAnn.Text = message;
            annotations.Add(clrAnn);
        }

        private void AppendXsdDocumentationInformation(List<ClrAnnotation> annotations, XmlSchemaObject schemaObject)
        {
            XmlSchemaAnnotated annotatedObject = schemaObject as XmlSchemaAnnotated;

            if (annotatedObject != null &&
                annotatedObject.Annotation != null)
            {
                XmlNode[] markup;
                foreach (XmlSchemaObject annot in annotatedObject.Annotation.Items)
                {
                    XmlSchemaDocumentation doc = annot as XmlSchemaDocumentation;
                    if (doc != null)
                    {
                        markup = doc.Markup;
                    }
                    else
                    {
                        markup = ((XmlSchemaAppInfo) annot).Markup;
                    }

                    string text = String.Empty;
                    foreach (XmlNode xn in markup)
                    {
                        text += xn.InnerText;
                    }

                    if (text.Length > 0)
                    {
                        AppendMessage(annotations, "summary", text);
                    }
                }
            }
        }

        private void AppendCardinalityInformation(List<ClrAnnotation> annotations,
            ClrBasePropertyInfo basePropertyInfo,
            XmlSchemaObject schemaObject,
            bool isInChoice,
            bool isInNestedGroup)
        {
            ClrPropertyInfo propertyInfo = basePropertyInfo as ClrPropertyInfo;

            string text = String.Empty;

            text += "Occurrence: ";

            if (propertyInfo.IsOptional)
            {
                text += "optional";
            }
            else
            {
                text += "required";
            }


            if (propertyInfo.IsStar ||
                propertyInfo.IsPlus)
            {
                text += ", repeating";
            }

            if (isInChoice)
            {
                text += ", choice";
            }

            // Append the occurrence message
            AppendMessage(annotations, "summary", text);

            if (isInNestedGroup)
            {
                AppendMessage(annotations, "summary", "Setter: Appends");
            }

            if (propertyInfo.IsSubstitutionHead)
            {
                bool fComma = false;
                text = "Substitution members: ";
                foreach (XmlSchemaElement xse in propertyInfo.SubstitutionMembers)
                {
                    if (!fComma)
                    {
                        fComma = true;
                    }
                    else
                    {
                        text += ", ";
                    }

                    text += xse.Name;
                }

                AppendMessage(annotations, "summary", text);
            }
        }

        private void AppendRegExInformation(ClrTypeInfo typeInfo)
        {
            if (typeInfo.ContentModelRegEx != null &&
                typeInfo.ContentModelRegEx.Length > 0)
            {
                string text = "Regular expression: " + typeInfo.ContentModelRegEx;
                AppendMessage(typeInfo.Annotations, "summaryRegEx", text);
            }
        }

        private void BuildAnnotationInformation(ClrTypeInfo typeInfo, XmlSchemaObject schemaObject)
        {
            AppendXsdDocumentationInformation(typeInfo.Annotations, schemaObject);
            AppendRegExInformation(typeInfo);
        }

        private void BuildAnnotationInformation(ClrBasePropertyInfo propertyInfo, XmlSchemaObject schemaObject,
            bool isInChoice, bool isInNestedGroup)
        {
            AppendXsdDocumentationInformation(propertyInfo.Annotations, schemaObject);
            AppendCardinalityInformation(propertyInfo.Annotations, propertyInfo, schemaObject, isInChoice,
                isInNestedGroup);
        }

        private struct ParticleData
        {
            public ParticleData(XmlSchemaGroupBase groupBase, GroupingInfo groupInfo, int index)
            {
                GroupBase    = groupBase;
                GroupingInfo = groupInfo;
                Index        = index;
            }
            public XmlSchemaGroupBase   GroupBase    { get; }
            public GroupingInfo         GroupingInfo { get; }
            public int                  Index        { get; }

            public override string      ToString() => $"{this.GroupingInfo}, {this.Index}";
        }

        private void TraverseParticle(XmlSchemaParticle particle, XmlSchemaComplexType baseType,
            ClrContentTypeInfo typeInfo, XmlSchemaDerivationMethod derivationMethod)
        {
            if (particleStack == null)
            {
                particleStack = new Stack<ParticleData>();
            }
            else
            {
                particleStack.Clear();
            }

            if (propertyNameTypeTable == null)
            {
                propertyNameTypeTable = new Dictionary<string, ClrPropertyInfo>();
            }
            else
            {
                propertyNameTypeTable.Clear();
            }

            StringBuilder     regEx        = new StringBuilder();
            XmlSchemaParticle baseParticle = baseType.ContentTypeParticle;
            ParticleData      particleData;
            GroupingInfo      parentGroupInfo;

            XmlSchemaGroupBase currentGroupBase    = null;
            GroupingInfo       currentGroupingInfo = null;
            int                currentIndex        = 0;

            while (true)
            {
                // dont interrogate a particle if we are past the end of the list
                if (currentGroupBase == null || currentIndex <= currentGroupBase.Items.Count)
                {
                    ParticleType particleType = particle.GetParticleType();
                    switch (particleType)
                    {
                        case ParticleType.Element:
                        {
                            XmlSchemaElement elem = particle as XmlSchemaElement;
                            bool fromBaseType = false;
                            if (derivationMethod == XmlSchemaDerivationMethod.Extension && typeInfo.IsDerived)
                            {
                                if (baseParticle.ContainsElement(elem))
                                {
                                    fromBaseType = true;
                                }
                                else if (!typeInfo.InlineBaseType && baseType.ContainsName(elem.QualifiedName))
                                {
                                    typeInfo.InlineBaseType = true;
                                }
                            }

                            ClrPropertyInfo propertyInfo = BuildPropertyForElement(elem, fromBaseType);
                            regEx.Append(propertyInfo.PropertyName);
                            regEx.Append(propertyInfo.OccurenceString);
                            //Add to parent
                            if (currentGroupingInfo == null)
                            {
                                //Not adding property to propertyNameTypeTable as this case will occur only for pointless groups, so they have just one property
                                BuildAnnotationInformation(propertyInfo, elem, false, false);
                                typeInfo.AddMember(propertyInfo);
                            }
                            else
                            {
                                BuildAnnotationInformation(propertyInfo, elem,
                                    currentGroupingInfo.ContentModelType == ContentModelType.Choice,
                                    currentGroupingInfo.IsNested);
                                currentGroupingInfo.AddChild(propertyInfo);
                                SetPropertyFlags(propertyInfo, currentGroupingInfo, elem.ElementSchemaType);
                            }

                            break;
                        }
                        case ParticleType.Any:
                        {
                            regEx.Append("any");
                            XmlSchemaAny any = particle as XmlSchemaAny;

                            if (derivationMethod == XmlSchemaDerivationMethod.Extension && typeInfo.IsDerived)
                            {
                                if (baseParticle.ContainsWildCard(any))
                                {
                                    typeInfo.HasElementWildCard = true; //ANY property in the base type will be reused
                                }
                            }

                            //Note we always create a property info object to keep the original nesting structure in the schema
                            //so it can be used to create a correct FSM; on the other hand, typeInfo.HasElementWildCard will indicate whether
                            //we need to create a property in the resulting object type.
                            ClrWildCardPropertyInfo wcPropertyInfo =
                                BuildAnyProperty(any, !typeInfo.HasElementWildCard);


                            //Add to parent
                            if (currentGroupingInfo == null)
                            {
                                typeInfo.AddMember(wcPropertyInfo);
                            }
                            else
                            {
                                currentGroupingInfo.AddChild(wcPropertyInfo);
                            }

                            if (!typeInfo.HasElementWildCard) typeInfo.HasElementWildCard = true;
                            break;
                        }

                        case ParticleType.Sequence:
                        case ParticleType.Choice:
                        case ParticleType.All:
                            regEx.Append("(");
                            if (currentGroupBase != null)
                            {
                                //already there is a group that we are processing, push it on stack to process sub-group
                                particleStack.Push(
                                    new ParticleData(currentGroupBase, currentGroupingInfo, currentIndex));
                                currentIndex = 0; //Re-start index for new group base
                            }

                            parentGroupInfo = currentGroupingInfo; //Assign parent before creating child groupInfo
                            currentGroupBase = particle as XmlSchemaGroupBase;
                            Debug.Assert(currentGroupBase != null);
                            currentGroupingInfo = new GroupingInfo((ContentModelType) ((int) particleType),
                                GetOccurence(currentGroupBase));

                            //Add to parent
                            if (parentGroupInfo == null)
                            {
                                typeInfo.AddMember(currentGroupingInfo);
                            }
                            else
                            {
                                parentGroupInfo.AddChild(currentGroupingInfo);
                                parentGroupInfo.HasChildGroups = true;
                                currentGroupingInfo.IsNested = true;
                                if (parentGroupInfo.IsRepeating)
                                {
                                    currentGroupingInfo.IsRepeating = true;
                                }

                                if (currentGroupingInfo.IsRepeating)
                                {
                                    parentGroupInfo.HasRepeatingGroups = true;
                                }
                            }

                            break;
                    }
                }

                //Drill down into items
                if (currentGroupBase != null && currentIndex < currentGroupBase.Items.Count)
                {
                    // if this isnt the first, then we need a seperator
                    if (currentIndex > 0)
                    {
                        regEx.Append(currentGroupingInfo.ContentModelType == ContentModelType.Choice ? " | " : ", ");
                    }

                    particle = (XmlSchemaParticle) currentGroupBase.Items[currentIndex++];
                }
                else
                {
                    if (currentGroupBase != null)
                    {
                        regEx.Append(')');
                        regEx.Append(currentGroupingInfo.OccurenceString);
                    }

                    if (particleStack.Count > 0)
                    {
                        bool childGroupHasRecurringElements = currentGroupingInfo.HasRecurrentElements;
                        bool childGroupHasRepeatingGroups = currentGroupingInfo.HasRepeatingGroups;

                        particleData = particleStack.Pop();
                        currentGroupBase = particleData.GroupBase;
                        currentGroupingInfo = particleData.GroupingInfo;

                        currentGroupingInfo.HasRecurrentElements = childGroupHasRecurringElements;
                        currentGroupingInfo.HasRepeatingGroups = childGroupHasRepeatingGroups;

                        currentIndex = particleData.Index;
                        if (currentIndex < currentGroupBase.Items.Count)
                        {
                            particle = (XmlSchemaParticle) currentGroupBase.Items[currentIndex++];
                            regEx.Append(currentGroupingInfo.ContentModelType == ContentModelType.Choice ? "|" : ", ");
                        }
                        else
                        {
                            // we were already at the end of the parent group, so just continue
                            currentIndex++; //we are off the end of this list
                        }
                    }
                    else
                    {
                        //No more particles to process
                        break;
                    }
                }
            }

            if (regEx.Length != 0)
            {
                typeInfo.ContentModelRegEx = regEx.ToString();
            }
        }

        private void SetPropertyFlags(ClrPropertyInfo propertyInfo, GroupingInfo currentGroupingInfo,
            XmlSchemaType propertyType)
        {
            propertyInfo.IsNullable |= currentGroupingInfo.ContentModelType == ContentModelType.Choice ||
                                       currentGroupingInfo.IsOptional;
            propertyInfo.VerifyRequired = configSettings.VerifyRequired;

            if (currentGroupingInfo.IsRepeating)
            {
                propertyInfo.IsList = true;
            }

            string propertyName = propertyInfo.PropertyName;
            ClrPropertyInfo prevProperty = null;
            if (propertyNameTypeTable.TryGetValue(propertyName, out prevProperty))
            {
                currentGroupingInfo.HasRecurrentElements = true;
                propertyInfo.IsDuplicate = true;
                prevProperty.IsList = true; //Change the first one to list
            }
            else
            {
                propertyNameTypeTable.Add(propertyName, propertyInfo);
            }
        }

        private void TraverseAttributes(XmlSchemaObjectTable derivedAttributes, ClrContentTypeInfo typeInfo)
        {
            foreach (XmlSchemaAttribute derivedAttribute in derivedAttributes.Values)
            {
                Debug.Assert(derivedAttribute.AttributeSchemaType !=
                             null); //For use=prohibited, without derivation it doesnt mean anything, hence attribute should be compiled
                ClrBasePropertyInfo propertyInfo = BuildProperty(derivedAttribute, false, false);
                BuildAnnotationInformation(propertyInfo, derivedAttribute, false, false);
                typeInfo.AddMember(propertyInfo);
            }
        }

        private void TraverseAttributes(XmlSchemaObjectTable derivedAttributes, XmlSchemaObjectTable baseAttributes,
            ClrContentTypeInfo typeInfo)
        {
            foreach (XmlSchemaAttribute derivedAttribute in derivedAttributes.Values)
            {
                if (derivedAttribute.Use == XmlSchemaUse.Prohibited)
                {
                    continue;
                }

                XmlSchemaAttribute baseAttribute = baseAttributes[derivedAttribute.QualifiedName] as XmlSchemaAttribute;
                if (baseAttribute != null && baseAttribute == derivedAttribute)
                {
                    // Its the one copied from the base
                    // http://linqtoxsd.codeplex.com/WorkItem/View.aspx?WorkItemId=3064
                    ClrBasePropertyInfo propertyInfo = BuildProperty(derivedAttribute, typeInfo.IsDerived, false, typeInfo);
                    BuildAnnotationInformation(propertyInfo, derivedAttribute, false, false);
                    typeInfo.AddMember(propertyInfo);
                }
                else
                {
                    ClrBasePropertyInfo propertyInfo = BuildProperty(derivedAttribute, false, baseAttribute != null, typeInfo);
                    BuildAnnotationInformation(propertyInfo, derivedAttribute, false, false);
                    typeInfo.AddMember(propertyInfo);
                }
            }
        }

        private XmlSchemaType BaseType(XmlSchemaComplexType ct)
        {
            XmlSchemaType baseType = ct.BaseXmlSchemaType;
            XmlQualifiedName baseTypeName = baseType.QualifiedName;
            if (baseType.TypeCode == XmlTypeCode.Item || baseTypeName.Equals(ct.QualifiedName) ||
                baseType is XmlSchemaSimpleType)
            {
                //Dont add inheritance hierarchy if deriving from schema simple types or anyType
                return null;
            }

            return baseType;
        }

        private ClrPropertyInfo BuildComplexTypeTextProperty(XmlSchemaElement parentElement,
            XmlSchemaComplexType schemaType)
        {
            Debug.Assert(schemaType != null);
            Debug.Assert(schemaType.GetContentType() == XmlSchemaContentType.TextOnly);

            ClrPropertyInfo textProperty = new ClrPropertyInfo(Constants.SInnerTypePropertyName, string.Empty,
                Constants.SInnerTypePropertyName, Occurs.One, configSettings);

            textProperty.Origin = SchemaOrigin.Text;
            ClrTypeReference typeRef = null;
            bool anonymous = false;

            //Could be derived by restriction or extension
            //If first time extension, make the base simple type as the type reference
            XmlSchemaType baseType = schemaType.BaseXmlSchemaType;
            if (baseType is XmlSchemaSimpleType)
            {
                typeRef = BuildTypeReference(baseType, baseType.QualifiedName, false, true);
                anonymous = false;
                if (!textPropInheritanceTracker.ContainsKey(schemaType))
                {
                    textPropInheritanceTracker.Add(schemaType, textProperty);
                }
            }
            else if (schemaType.HasFacetRestrictions())
            {
                //Derived by restriction, represents the content type with restrictions as a local type
                //Make the base simple type as the type reference so that we know if it is a list, union or atomic
                XmlSchemaSimpleType st = schemaType.GetBaseSimpleType();
                Debug.Assert(st != null);
                typeRef = BuildTypeReference(st, st.QualifiedName, true, true);
                typeRef.Validate = true;
                anonymous = true;

                //Also get its base complex type and see if we need to override the content property
                ClrPropertyInfo baseProp = null;
                if (textPropInheritanceTracker.TryGetValue(baseType, out baseProp))
                {
                    textProperty.IsOverride = true;
                    if (!baseProp.IsOverride)
                    {
                        baseProp.IsVirtual = true;
                    }
                }
            }
            else
            {
                return null;
            }

            if (anonymous)
            {
                //anonymous type, fixed up the name later, treat complex type with restrictions as an anonymous type
                //because we need to generate a type to encapsualte these restrictions
                if (parentElement != null)
                {
                    string identifier = localSymbolTable.AddLocalElement(parentElement);
                    localSymbolTable.AddAnonymousType(identifier, parentElement, typeRef);
                }
                else
                {
                    localSymbolTable.AddComplexRestrictedContentType(schemaType, typeRef);
                }
            }

            textProperty.TypeReference = typeRef;

            return textProperty;
        }

        private ClrPropertyInfo BuildSimpleTypeTextProperty(XmlSchemaElement parentElement,
            XmlSchemaSimpleType schemaType)
        {
            Debug.Assert(schemaType != null);
            ClrPropertyInfo textProperty = new ClrPropertyInfo(Constants.SInnerTypePropertyName, string.Empty,
                Constants.SInnerTypePropertyName, Occurs.One, configSettings);
            textProperty.Origin = SchemaOrigin.Text;

            bool anonymous = schemaType.QualifiedName.IsEmpty;
            ClrTypeReference typeRef = BuildTypeReference(schemaType, schemaType.QualifiedName, anonymous, true);
            textProperty.TypeReference = typeRef;

            if (anonymous && parentElement != null)
            {
                //anonymous type, fixed up the name later
                string idenfitier = localSymbolTable.AddLocalElement(parentElement);
                localSymbolTable.AddAnonymousType(idenfitier, parentElement, typeRef);
            }

            return textProperty;
        }


        private ClrPropertyInfo BuildPropertyForElement(XmlSchemaElement elem, bool fromBaseType)
        {
            string identifierName = localSymbolTable.AddLocalElement(elem);

            XmlSchemaType schemaType = elem.ElementSchemaType;
            XmlQualifiedName schemaTypeName = schemaType.QualifiedName;
            string schemaName = elem.QualifiedName.Name;
            string schemaNs = elem.QualifiedName.Namespace;
            string clrNs = elem.FormResolved() == XmlSchemaForm.Qualified
                ? configSettings.GetClrNamespace(schemaNs)
                : string.Empty;


            SchemaOrigin typeRefOrigin = SchemaOrigin.Fragment;
            bool isTypeRef = false;
            //Anonymous types have a non null XmlSchemaElement.SchemaType value
            bool isAnonymous = elem.SchemaType != null;
            XmlSchemaObject schemaObject = schemaType;

            ArrayList substitutionMembers = null;
            if (elem.IsGlobal())
            {
                substitutionMembers = IsSubstitutionGroupHead(elem);
                schemaTypeName = elem.QualifiedName;
                isTypeRef = true;
                typeRefOrigin = SchemaOrigin.Element;
                schemaObject =
                    schemas.GlobalElements
                        [schemaTypeName]; //For ref, get the element decl SOM object, as nameMappings are keyed off the SOM object
                isAnonymous = false;
            }

            ClrTypeReference typeRef = BuildTypeReference(schemaObject, schemaTypeName, isAnonymous, true);
            typeRef.Origin = typeRefOrigin;
            typeRef.IsTypeRef = isTypeRef;
            if (isAnonymous && !fromBaseType)
            {
                //to fixup later.
                localSymbolTable.AddAnonymousType(identifierName, elem, typeRef);
            }

            ClrPropertyInfo propertyInfo =
                new ClrPropertyInfo(identifierName, schemaNs, schemaName, GetOccurence(elem), configSettings);
            propertyInfo.Origin = SchemaOrigin.Element;
            propertyInfo.FromBaseType = fromBaseType;
            propertyInfo.TypeReference = typeRef;
            propertyInfo.ClrNamespace = clrNs;

            //SetFixedDefaultValue(elem, propertyInfo);

            if (substitutionMembers != null)
            {
                propertyInfo.SubstitutionMembers = substitutionMembers;
            }

            //BuildAnnotationInformation(propertyInfo, elem);
            return propertyInfo;
            //Place it in the element's namespace, maybe element's parent type's namespace?
        }

        private ClrPropertyInfo BuildProperty(XmlSchemaAttribute attribute, bool fromBaseType, bool isNew,
            ClrTypeInfo containingType = null)
        {
            string identifierName = localSymbolTable.AddAttribute(attribute);

            XmlSchemaType schemaType = attribute.AttributeSchemaType;
            XmlQualifiedName schemaTypeName = schemaType.QualifiedName;
            string schemaName = attribute.QualifiedName.Name;
            string schemaNs = attribute.QualifiedName.Namespace;
            string clrNs = attribute.FormResolved() == XmlSchemaForm.Qualified
                ? configSettings.GetClrNamespace(schemaNs)
                : string.Empty;

            SchemaOrigin typeRefOrigin = SchemaOrigin.Fragment;
            bool isTypeRef = false;
            //Anonymous types have a non null XmlSchemaAttribute.SchemaType value
            bool isAnonymous = attribute.SchemaType != null; //|| attribute.SchemaTypeName.IsEmpty;
            XmlSchemaObject schemaObject = schemaType;

            ClrTypeReference typeRef = BuildTypeReference(schemaObject, schemaTypeName, isAnonymous, true);
            typeRef.Origin = typeRefOrigin;
            typeRef.IsTypeRef = isTypeRef;

            ClrPropertyInfo propertyInfo = new ClrPropertyInfo(identifierName, schemaNs, schemaName, GetOccurence(attribute), configSettings);
            propertyInfo.Origin = SchemaOrigin.Attribute;
            propertyInfo.FromBaseType = fromBaseType;
            propertyInfo.TypeReference = typeRef;
            propertyInfo.ClrNamespace = clrNs;
            propertyInfo.IsNew = isNew;
            propertyInfo.VerifyRequired = configSettings.VerifyRequired;

            SetFixedDefaultValue(attribute, propertyInfo);
            return propertyInfo;
        }

        private ClrPropertyInfo BuildPropertyForAttribute(XmlSchemaAttribute attribute, bool fromBaseType, bool isNew, ClrTypeInfo containingType = null)
        {
            string schemaName = attribute.QualifiedName.Name;
            string schemaNs = attribute.QualifiedName.Namespace;

            string propertyName = localSymbolTable.AddAttribute(attribute);
            ClrPropertyInfo propertyInfo = new ClrPropertyInfo(propertyName, schemaNs, schemaName, GetOccurence(attribute), configSettings);
            propertyInfo.Origin = SchemaOrigin.Attribute;
            propertyInfo.FromBaseType = fromBaseType;
            propertyInfo.IsNew = isNew;
            propertyInfo.VerifyRequired = configSettings.VerifyRequired;

            XmlSchemaSimpleType schemaType = attribute.AttributeSchemaType;
            var isInlineEnum = attribute.AttributeSchemaType.IsEnum() && attribute.AttributeSchemaType.IsDerivedByRestriction() &&
                                        ((attribute.AttributeSchemaType.Content as XmlSchemaSimpleTypeRestriction)?.Facets
                                         .Cast<XmlSchemaObject>().Any() ?? false);
            var isAnonymous = !attribute.AttributeSchemaType.IsGlobal() &&
                               !attribute.AttributeSchemaType.IsBuiltInSimpleType();

            var qName = schemaType.QualifiedName;
            if (qName.IsEmpty) qName = attribute.QualifiedName;

            ClrTypeReference typeRef = BuildTypeReference(schemaType, qName, isAnonymous, true);
            if (isInlineEnum && isAnonymous) {
                UpdateTypeRefForInlineAnonymousEnum(attribute, containingType, typeRef, propertyInfo);
            }
            propertyInfo.TypeReference = typeRef;
            Debug.Assert(schemaType.Datatype != null);
            SetFixedDefaultValue(attribute, propertyInfo);
            return propertyInfo;
        }

        private void UpdateTypeRefForInlineAnonymousEnum(XmlSchemaAttribute attribute, ClrTypeInfo containingType,
            ClrTypeReference typeRef, ClrPropertyInfo propertyInfo)
        {
            typeRef.Name += "Enum";
            if (typeRef.ClrFullTypeName.IsNullOrEmpty()) {
                var typeScopedResolutionString = containingType?.GetNestedTypeScopedResolutionString();
                if (typeScopedResolutionString.IsNullOrEmpty()) {
                    // if this is empty, then take the referencing element
                    var closestNamedParent = attribute.GetClosestNamedParent().GetPotentialName();
                    typeScopedResolutionString = closestNamedParent;
                }

                typeRef.UpdateClrFullEnumTypeName(propertyInfo, null, typeScopedResolutionString);
            }
        }

        private ClrWildCardPropertyInfo BuildAnyProperty(XmlSchemaAny any, bool addToTypeDef)
        {
            ClrWildCardPropertyInfo property =
                new ClrWildCardPropertyInfo(any.Namespace, any.GetTargetNS(), addToTypeDef, GetOccurence(any));
            property.PropertyName = Constants.Any;

            return property;
        }

        private ClrTypeReference BuildTypeReference(XmlSchemaObject schemaObject, XmlQualifiedName typeQName,
            bool anonymousType, bool setVariety)
        {
            string typeName = typeQName.Name;
            string typeNs = typeQName.Namespace;
            if (!anonymousType)
            {
                typeNs = configSettings.GetClrNamespace(typeNs);
            }

            ClrTypeReference typeRef = new ClrTypeReference(typeName, typeNs, schemaObject, anonymousType, setVariety);
            return typeRef;
        }

        private bool CheckUnhandledAttributes(XmlSchemaAnnotated annotated)
        {
            if (annotated.UnhandledAttributes != null)
            {
                foreach (XmlAttribute att in annotated.UnhandledAttributes)
                {
                    if (att.LocalName == "root" && att.NamespaceURI == Constants.TypedXLinqNs)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Occurs GetOccurence(XmlSchemaParticle particle)
        {
            if (particle.MinOccurs == 1)
            {
                if (particle.MaxOccurs == 1)
                {
                    return Occurs.One;
                }
                else
                {
                    //maxOccurs cannot be 0
                    return Occurs.OneOrMore;
                }
            }
            else if (particle.MinOccurs == 0)
            {
                if (particle.MaxOccurs == 1)
                {
                    return Occurs.ZeroOrOne;
                }
                else
                {
                    //maxOccurs cannot be 0
                    return Occurs.ZeroOrMore;
                }
            }
            else
            {
                Debug.Assert(particle.MinOccurs > 1);
                return Occurs.OneOrMore;
            }
        }

        private Occurs GetOccurence(XmlSchemaAttribute attribute)
        {
            return attribute.Use == XmlSchemaUse.Required ? Occurs.One : Occurs.ZeroOrOne;
        }

        private ArrayList IsSubstitutionGroupHead(XmlSchemaElement element)
        {
            XmlQualifiedName elementName = element.QualifiedName;
            ArrayList memberList;
            substitutionGroups.TryGetValue(elementName, out memberList);
            return memberList;
        }

        private void Validationcallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                schemaErrorCount++;
            }

            Console.WriteLine("Exception Severity: " + args.Severity);
            Console.WriteLine(args.Message);
        }

        private void SetFixedDefaultValue(XmlSchemaAttribute attribute, ClrPropertyInfo propertyInfo)
        {
            //saves fixed/default value in the corresponding property
            //Currently only consider fixed/default values for simple types
            if (attribute.RefName != null && !attribute.RefName.IsEmpty)
            {
                XmlSchemaAttribute globalAtt = (XmlSchemaAttribute) this.schemas.GlobalAttributes[attribute.RefName];
                propertyInfo.FixedValue = globalAtt.FixedValue;
                propertyInfo.DefaultValue = globalAtt.DefaultValue;
            }
            else
            {
                propertyInfo.FixedValue = attribute.FixedValue;
                propertyInfo.DefaultValue = attribute.DefaultValue;
            }

            if (attribute.AttributeSchemaType.DerivedBy == XmlSchemaDerivationMethod.Union)
            {
                string value = propertyInfo.FixedValue;
                if (value == null)
                    value = propertyInfo.DefaultValue;
                if (value != null)
                {
                    propertyInfo.unionDefaultType = attribute
                                                    .AttributeSchemaType.Datatype
                                                    .ParseValue(value, new NameTable(), null).GetType();
                }
            }
        }
    }
}