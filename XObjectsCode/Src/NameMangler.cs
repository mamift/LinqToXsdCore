//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using System.Globalization;

namespace Xml.Schema.Linq.CodeGen
{
    internal static class NameGenerator
    {
        static int uniqueIdCounter = 0;
        static HashSet<string> keywords;

        static NameGenerator()
        {
            keywords = new HashSet<string>(StringComparer.Ordinal)
            {
                "abstract", "event", "new", "struct", "as", "explicit", "null", "switch",
                "base", "extern", "object", "this", "bool", "false", "operator", "throw",
                "break", "finally", "out", "true", "byte", "fixed", "override", "try", "case",
                "float", "params", "typeof", "catch", "for", "private", "uint", "char", "foreach",
                "protected", "ulong", "checked", "goto", "public", "unchecked", "class",
                "if", "readonly", "unsafe", "const", "implicit", "ref", "ushort", "continue",
                "in", "return", "using", "decimal", "int", "sbyte", "virtual", "default",
                "interface", "sealed", "volatile", "delegate", "internal", "short", "void",
                "do", "is", "sizeof", "while", "double", "lock", "stackalloc", "else", "long",
                "static", "enum", "namespace", "string", "var"
            };
        }

        public static int GetUniqueID()
        {
            Interlocked.Increment(ref uniqueIdCounter);
            return uniqueIdCounter;
        }

        public static string ChangeClrName(string clrName, NameOptions options)
        {
            switch (options)
            {
                case NameOptions.MakeCollection:
                    if (clrName[0] == '@')
                    {
                        clrName = clrName.Remove(0, 1);
                    }

                    return clrName + "Collection";

                case NameOptions.MakeList:
                    return clrName + "List";

                case NameOptions.MakePlural:
                    return clrName + "s";

                case NameOptions.MakeField:
                    return clrName + "Field";

                case NameOptions.MakeLocal:
                    return clrName + "LocalType";

                case NameOptions.MakeUnion:
                    return clrName + "UnionValue";

                case NameOptions.MakeFixedValueField:
                    return clrName + "FixedValue";

                case NameOptions.MakeParam:
                    return clrName + "Param";

                case NameOptions.MakeDefaultValueField:
                    return clrName + "DefaultValue";
            }

            return clrName;
        }

        public static string GetServicesClassName()
        {
            return Constants.LinqToXsdTypeManager;
        }

        public static string MakeValidCLRNamespace(string xsdNamespace, bool nameMangler2)
        {
            if (xsdNamespace == null || xsdNamespace == string.Empty)
            {
                return string.Empty;
            }

            xsdNamespace = xsdNamespace.Replace("http://", string.Empty);
            if (xsdNamespace == string.Empty)
            {
                return string.Empty;
            }

            if (nameMangler2)
            {
                xsdNamespace = xsdNamespace.Replace('.', '_').Replace('-', '_');
            }

            string[] pieces = xsdNamespace.Split(new char[]
                {'/', '.', ':', '-'});
            string clrNS = NameGenerator.MakeValidIdentifier(pieces[0]);
            for (int i = 1; i < pieces.Length; i++)
            {
                if (pieces[i] != string.Empty)
                    clrNS = clrNS + "." + NameGenerator.MakeValidIdentifier(pieces[i]);
            }

            return clrNS;
        }

        public static string MakeValidIdentifier(string identifierName)
        {
            identifierName = CodeIdentifier.MakeValid(identifierName);
            if (isKeyword(identifierName))
                return "@" + identifierName;
            return identifierName;
        }

        public static bool isKeyword(string identifier)
        {
            return keywords.Contains(identifier);
        }
    }

    internal class SymbolEntry
    {
        public string xsdNamespace;
        public string clrNamespace;
        public string symbolName; //schema-name
        public string identifierName; //clr-name

        public override int GetHashCode()
        {
            return identifierName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SymbolEntry se = obj as SymbolEntry;
            if (se != null)
            {
                return (xsdNamespace == se.xsdNamespace) &&
                       identifierName.Equals(se.identifierName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public bool isNameFixed()
        {
            return symbolName != identifierName;
        }
    }

    internal class GlobalSymbolTable
    {
        internal Dictionary<SymbolEntry, SymbolEntry> symbols;
        internal Dictionary<XmlSchemaObject, string> schemaNameToIdentifiers;
        internal int nFixedNames = 0;
        LinqToXsdSettings configSettings;

        public GlobalSymbolTable(LinqToXsdSettings settings)
        {
            configSettings = settings;
            symbols = new Dictionary<SymbolEntry, SymbolEntry>();
            schemaNameToIdentifiers = new Dictionary<XmlSchemaObject, string>();
        }

        public SymbolEntry AddElement(XmlSchemaElement element)
        {
            return AddSymbol(element.QualifiedName, element, string.Empty);
        }

        public SymbolEntry AddType(XmlSchemaType type)
        {
            return AddSymbol(type.QualifiedName, type, Constants.TypeSuffix);
        }

        protected SymbolEntry AddSymbol(XmlQualifiedName qname, XmlSchemaObject schemaObject, string suffix)
        {
            SymbolEntry symbol = new SymbolEntry();
            symbol.xsdNamespace = qname.Namespace;
            symbol.clrNamespace = configSettings.GetClrNamespace(qname.Namespace);
            symbol.symbolName = qname.Name;
            string identifierName = NameGenerator.MakeValidIdentifier(symbol.symbolName);
            symbol.identifierName = identifierName;
            int id = 0;
            if (symbols.ContainsKey(symbol))
            {
                identifierName = identifierName + suffix;
                symbol.identifierName = identifierName;
                while (symbols.ContainsKey(symbol))
                {
                    id++;
                    symbol.identifierName = identifierName + id.ToString(CultureInfo.InvariantCulture.NumberFormat);
                }
            }

            if (symbol.isNameFixed())
                nFixedNames++;

            symbols.Add(symbol, symbol);
            schemaNameToIdentifiers.Add(schemaObject, symbol.identifierName); //Type vs typeName
            return symbol;
        }
    }

    internal class AnonymousType
    {
        public string identifier;
        public XmlSchemaElement parentElement;
        public XmlSchemaComplexType wrappingType;
        public ClrTypeReference typeRefence;
    }

    internal class LocalSymbolTable
    {
        Hashtable symbolToQName;
        Hashtable qNameToSymbol;
        List<AnonymousType> anonymousTypes;

        public void Init(XmlSchemaElement element)
        {
            Init(element.QualifiedName.Name);
        }

        public void Init(XmlSchemaType type)
        {
            Init(type.QualifiedName.Name);
        }

        public void Init(string className)
        {
            if (anonymousTypes == null)
            {
                symbolToQName = new Hashtable();
                qNameToSymbol = new Hashtable();
                anonymousTypes = new List<AnonymousType>();
            }
            else
                Reset();

            symbolToQName.Add(className.ToUpper(CultureInfo.InvariantCulture), XmlQualifiedName.Empty);
        }

        public void Reset()
        {
            symbolToQName.Clear();
            qNameToSymbol.Clear();
            if (anonymousTypes.Count > 0)
                anonymousTypes = new List<AnonymousType>();
        }

        public string AddLocalElement(XmlSchemaElement element)
        {
            string identifierName = (string) qNameToSymbol[element.QualifiedName];
            if (identifierName != null)
                return identifierName;
            identifierName = NameGenerator.MakeValidIdentifier(element.QualifiedName.Name);
            identifierName = getSymbol(identifierName, Constants.LocalElementConflictSuffix);
            symbolToQName.Add(identifierName.ToUpper(CultureInfo.InvariantCulture), element.QualifiedName);
            qNameToSymbol.Add(element.QualifiedName, identifierName);
            return identifierName;
        }

        public string AddAttribute(XmlSchemaAttribute attribute)
        {
            string identifierName = NameGenerator.MakeValidIdentifier(attribute.QualifiedName.Name);
            identifierName = getSymbol(identifierName, Constants.LocalAttributeConflictSuffix);
            symbolToQName.Add(identifierName.ToUpper(CultureInfo.InvariantCulture), attribute.QualifiedName);
            return identifierName;
        }

        public void AddAnonymousType(string identifier, XmlSchemaElement parentElement,
            ClrTypeReference parentElementTypeRef)
        {
            AnonymousType at = new AnonymousType();
            at.identifier = identifier;
            at.typeRefence = parentElementTypeRef;
            at.parentElement = parentElement;
            anonymousTypes.Add(at);
        }

        public void AddComplexRestrictedContentType(XmlSchemaComplexType wrappingType, ClrTypeReference wrappingTypeRef)
        {
            string identifier = NameGenerator.MakeValidIdentifier(wrappingType.Name);
            AnonymousType at = new AnonymousType();
            at.identifier = identifier;
            at.typeRefence = wrappingTypeRef;
            at.wrappingType = wrappingType;
            anonymousTypes.Add(at);
        }

        public List<AnonymousType> GetAnonymousTypes()
        {
            foreach (AnonymousType at in anonymousTypes)
            {
                ClrTypeReference typeReference = at.typeRefence;
                string typeIdentifier = getSymbol(at.identifier, Constants.LocalTypeSuffix);
                symbolToQName.Add(typeIdentifier.ToUpper(CultureInfo.InvariantCulture), XmlQualifiedName.Empty);
                typeReference.Name = typeIdentifier;
                at.identifier = typeIdentifier;
            }

            return anonymousTypes;
        }

        public void RegisterMember(string identifierName)
        {
            string outputSymbol = null;
            outputSymbol = AddMember(identifierName);

            // We shouldn't be getting collisions for 
            // identifiers that are hard-coded into classes
            Debug.Assert(outputSymbol == identifierName);
        }

        public string AddMember(string identifierName)
        {
            // not making valid. Assuming this has already been done. 
            string outputSymbol = null;
            outputSymbol = getSymbol(identifierName, String.Empty);
            symbolToQName.Add(outputSymbol.ToUpper(CultureInfo.InvariantCulture), identifierName);

            return outputSymbol;
        }

        private string getSymbol(string identifierName, string suffix)
        {
            int id = 0;
            string symbol = identifierName;
            string symbolU = symbol.ToUpper(CultureInfo.InvariantCulture);
            if (symbolToQName[symbolU] == null)
            {
                return symbol;
            }

            symbol = symbol + suffix;
            symbolU = symbol.ToUpper(CultureInfo.InvariantCulture);
            string temp = symbolU;

            while (symbolToQName[symbolU] != null)
            {
                id++;
                symbolU = temp + id.ToString(CultureInfo.InvariantCulture.NumberFormat);
            }

            if (id > 0)
                symbol = symbol + id.ToString(CultureInfo.InvariantCulture.NumberFormat);
            return symbol;
        }
    }
}