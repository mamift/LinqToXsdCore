//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Xml.Schema.Linq.CodeGen
{
    internal static class Constants
    {
        public const string TypedXLinqNs = "http://www.microsoft.com/xml/schema/linq";
        public const string FxtNs = "http://www.microsoft.com/FXT";
        public const string XSD = "http://www.w3.org/2001/XMLSchema";

        public static readonly string SystemTypeName = $"{nameof(System)}.{nameof(System.Type)}";
        public static readonly string SystemXmlLinqNamespaceQualifer = $"{nameof(System)}.{nameof(System.Xml)}.{nameof(System.Xml.Linq)}";

        //Custom Attribute names
        public const string XElement = "XElement";

        //XElement Method Calls
        public const string BuildTypeDictionary = "BuildTypeDictionary";
        public const string BuildElementDictionary = "BuildElementDictionary";
        public const string BuildWrapperDictionary = "BuildWrapperDictionary";
        public const string GetRootType = "GetRootType";
        public const string Initialize = "Initialize";
        public const string SetList = "SetList";

        //XTypedElement Method calls
        public const string GetBuiltInSimpleType = "GetBuiltInSimpleType";
        public const string ParseValue = "ParseValue";
        public const string ParseListValue = "ParseListValue";
        public const string ParseUnionValue = "ParseUnionValue";
        public const string Untyped = "Untyped";

        //Code Gen Helper Method calls
        public const string GetContentModel = "GetContentModel";

        //Variable names in properties and constructors
        public const string ContentModelMember = "contentModel";
        public const string ContentModelType = "ContentModelEntity";
        public const string SequenceContentModelEntity = "SequenceContentModelEntity";
        public const string ChoiceContentModelEntity = "ChoiceContentModelEntity";
        public const string NamedContentModelEntity = "NamedContentModelEntity";
        public const string SubstitutedContentModelEntity = "SubstitutedContentModelEntity";

        //Interfaces
        public const string ILinqToXsdTypeManager = "ILinqToXsdTypeManager";
        public const string IXMetaData = "IXMetaData";
        public const string IXmlSerializable = "IXmlSerializable";

        //Classes / struct
        public const string XTypedElement = "XTypedElement";
        public const string XTypedServices = "XTypedServices";
        public const string LinqToXsdTypeManager = "LinqToXsdTypeManager";
        public static readonly string XNameType = $"{SystemXmlLinqNamespaceQualifer}.{nameof(System.Xml.Linq.XName)}";
        public const string XmlSchemaType = "XmlSchemaType";
        public const string XmlTypeCode = "XmlTypeCode";
        public const string Any = "Any";
        public const string LinqToXsdException = "LinqToXsdException";

        //Field, Property names
        public const string GlobalTypeDictionary = "GlobalTypeDictionary";
        public const string GlobalElementDictionary = "GlobalElementDictionary";
        public const string RootContentTypeMapping = "RootContentTypeMapping";
        public const string LocalElementsDictionary = "LocalElementsDictionary";
        public const string TypeDictionaryField = "typeDictionary";
        public const string ElementDictionaryField = "elementDictionary";
        public const string WrapperDictionaryField = "wrapperDictionary";
        public const string LocalElementDictionaryField = "localElementDictionary";
        public const string EmptyDictionaryField = "EmptyDictionary";
        public const string EmptyTypeMappingDictionary = "EmptyTypeMappingDictionary";
        public const string TypeManagerSingletonField = "typeManagerSingleton";
        public const string TypeManager = "TypeManager";
        public const string TypeManagerInstance = "Instance";
        public const string SchemaName = "SchemaName";
        public const string CInnerTypePropertyName = "Content";
        public const string SInnerTypePropertyName = "TypedValue";
        public const string InnerTypeParamName = "content";
        public const string SetInnerType = "SetInnerType";
        public const string SetSubstitutionMember = "SetSubstitutionMember";
        public const string ToSubstitutedXTypedElement = "ToSubstitutedXTypedElement";
        public const string ToXTypedElement = "ToXTypedElement";
        public const string XTypedList = "XTypedList";
        public const string XTypedSubstitutedList = "XTypedSubstitutedList";
        public const string XSimpleList = "XSimpleList";
        public const string Default = "Default";
        public const string TypeOrigin = "TypeOrigin";
        public const string Origin = "SchemaOrigin";

        //Prefixes/Suffixes for name handling
        public const string TypeSuffix = "Type";
        public const string LocalTypeSuffix = "LocalType";
        public const string LocalElementConflictSuffix = "";
        public const string LocalAttributeConflictSuffix = "";


        //Contants used in FSM
        public const string FSMMember = "validationStates";
        public const string FSMClass = "FSM";
        public const string GetFSM = "GetValidationStates";
        public const string InitFSM = "InitFSM";
        public const string TransitionsVar = "transitions";
        public const string Int = "System.Int32";
        public const string SingleTrans = "SingleTransition";
        public const string WildCard = "WildCard";

        public const int DecimalMaxPower = 29;
        public const string Datatype = "Datatype";
        public const string SimpleTypeDefInnerType = "TypeDefinition";
        public const string XmlSchemaWhiteSpace = "XmlSchemaWhiteSpace";
        public const string SimpleTypeValidator = "Xml.Schema.Linq.SimpleTypeValidator";
        public const string ListSimpleTypeValidator = "Xml.Schema.Linq.ListSimpleTypeValidator";
        public const string AtomicSimpleTypeValidator = "Xml.Schema.Linq.AtomicSimpleTypeValidator";
        public const string UnionSimpleTypeValidator = "Xml.Schema.Linq.UnionSimpleTypeValidator";
        public const string RestrictionFacets = "Xml.Schema.Linq.RestrictionFacets";
        public const string RestrictionFlags = "Xml.Schema.Linq.RestrictionFlags";
        public const string EqualityCheck = "Equals";
    }
}