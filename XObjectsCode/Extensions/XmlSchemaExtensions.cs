using System;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using Xml.Schema.Linq.CodeGen;

namespace XObjects
{
    public static class XmlSchemaExtensions
    {
        public static bool IsInlineDefinedEnum(this XmlSchemaAttribute attribute)
        {
            if (!attribute.AttributeSchemaType.IsEnum()) return false;

            var xmlSchemaSimpleTypeRestriction = attribute.AttributeSchemaType.Content as XmlSchemaSimpleTypeRestriction;
            var facets = xmlSchemaSimpleTypeRestriction?.Facets.Cast<XmlSchemaObject>();
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
    }
}