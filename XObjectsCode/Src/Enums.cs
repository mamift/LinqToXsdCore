//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Xml.Schema.Linq
{
    public enum SchemaOrigin
    {
        None,
        Element,
        Attribute,
        Fragment,
        Text,
    }

    public enum ContentModelType
    {
        None,
        Sequence,
        Choice,
        All,
    }

    public enum ContainerType
    {
        Attribute,
        Element,
    }

    public enum XmlSchemaWhiteSpace
    {
        Preserve,
        Replace,
        Collapse,
    }

    [FlagsAttribute]
    public enum RestrictionFlags
    {
        Length = 0x0001,
        MinLength = 0x0002,
        MaxLength = 0x0004,
        Pattern = 0x0008,
        Enumeration = 0x0010,
        WhiteSpace = 0x0020,
        MaxInclusive = 0x0040,
        MaxExclusive = 0x0080,
        MinInclusive = 0x0100,
        MinExclusive = 0x0200,
        TotalDigits = 0x0400,
        FractionDigits = 0x0800,
    }

    internal enum EditAction
    {
        None,
        AddBefore,
        AddAfter,
        Append,
        Update,
    }
}

namespace Xml.Schema.Linq.CodeGen
{
    public enum ParticleType
    {
        Empty,
        Sequence,
        Choice,
        All,
        Element,
        Any,
        GroupRef //Pre-compiled
    }

    internal enum Occurs
    {
        One = 1,
        ZeroOrOne = 2,
        ZeroOrMore = 3,
        OneOrMore = 4,
    }

    internal enum NameOptions
    {
        None,
        MakeCollection,
        MakeList,
        MakePlural,
        MakeField,
        MakeParam,
        MakeLocal,
        MakeUnion,
        MakeDefaultValueField,
        MakeFixedValueField,
    }

    internal enum ContentType
    {
        WildCardProperty,
        Property,
        Grouping,
    }

    [Flags]
    internal enum GroupingFlags
    {
        None = 0x0000,
        Nested = 0x0001,
        Repeating = 0x0002,
        HasChildGroups = 0x0004,
        HasRepeatingGroups = 0x0008,
        HasRecurrentElements = 0x0010,
        Optional = 0x0020,
    }

    [Flags]
    internal enum ClrTypeRefFlags
    {
        None = 0x00,
        IsValueType = 0x01,
        IsLocalType = 0x02,
        IsSimpleType = 0x04,
        IsAnyType = 0x08,
        IsElementRef = 0x10,
        IsUnion = 0x20,
        IsEnum = 0x40,
        IsSchemaList = 0x80,
        Validate = 0x100,
    }

    [Flags]
    internal enum ClrTypeFlags
    {
        None = 0x00,
        IsAbstract = 0x01,
        IsSealed = 0x02,
        IsRoot = 0x04,
        IsNested = 0x08,
        InlineBaseType = 0x10,
        IsSubstitutionHead = 0x20,
        HasFixedValue = 0x40,
        HasDefaultValue = 0x80,
        HasElementWildCard = 0x100,
    }

    [Flags]
    internal enum PropertyFlags
    {
        None = 0x00,
        FromBaseType = 0x01,
        IsDuplicate = 0x02,
        HasFixedValue = 0x04,
        HasDefaultValue = 0x08,
        IsNew = 0x10,
        IsList = 0x20,
        IsNullable = 0x40, //For nullability of valueTypes
        VerifyRequired = 0x80,
    }
}