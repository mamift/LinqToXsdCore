//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Xml.Fxt
{
    using System.Xml.Schema;

    public static class XmlSchemaDmlExtensions
    {
        public static void Add(this XmlSchema that, XmlSchemaType ty)
        {
            that.Items.Add(ty);
        }

        public static void Add(this XmlSchema that, XmlSchemaElement el)
        {
            that.Items.Add(el);
        }

        public static void Add(this XmlSchema that, XmlSchemaAttribute at)
        {
            that.Items.Add(at);
        }

        public static void ReplaceThisWith(this XmlSchemaElement that, XmlSchemaElement with)
        {
            var gr = that.Parent as XmlSchemaGroupBase;
            int idx = gr.Items.IndexOf(that);
            gr.Items.Insert(idx, with);
            gr.Items.Remove(that);
        }

        public static void ReplaceThisWith(this XmlSchemaAttribute that, XmlSchemaAttribute with)
        {
            var gr = that.Parent as XmlSchemaComplexType;
            int idx = gr.Attributes.IndexOf(that);
            gr.Attributes.Insert(idx, with);
            gr.Attributes.Remove(that);
        }

        public static void RemoveThis(this XmlSchemaType that)
        {
            ((XmlSchema) that.Parent).Items.Remove(that);
        }
    }
}