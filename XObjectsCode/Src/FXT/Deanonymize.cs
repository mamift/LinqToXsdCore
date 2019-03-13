//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Xml.Fxt
{
    using System.Xml;
    using System.Xml.Schema;
    using System.Collections.Generic;

    // Lazy transformation object
    public class ExtractType : IFxtTransformation
    {
        internal XmlSchemaElement element;

        public void Run()
        {
            element.SchemaType.Name = element.Name;
            element.XmlSchema().Add(element.SchemaType);
            element.SchemaType = null;
            element.SchemaTypeName =
                new XmlQualifiedName(
                    element.Name,
                    element.XmlSchema().TargetNamespace);
        }
    }

    // Mark extracted types this way
    public class ExtractTypeAnnotation : FxtXAnnotation
    {
    }

    public static class FxtDeanonymize
    {
        public static IEnumerable<IFxtTransformation> Deanonymize(
            this XmlSchemaSet schemas, // the schemas to transform
            FxtScope scope, // affected abstractions
            bool strict, // be strict about de-anonymization
            FxtLog log // log transformations and analyses
        )
        {
            // Determine potential ambiguity of local element names
            var recurrence = new Dictionary<string, int>();
            foreach (var el in schemas.LocalXsdElements())
                if (el.SchemaType != null
                    && el.SchemaType is XmlSchemaComplexType)
                    if (!recurrence.ContainsKey(el.Name))
                        recurrence.Add(el.Name, 1);
                    else
                        recurrence[el.Name]++;

            foreach (var el in schemas.LocalXsdElements())

                // Test whether element is affected
                if (el.SchemaType != null
                    && el.SchemaType is XmlSchemaComplexType
                    && scope.Test(el))
                {
                    // Determine name of new type
                    var qname = new XmlQualifiedName(
                        el.Name,
                        el.XmlSchema().TargetNamespace);

                    // Check for name clashes
                    if (schemas.DefinesXsdType(qname)
                        || recurrence[el.Name] > 1)
                        if (!strict)
                            continue; // Skip this candidate
                        else
                            throw new FxtTypeClashException(qname);

                    // Build and yield transformation
                    var trafo = new ExtractType();
                    trafo.element = el;
                    log.AtType(qname).Add(new ExtractTypeAnnotation());
                    yield return trafo;
                }
        }
    }
}