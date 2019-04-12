//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.Linq;

namespace Xml.Schema.Linq
{
    public interface ILinqToXsdTypeManager
    {
        Dictionary<XName, Type> GlobalTypeDictionary { get; }
        Dictionary<XName, Type> GlobalElementDictionary { get; }
        Dictionary<Type, Type> RootContentTypeMapping { get; }
        XmlSchemaSet Schemas { get; set; }
    }

    public interface IXMetaData
    {
        XName SchemaName { get; }
        ILinqToXsdTypeManager TypeManager { get; }
        SchemaOrigin TypeOrigin { get; }
        Dictionary<XName, System.Type> LocalElementsDictionary { get; }
        XTypedElement Content { get; }
        ContentModelEntity GetContentModel();
        FSM GetValidationStates();
    }

    public interface IXTyped
    {
        IEnumerable<T> Descendants<T>() where T : XTypedElement, new();
        IEnumerable<T> Ancestors<T>() where T : XTypedElement;
        IEnumerable<T> SelfAndDescendants<T>() where T : XTypedElement, new();
        IEnumerable<T> SelfAndAncestors<T>() where T : XTypedElement;
    }
}