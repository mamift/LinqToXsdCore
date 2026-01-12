#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Xml.Fxt;
using Xml.Schema.Linq;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;
using OneOf;

namespace XObjects
{
    using XElementOrAttrOrQName = OneOf<XmlSchemaElement, XmlSchemaAttribute, XmlQualifiedName>;
    
    public static class XmlSchemaExtensions
    {
        /// <summary>
        /// Goes through the current <see cref="XmlSchema"/> and retrieves from all element and attribute definitions, any anonymously defined simple types.
        /// <para>Because these types are anonymous this method traverses the entire schema from top to bottom, inspecting the type of all elements and attributes.</para>
        /// </summary>
        /// <param name="schemaSet"></param>
        /// 
        public static Dictionary<XElementOrAttrOrQName, XmlSchemaSimpleType> RetrieveAllAnonymousSimpleTypes(this XmlSchemaSet schemaSet)
        {
            var simpleTypes = schemaSet.RetrieveAllSimpleTypes();

            return simpleTypes.Where(st => st.Value.Name.IsEmpty())
                .ToDictionary(k => k.Key, v => v.Value);
        }

        public static Dictionary<XElementOrAttrOrQName, XmlSchemaSimpleType> RetrieveAllAnonymousSimpleUnionTypes(this XmlSchemaSet schemaSet)
        {
            var simpleTypes = schemaSet.RetrieveAllAnonymousSimpleTypes();

            return simpleTypes.Where(st => st.Value.Content is XmlSchemaSimpleTypeUnion || st.Value.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
                .ToDictionary(k => k.Key, v => v.Value);
        }

        public static Dictionary<XElementOrAttrOrQName, XmlSchemaSimpleType> RetrieveAllSimpleTypes(this XmlSchemaSet schemaset)
        {
            var simpleTypesDictionary = new Dictionary<XElementOrAttrOrQName, XmlSchemaSimpleType>();

            IEnumerable<XmlSchemaObject> allAttrsAndElements = schemaset.GlobalElements.Values.Cast<XmlSchemaObject>()
                .Concat(schemaset.GlobalAttributes.Values.Cast<XmlSchemaObject>())
                .Concat(schemaset.GlobalTypes.Values.Cast<XmlSchemaObject>());

            foreach (var item in allAttrsAndElements) {
                TraverseAllSimpleTypes(item, ref simpleTypesDictionary, out _);
            }

            return simpleTypesDictionary;
        }

        private static void TraverseAllSimpleTypes(XmlSchemaObject schemaObject, ref Dictionary<XElementOrAttrOrQName, XmlSchemaSimpleType> simpleTypes, out bool breakOutOfLoop)
        {
            if (simpleTypes == null) throw new ArgumentNullException(nameof(simpleTypes));

            bool didAdd = true;
            switch (schemaObject) {
                case XmlSchemaAttribute attribute: {
                    didAdd = simpleTypes.AddIfNotAlreadyExists(attribute, attribute.AttributeSchemaType);
                    break;
                }

                case XmlSchemaElement element: {
                        if (element.ElementSchemaType is XmlSchemaSimpleType simpleType) {
                            didAdd = simpleTypes.AddIfNotAlreadyExists(element, simpleType);
                        } 
                        else if (element.ElementSchemaType is XmlSchemaComplexType complexType) {
                            foreach (var attr in complexType.AttributeUses.Values.Cast<XmlSchemaAttribute>()) {
                                TraverseAllSimpleTypes(attr, ref simpleTypes, out breakOutOfLoop);
                                if (breakOutOfLoop) return;
                            }

                            var childElements = complexType.LocalXsdElements()
                                .Where(e => !ReferenceEquals(e, schemaObject))
                                .ToList();
                            
                            foreach (var el in childElements) {
                                TraverseAllSimpleTypes(el, ref simpleTypes, out breakOutOfLoop);
                                if (breakOutOfLoop) return;
                            }
                        }
                        else {
                            if (element.ElementSchemaType is XmlSchemaComplexType { Particle: XmlSchemaGroupBase gb }) {
                                foreach (var particle in gb.Items) {
                                    TraverseAllSimpleTypes(particle, ref simpleTypes, out breakOutOfLoop);
                                    if (breakOutOfLoop) return;
                                }
                            }
                        }

                        break;
                    }

                case XmlSchemaComplexType complexType: {
                    foreach (var attribute in complexType.AttributeUses.Values.OfType<XmlSchemaAttribute>()) {
                        if (attribute.AttributeSchemaType is { } simpleType) {
                            didAdd = simpleTypes.AddIfNotAlreadyExists(attribute, simpleType);
                        }
                    }

                    if (complexType.ContentTypeParticle != null) {
                        TraverseAllSimpleTypes(complexType.ContentTypeParticle, ref simpleTypes, out breakOutOfLoop);
                        if (breakOutOfLoop) return;
                    }

                    break;
                }

                case XmlSchemaGroupBase groupBase: {
                    OneOf<XmlSchemaAll, XmlSchemaSequence, XmlSchemaChoice> matchModel = default;
                    if (groupBase is XmlSchemaAll all) { matchModel = all; }
                    else if (groupBase is XmlSchemaSequence seq) { matchModel = seq; }
                    else if (groupBase is XmlSchemaChoice choice) { matchModel = choice; }

                    foreach (var item in matchModel.Match(a => a.Items, s => s.Items, c => c.Items)) {
                        TraverseAllSimpleTypes(item, ref simpleTypes, out breakOutOfLoop);
                        if (breakOutOfLoop) return;
                    }

                    break;
                }
            }

            breakOutOfLoop = !didAdd;
        }

        public static bool IsOfAnonymousType(this XmlSchemaAttribute attr)
        {
            return attr.SchemaType != null && !(
                (attr.AttributeSchemaType?.IsGlobal()).GetValueOrDefault() && 
                (attr.AttributeSchemaType?.IsBuiltInSimpleType()).GetValueOrDefault()
            );
        }
        
        public static bool IsOfAnonymousType(this XmlSchemaElement el)
        {
            return el.SchemaType != null && !(
                (el.ElementSchemaType?.IsGlobal()).GetValueOrDefault() && 
                (el.ElementSchemaType?.IsBuiltInSimpleType()).GetValueOrDefault()
            );
        }

        /// <summary>
        /// Returns true or false if the current <paramref name="attribute"/> defines an inline enumeration of values.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static bool DefinesInlineEnum(this XmlSchemaAttribute attribute)
        {
            if (!attribute.AttributeSchemaType.IsEnum()) return false;

            var xmlSchemaSimpleTypeRestriction = attribute.AttributeSchemaType.Content as XmlSchemaSimpleTypeRestriction;
            var facets = xmlSchemaSimpleTypeRestriction?.Facets.Cast<XmlSchemaFacet>();
            var isInlineEnum = attribute.AttributeSchemaType.IsEnum() &&
                               attribute.AttributeSchemaType.IsDerivedByRestriction() &&
                               (facets?.Any()).GetValueOrDefault();
            return isInlineEnum;
        }

        /// <summary>
        /// Returns either the <see cref="XmlSchemaObject"/> that is the named parent of the current one or
        /// null if the parent has no name attribute.
        /// </summary>
        /// <remarks>Many <see cref="XmlSchemaObject"/>s are themselves nested under other objects that are
        /// themselves unnamed, and it is the named ones that are helpful to know.</remarks>
        /// <param name="object"></param>
        /// <returns></returns>
        public static XmlSchemaObject GetClosestNamedParent(this XmlSchemaObject @object)
        {
            if (@object.Parent is XmlSchemaXPath xmlSchemaXPath) { return null; }
            if (@object.Parent is XmlSchema xmlSchema) { return null; }
            if (@object.Parent is XmlSchemaAll xmlSchemaAll) { return null; }
            //if (@object.Parent is XmlSchemaAnnotated xmlSchemaAnnotated) { return null; }
            if (@object.Parent is XmlSchemaAnnotation xmlSchemaAnnotation) { return null; }
            if (@object.Parent is XmlSchemaAny xmlSchemaAny) { return null; }
            if (@object.Parent is XmlSchemaAnyAttribute xmlSchemaAnyAttribute) { return null; }
            if (@object.Parent is XmlSchemaAppInfo xmlSchemaAppInfo) { return null; }
            if (@object.Parent is XmlSchemaAttribute xmlSchemaAttribute) { return xmlSchemaAttribute; }
            if (@object.Parent is XmlSchemaAttributeGroup xmlSchemaAttributeGroup) { return xmlSchemaAttributeGroup; }
            if (@object.Parent is XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef) { return null; }
            if (@object.Parent is XmlSchemaChoice xmlSchemaChoice) { return null; }

            if (@object.Parent is XmlSchemaComplexContent xmlSchemaComplexContent) {
                if (xmlSchemaComplexContent.Parent != null) return xmlSchemaComplexContent.GetClosestNamedParent();
                return null;
            }

            if (@object.Parent is XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension) {
                if (xmlSchemaComplexContentExtension.Parent != null)
                    return xmlSchemaComplexContentExtension.GetClosestNamedParent();
                return null;
            }

            if (@object.Parent is XmlSchemaComplexContentRestriction xmlSchemaComplexContentRestriction) {
                if (xmlSchemaComplexContentRestriction.Parent != null)
                    return xmlSchemaComplexContentRestriction.GetClosestNamedParent();
                return null;
            }

            if (@object.Parent is XmlSchemaComplexType xmlSchemaComplexType) {
                if (xmlSchemaComplexType.Name == null) return xmlSchemaComplexType.GetClosestNamedParent();
                return xmlSchemaComplexType;
            }
            //if (@object.Parent is XmlSchemaContent xmlSchemaContent) { 
            //    if (xmlSchemaContent.Parent != null) return xmlSchemaContent.GetClosestNamedParent();
            //    return null; 
            //}
            //if (@object.Parent is XmlSchemaContentModel xmlSchemaContentModel) { 
            //    if (xmlSchemaContentModel.Parent != null) return xmlSchemaContentModel.GetClosestNamedParent();
            //    return null; 
            //}
            if (@object.Parent is XmlSchemaDocumentation xmlSchemaDocumentation) { return null; }
            if (@object.Parent is XmlSchemaElement xmlSchemaElement) { return xmlSchemaElement; }
            if (@object.Parent is XmlSchemaEnumerationFacet xmlSchemaEnumerationFacet) { return null; }
            if (@object.Parent is XmlSchemaExternal xmlSchemaExternal) { return null; }
            if (@object.Parent is XmlSchemaFacet xmlSchemaFacet) { return null; }
            if (@object.Parent is XmlSchemaFractionDigitsFacet xmlSchemaFractionDigitsFacet) { return null; }
            if (@object.Parent is XmlSchemaGroup xmlSchemaGroup) { return xmlSchemaGroup; }
            if (@object.Parent is XmlSchemaGroupBase xmlSchemaGroupBase) { return null; }
            if (@object.Parent is XmlSchemaGroupRef xmlSchemaGroupRef) { return null; }
            if (@object.Parent is XmlSchemaIdentityConstraint xmlSchemaIdentityConstraint) { return null; }
            if (@object.Parent is XmlSchemaImport xmlSchemaImport) { return null; }
            if (@object.Parent is XmlSchemaInclude xmlSchemaInclude) { return null; }
            if (@object.Parent is XmlSchemaKey xmlSchemaKey) { return xmlSchemaKey; }
            if (@object.Parent is XmlSchemaKeyref xmlSchemaKeyref) { return xmlSchemaKeyref; }
            if (@object.Parent is XmlSchemaLengthFacet xmlSchemaLengthFacet) { return null; }
            if (@object.Parent is XmlSchemaMaxExclusiveFacet xmlSchemaMaxExclusiveFacet) { return null; }
            if (@object.Parent is XmlSchemaMaxInclusiveFacet xmlSchemaMaxInclusiveFacet) { return null; }
            if (@object.Parent is XmlSchemaMaxLengthFacet xmlSchemaMaxLengthFacet) { return null; }
            if (@object.Parent is XmlSchemaMinExclusiveFacet xmlSchemaMinExclusiveFacet) { return null; }
            if (@object.Parent is XmlSchemaMinInclusiveFacet xmlSchemaMinInclusiveFacet) { return null; }
            if (@object.Parent is XmlSchemaMinLengthFacet xmlSchemaMinLengthFacet) { return null; }
            if (@object.Parent is XmlSchemaNotation xmlSchemaNotation) { return null; }
            if (@object.Parent is XmlSchemaNumericFacet xmlSchemaNumericFacet) { return null; }
            if (@object.Parent is XmlSchemaParticle xmlSchemaParticle) { return null; }
            if (@object.Parent is XmlSchemaPatternFacet xmlSchemaPatternFacet) { return null; }
            if (@object.Parent is XmlSchemaRedefine xmlSchemaRedefine) { return null; }
            if (@object.Parent is XmlSchemaSequence xmlSchemaSequence) { return null; }

            if (@object.Parent is XmlSchemaSimpleContent xmlSchemaSimpleContent) {
                if (xmlSchemaSimpleContent.Parent != null) return xmlSchemaSimpleContent.GetClosestNamedParent();
                return null;
            }

            if (@object.Parent is XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension) {
                if (xmlSchemaSimpleContentExtension.Parent != null)
                    return xmlSchemaSimpleContentExtension.GetClosestNamedParent();
                return null;
            }
            if (@object.Parent is XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction) { 
                if (xmlSchemaSimpleContentRestriction.Parent != null) 
                    return xmlSchemaSimpleContentRestriction.GetClosestNamedParent();
                return null; 
            }

            if (@object.Parent is XmlSchemaSimpleType xmlSchemaSimpleType) {
                if (xmlSchemaSimpleType.Name == null) return xmlSchemaSimpleType.GetClosestNamedParent();
                return xmlSchemaSimpleType;
            }
            //if (@object.Parent is XmlSchemaSimpleTypeContent xmlSchemaSimpleTypeContent) { return null; }
            if (@object.Parent is XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList) { return null; }
            if (@object.Parent is XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction) { 
                if (xmlSchemaSimpleTypeRestriction.Parent != null) 
                    return xmlSchemaSimpleTypeRestriction.GetClosestNamedParent();
                return null; 
            }
            if (@object.Parent is XmlSchemaSimpleTypeUnion xmlSchemaSimpleTypeUnion) { return null; }
            if (@object.Parent is XmlSchemaTotalDigitsFacet xmlSchemaTotalDigitsFacet) { return null; }
            if (@object.Parent is XmlSchemaType xmlSchemaType) { return null; }
            if (@object.Parent is XmlSchemaUnique xmlSchemaUnique) { return null; }
            if (@object.Parent is XmlSchemaWhiteSpaceFacet xmlSchemaWhiteSpaceFacet) { return null; }

            return null;
        }

        /// <summary>
        /// Using reflection, retrieves the value for any property that is named 'Name'.
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static string GetPotentialName(this XmlSchemaObject @object)
        {
            var properties = @object.GetType().GetProperties();

            var possibleNameProp = properties.FirstOrDefault(p => p.Name == "Name");

            if (possibleNameProp == null) return null;

            var possibleNameValue = possibleNameProp.GetValue(@object);

            if (!(possibleNameValue is string)) 
                throw new NotSupportedException("Bad type!");

            return Convert.ToString(possibleNameValue);     
        }

        public static XDocument ToXDocument(this XmlSchema xs)
        {
            var stringWriter = new StringWriter();
            xs.Write(stringWriter);
            var xDocument = XDocument.Parse(stringWriter.ToString());
            return xDocument;
        }

        public static List<XDocument> ToXDocuments(this XmlSchemaSet xs)
        {
            return xs.Schemas().Cast<XmlSchema>().Select(x => x.ToXDocument()).ToList();
        }

        /// <summary>
        /// Merges the configurations of all schemas in the provided <see cref="XmlSchemaSet"/> into a single configuration.
        /// </summary>
        /// <param name="xs">The <see cref="XmlSchemaSet"/> containing the schemas to merge configurations for.</param>
        /// <param name="startingConfig">
        /// An optional starting <see cref="Configuration"/> to merge into. If not provided, an example configuration is used.
        /// </param>
        /// <returns>
        /// A merged <see cref="Configuration"/> that combines namespace configurations from all schemas in the set.
        /// </returns>
        /// <remarks>
        /// This method processes each schema in the <see cref="XmlSchemaSet"/>, converts it to an <see cref="XDocument"/>,
        /// loads its configuration, and merges it into the starting configuration.
        /// </remarks>
        public static Configuration ToDefaultMergedConfiguration(this XmlSchemaSet xs, Configuration startingConfig = null)
        {
            var egConfig = startingConfig ?? (Configuration)ConfigurationProvider.ProvideExampleConfigurationXml();
            var docs = xs.Schemas().Cast<XmlSchema>().Select(x => x.ToXDocument()).ToList();
            var configs = docs.Select(d => Configuration.LoadForSchema(d));

            Configuration mergedConfigOutput = configs.Aggregate(egConfig,
                (theEgConfig, loadedConfig) => theEgConfig.MergeNamespaces(loadedConfig));

            return mergedConfigOutput;
        }
    }
}
