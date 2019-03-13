//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Xml.Fxt
{
    using System.Linq;
    using System.Xml;
    using System.Xml.Schema;
    using System.Collections.Generic;

    public static class XmlSchemaQueryExtensions
    {
        public static bool DefinesXsdType(this XmlSchemaSet schemas, XmlQualifiedName name)
        {
            return schemas.GlobalXsdTypes().QNames().Contains(name);
        }

        public static bool DefinesXsdElement(this XmlSchemaSet schemas, XmlQualifiedName name)
        {
            return schemas.GlobalXsdElements().QNames().Contains(name);
        }

        public static bool DefinesXsdAttribute(this XmlSchemaSet schemas, XmlQualifiedName name)
        {
            return schemas.GlobalXsdAttributes().QNames().Contains(name);
        }

        public static XmlQualifiedName RootType(this XmlSchemaSet schemas, XmlQualifiedName name)
        {
            var ct = schemas.GlobalTypes[name] as XmlSchemaComplexType;
            if (ct == null)
                return null;
            if (ct.BaseXmlSchemaType.TypeCode == XmlTypeCode.Item)
                return name;
            return schemas.RootType(ct.BaseXmlSchemaType.QualifiedName);
        }

        public static XmlQualifiedName RootElement(this XmlSchemaSet schemas, XmlQualifiedName name)
        {
            var el = schemas.GlobalElements[name] as XmlSchemaElement;
            var bname = el.SubstitutionGroup;
            if (bname.IsEmpty)
                return name;
            return schemas.RootElement(bname);
        }

        public static bool IsDerivedComplexType(this XmlSchemaType ty)
        {
            var ct = ty as XmlSchemaComplexType;
            if (ct != null)
                return (ct.ContentModel as XmlSchemaComplexContent) != null;
            return false;
        }

        public static bool IsGlobal(this XmlSchemaElement el)
        {
            return el.Parent is XmlSchema;
        }

        public static bool IsLocal(this XmlSchemaElement el)
        {
            return !el.IsGlobal();
        }

        public static XmlSchema XmlSchema(this XmlSchemaObject o)
        {
            if (o is XmlSchema) return (o as XmlSchema);
            else return o.Parent.XmlSchema();
        }

        public static IEnumerable<XmlSchema> XmlSchemas(this XmlSchemaSet set)
        {
            foreach (XmlSchema x in set.Schemas())
                yield return x;
        }

        public static IEnumerable<XmlSchemaType> GlobalXsdTypes(this XmlSchemaSet set)
        {
            foreach (var x in set.XmlSchemas())
            foreach (var y in x.GlobalXsdTypes())
                yield return y;
        }

        public static IEnumerable<XmlSchemaType> AllXsdTypes(this XmlSchemaSet set)
        {
            return Enumerable.Union(
                set.GlobalXsdTypes(),
                set.AnonymousXsdTypes());
        }

        public static IEnumerable<XmlSchemaType> GlobalXsdTypes(this XmlSchema schema)
        {
            foreach (XmlSchemaType x in schema.SchemaTypes.Values)
                yield return x;
        }

        public static IEnumerable<XmlSchemaType> AnonymousXsdTypes(this XmlSchemaSet set)
        {
            return
                from gel in set.AllXsdElements()
                where gel.SchemaType != null
                select gel.SchemaType;
        }

        public static IEnumerable<XmlQualifiedName> QNames(this IEnumerable<XmlSchemaType> types)
        {
            return types.Select(x => x.QualifiedName);
        }

        public static IEnumerable<XmlSchemaElement> AllXsdElements(this XmlSchemaSet set)
        {
            return Enumerable.Union(
                set.GlobalXsdElements(),
                set.LocalXsdElements());
        }

        public static IEnumerable<XmlSchemaElement> GlobalXsdElements(this XmlSchemaSet set)
        {
            foreach (var x in set.XmlSchemas())
            foreach (var y in x.GlobalXsdElements())
                yield return y;
        }

        public static IEnumerable<XmlSchemaElement> GlobalXsdElements(this XmlSchema schema)
        {
            foreach (XmlSchemaElement x in schema.Elements.Values)
                yield return x;
        }

        public static IEnumerable<XmlSchemaAttribute> GlobalXsdAttributes(this XmlSchemaSet set)
        {
            foreach (var x in set.XmlSchemas())
            foreach (var y in x.GlobalXsdAttributes())
                yield return y;
        }

        public static IEnumerable<XmlSchemaAttribute> GlobalXsdAttributes(this XmlSchema schema)
        {
            foreach (XmlSchemaAttribute x in schema.Attributes.Values)
                yield return x;
        }

        public static IEnumerable<XmlQualifiedName> QNames(this IEnumerable<XmlSchemaElement> els)
        {
            return els.Select(x => x.QualifiedName);
        }

        public static IEnumerable<XmlQualifiedName> QNames(this IEnumerable<XmlSchemaAttribute> els)
        {
            return els.Select(x => x.QualifiedName);
        }

        public static IEnumerable<XmlSchemaElement> LocalXsdElements(this XmlSchemaSet set)
        {
            foreach (var x in set.XmlSchemas())
            foreach (var y in x.LocalXsdElements())
                yield return y;
        }

        public static IEnumerable<XmlSchemaElement> LocalXsdElements(this XmlSchema schema)
        {
            foreach (XmlSchemaElement x in schema.Elements.Values)
            foreach (var y in x.LocalXsdElements())
                yield return y;

            foreach (XmlSchemaType x in schema.SchemaTypes.Values)
            foreach (var y in x.LocalXsdElements())
                yield return y;

            foreach (XmlSchemaGroup x in schema.Groups.Values)
            foreach (var y in x.LocalXsdElements())
                yield return y;
        }

        public static IEnumerable<XmlSchemaElement> LocalXsdElements(this XmlSchemaElement el)
        {
            return el.SchemaType.LocalXsdElements();
        }

        public static IEnumerable<XmlSchemaElement> LocalXsdElements(this XmlSchemaType ty)
        {
            var ct = ty as XmlSchemaComplexType;
            if (ct == null) yield break;
            if (ct.ContentModel == null)
                foreach (var x in ct.Particle.LocalXsdElements())
                    yield return x;
            else
            {
                var cc = ct.ContentModel as XmlSchemaComplexContent;
                if (cc == null) yield break;
                var ext = cc.Content as XmlSchemaComplexContentExtension;
                if (ext == null) yield break;
                foreach (var x in ext.Particle.LocalXsdElements())
                    yield return x;
            }
        }

        public static IEnumerable<XmlSchemaElement> LocalXsdElements(this XmlSchemaGroup gr)
        {
            return gr.Particle.LocalXsdElements();
        }

        public static IEnumerable<XmlSchemaElement> LocalXsdElements(this XmlSchemaParticle pa)
        {
            if (pa == null) yield break;

            var grp = pa as XmlSchemaGroupBase;
            if (grp != null)
            {
                foreach (XmlSchemaParticle x in grp.Items)
                foreach (var y in x.LocalXsdElements())
                    yield return y;
                yield break;
            }

            var el = pa as XmlSchemaElement;
            if (el != null)
            {
                yield return el;
                foreach (var x in el.SchemaType.LocalXsdElements())
                    yield return x;
                yield break;
            }
        }

        public static IEnumerable<XmlSchemaAttribute> LocalXsdAttributes(this XmlSchemaSet set)
        {
            foreach (var x in set.XmlSchemas())
            foreach (var y in x.LocalXsdAttributes())
                yield return y;
        }

        public static IEnumerable<XmlSchemaAttribute> LocalXsdAttributes(this XmlSchema schema)
        {
            foreach (XmlSchemaElement x in schema.Elements.Values)
            foreach (var y in x.LocalXsdAttributes())
                yield return y;

            foreach (XmlSchemaType x in schema.SchemaTypes.Values)
            foreach (var y in x.LocalXsdAttributes())
                yield return y;

            foreach (XmlSchemaGroup x in schema.Groups.Values)
            foreach (var y in x.LocalXsdAttributes())
                yield return y;
        }

        public static IEnumerable<XmlSchemaAttribute> LocalXsdAttributes(this XmlSchemaElement el)
        {
            return el.SchemaType.LocalXsdAttributes();
        }

        public static IEnumerable<XmlSchemaAttribute> LocalXsdAttributes(this XmlSchemaType ty)
        {
            var ct = ty as XmlSchemaComplexType;
            if (ct == null) yield break;
            foreach (XmlSchemaAttribute x in ct.Attributes)
                yield return x;
            if (ct.ContentModel == null)
                foreach (var x in ct.Particle.LocalXsdAttributes())
                    yield return x;
            else
            {
                var cc = ct.ContentModel as XmlSchemaComplexContent;
                if (cc == null) yield break;
                var ext = cc.Content as XmlSchemaComplexContentExtension;
                if (ext == null) yield break;
                foreach (XmlSchemaAttribute x in ext.Attributes)
                    yield return x;
                foreach (var x in ext.Particle.LocalXsdAttributes())
                    yield return x;
            }
        }

        public static IEnumerable<XmlSchemaAttribute> LocalXsdAttributes(this XmlSchemaGroup gr)
        {
            return gr.Particle.LocalXsdAttributes();
        }

        public static IEnumerable<XmlSchemaAttribute> LocalXsdAttributes(this XmlSchemaParticle pa)
        {
            if (pa == null) yield break;

            var grp = pa as XmlSchemaGroupBase;
            if (grp != null)
            {
                foreach (XmlSchemaParticle x in grp.Items)
                foreach (var y in x.LocalXsdAttributes())
                    yield return y;
                yield break;
            }

            var el = pa as XmlSchemaElement;
            if (el != null)
            {
                foreach (var x in el.SchemaType.LocalXsdAttributes())
                    yield return x;
                yield break;
            }
        }

        public static IEnumerable<XmlSchemaAttribute> XsdAttributesInScope(this XmlSchemaElement el)
        {
            return el.SchemaType.XsdAttributesInScope();
        }

        public static IEnumerable<XmlSchemaAttribute> XsdAttributesInScope(this XmlSchemaType ty)
        {
            if (ty == null) yield break;
            var ct = ty as XmlSchemaComplexType;
            if (ct == null) yield break;
            foreach (XmlSchemaAttribute x in ct.Attributes)
                yield return x;
            if (ct.ContentModel != null)
            {
                var cc = ct.ContentModel as XmlSchemaComplexContent;
                if (cc == null) yield break;
                var ext = cc.Content as XmlSchemaComplexContentExtension;
                if (ext == null) yield break;
                foreach (XmlSchemaAttribute x in ext.Attributes)
                    yield return x;
            }
        }
    }
}