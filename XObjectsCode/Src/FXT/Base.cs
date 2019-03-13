//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Xml.Fxt
{
    using System;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Linq;
    using System.Collections.Generic;

    public class FxtScope
    {
        public bool Test(XmlSchemaElement el)
        {
            return true;
        }

        public bool Test(XmlSchemaAttribute at)
        {
            return true;
        }

        public bool Test(XmlSchemaType ty)
        {
            return true;
        }
    }

    public class FxtLog
    {
        public List<FxtAnnotation> AtType(XmlQualifiedName n)
        {
            if (!aTypes.ContainsKey(n))
                aTypes.Add(n, new List<FxtAnnotation>());
            return aTypes[n];
        }

        public List<FxtAnnotation> AtElement(XmlQualifiedName n)
        {
            if (!aElements.ContainsKey(n))
                aElements.Add(n, new List<FxtAnnotation>());
            return aElements[n];
        }

        public List<FxtAnnotation> AtAttribute(XmlQualifiedName n)
        {
            if (!aAttributes.ContainsKey(n))
                aAttributes.Add(n, new List<FxtAnnotation>());
            return aAttributes[n];
        }

        public List<FxtAnnotation> AtObject(XmlSchemaObject o)
        {
            if (!aObjects.ContainsKey(o))
                aObjects.Add(o, new List<FxtAnnotation>());
            return aObjects[o];
        }

        Dictionary<XmlQualifiedName, List<FxtAnnotation>> aTypes =
            new Dictionary<XmlQualifiedName, List<FxtAnnotation>>();

        Dictionary<XmlQualifiedName, List<FxtAnnotation>> aElements =
            new Dictionary<XmlQualifiedName, List<FxtAnnotation>>();

        Dictionary<XmlQualifiedName, List<FxtAnnotation>> aAttributes =
            new Dictionary<XmlQualifiedName, List<FxtAnnotation>>();

        Dictionary<XmlSchemaObject, List<FxtAnnotation>> aObjects =
            new Dictionary<XmlSchemaObject, List<FxtAnnotation>>();
    }

    public interface IFxtTransformation
    {
        void Run();
    }

    public class FxtException : Exception
    {
        public FxtException() : base()
        {
        }

        public FxtException(string msg) : base(msg)
        {
        }
    }

    public class FxtInterpreterException : FxtException
    {
        public FxtInterpreterException(string msg) : base(msg)
        {
        }
    }

    public class FxtTypeClashException : FxtException
    {
        public FxtTypeClashException(XmlQualifiedName name) : base()
        {
        }
    }

    public class FxtElementClashException : FxtException
    {
        public FxtElementClashException(XmlQualifiedName name) : base()
        {
        }
    }

    public class FxtAttributeClashException : FxtException
    {
        public FxtAttributeClashException(XmlQualifiedName name) : base()
        {
        }
    }

    public abstract class FxtAnnotation
    {
    }

    public abstract class FxtXAnnotation : FxtAnnotation
    {
    }

    public abstract class FxtOAnnotation : FxtAnnotation
    {
    }

    public delegate void interpreter(XmlSchemaSet schemas, XElement trafo, FxtLog log, List<IFxtTransformation> trafos);

    public static class FxtInterpreter
    {
        internal static XNamespace FxtNs = "http://www.microsoft.com/FXT";

        internal static FxtException ex = new FxtInterpreterException(
            "Requested Xsd transformation not understood.");

        public static FxtLog Run(XmlSchemaSet schemas, XElement trafo, interpreter i)
        {
            var trafos = new List<IFxtTransformation>();
            var log = new FxtLog();

            // Compile if necessary
            if (!schemas.IsCompiled)
                schemas.Compile();
            if (trafo == null)
                return log;

            // Interpret trafo
            i(schemas, trafo, log, trafos);

            // Execute trafos
            foreach (var x in trafos)
                x.Run();

            // Re-compile
            foreach (var x in schemas.XmlSchemas())
                schemas.Reprocess(x);
            schemas.Compile();
            return log;
        }
    }
}