using System;
using System.Xml;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Xml.Schema.Linq.CodeGen
{
    public partial class XsdToTypesConverter
    {
        public List<XmlSchemaSimpleType> AnonymousSimpleTypes { get; } = new List<XmlSchemaSimpleType>();
    }
}