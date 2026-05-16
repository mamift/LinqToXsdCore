#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using XObjects;

namespace Xml.Schema.Linq.CodeGen.Model;

/// <summary>
/// Represents a class in generated code, specialized into various kind of xsd source: simple type, element.
/// </summary>
public abstract class CClass(ClrTypeInfo info)
{
    // Initially null until added to a namespace during processing.
    // Remains null for nested types.
    public CNamespace? Namespace { get; set; }

    public string XName => info.schemaName;
    public string XNamespace => info.schemaNs;
    public virtual string Name => info.clrtypeName;
    public bool IsAbstract => info.IsAbstract;

    public IEnumerable<string> Comments => info.Annotations?.Select(a => a.Text) ?? [];
    public IEnumerable<string> RegexComments 
        => info.Annotations?
            .Where(a => a.Section == "summaryRegEx")
            .Select(a => a.Text) 
            ?? [];

    // This isn't great OOP design but Scriban doesn't have first-class support for OOP (no `is` operator).
    // Maybe it'd be cleaner to put the `is CSimpleType` behind a global function.
    public virtual bool IsSimpleType => false;

    [return: NotNullIfNotNull(nameof(name))]
    protected static string? QualifiedName(string? ns, string? name, bool global = false)
    {
        if (name == null) return null;
        var fqn = string.IsNullOrWhiteSpace(ns) ? name : ns + "." + name;
        return global ? "global::" + fqn : fqn;
    }
}

/// <summary>
/// Represents a class depicting an xsd element in generated code
/// </summary>
public class CElement(ClrContentTypeInfo info) : CClass(info), IHasTypes
{
    public SchemaOrigin Origin => info.typeOrigin;
    public string Fqn => QualifiedName(Namespace!.Name, Name);
    public bool IsSealed => info.IsSealed;
    public bool IsSubstitutionHead => info.IsSubstitutionHead;
    public bool IsDerived => info.IsDerived;
    public string BaseTypeName => QualifiedName(info.baseTypeClrNs, info.baseTypeClrName, global: true) ?? "XTypedElement";
    public CClass? BaseType { get; internal set; }
    public bool HasSaveMethods => info.typeOrigin == SchemaOrigin.Element && !info.IsDerived;
    public bool HasLoadMethods => info.typeOrigin == SchemaOrigin.Element;

    public bool HasWildcard => info.HasElementWildCard;

    private FSM? fsm;
    private HashSet<int>? reachableStates;

    // Only meaningful for elements with wildcards
    public IEnumerable<FSMTransition> FSMTransitions
    {
        get 
        {            
            fsm = info.CreateFSM(new StateNameSource());
            reachableStates = new HashSet<int>();
            return AddTransition(fsm.Start);
            
            IEnumerable<FSMTransition> AddTransition(int state)
            {
                if (!reachableStates.Add(state)) yield break;
                if (!fsm.Trans.TryGetValue(state, out var trans) || trans.Count == 0) yield break;
                var subStates = new HashSet<int>();
                
                foreach (var t in trans.nameTransitions ?? []) 
                {
                    subStates.Add(t.Value);
                    yield return new FSMTransition(t.Value) { XName = t.Key };
                }

                foreach (var t in trans.wildCardTransitions ?? []) 
                {
                    subStates.Add(t.Value);
                    yield return new FSMTransition(t.Value) { WildCard = t.Key };
                }

                foreach (var result in subStates.SelectMany(AddTransition))
                    yield return result;
            }
        }
    }

    // Only read after reading FSMTransitions, which initializes `fsm`
    public int FSMStartState => fsm!.Start;

    // Only read after enumerating FSMTransitions, which initializes `fsm` and visits the graph to create `reachableStates`
    public IEnumerable<int> FSMAcceptStates => fsm!.Accept.Intersect(reachableStates);

    public class FSMTransition(int state)
    {
        public int State => state;
        public WildCard? WildCard { get; init; } 
        public string WildCardNs => WildCard!.NsList.Namespaces;
        public string WildCardTargetNs => WildCard!.NsList.TargetNamespace;
        public XName? XName { get; init; }
    }

    public IEnumerable<CContent> Content => info.Content.SelectMany(FlattenContents).Where(x => x.ShouldGenerate);
    public bool HasContentModel => Group?.Type != null;
    public bool HasGroups => info.Content.Any(x => x.ContentType == ContentType.Grouping);
    public ContentGroup? Group => info.Content
        .Where(x => x.ContentType == ContentType.Grouping)
        .Select(x => new ContentGroup(x))
        .FirstOrDefault();    

    public IEnumerable<CContent> LocalElements => info.Content.SelectMany(FlattenContents).Where(x => x.IsLocalElement);

    public List<CClass> Types { get; } = [];

    public void Add(CClass type) => Types.Add(type);
    
    private IEnumerable<CContent> FlattenContents(ContentInfo info)
    {
        return info.ContentType switch 
        {
            ContentType.Property => [new CAttribute((ClrPropertyInfo)info, this)],
            ContentType.Grouping => info.Children.SelectMany(FlattenContents),
            ContentType.WildCardProperty => [new CAny((ClrWildCardPropertyInfo)info, this)],
            _ => [],
        };
    }

    public class ContentGroup(ContentInfo info)
    {
        public string? Type => info switch {
            GroupingInfo { ContentModelType: ContentModelType.Sequence } => "Sequence",
            GroupingInfo { ContentModelType: ContentModelType.Choice } => "Choice",
            ClrPropertyInfo { IsSubstitutionHead: true } => "Substituted",
            ClrPropertyInfo => "Named",
            _ => null,
        };

        public string? XNameField => info is ClrPropertyInfo p ? p.PropertyName + "XName" : null;

        public IEnumerable<XmlSchemaElement> SubstitutionMembers => info is ClrPropertyInfo p ? p.SubstitutionMembers : [];

        public IEnumerable<ContentGroup> Children => info is GroupingInfo g 
            ? g.Children.Select(x => new ContentGroup(x)) 
            : [];
    }    

    public IEnumerable<CAttribute> ChoiceConstructors
    {
        get 
        {
            Dictionary<string, CAttribute>? choices = null;

            foreach (var group in info.Content.SelectMany(TraverseGroupInfos))
            {
                if (!group.InChoice || group.IsNested || group.IsRepeating || group.HasChildGroups) continue;
                choices ??= new();
                foreach (var child in group.Children)
                {
                    if (child is not ClrPropertyInfo choiceInfo) continue;
                    // FIXME: TryAdd not avail. in netstandard 2.0
                    if (choices.ContainsKey(choiceInfo.ReturnTypeStr)) return [];
                    choices.Add(choiceInfo.ReturnTypeStr, new CAttribute(choiceInfo, this));
                }
            }
            
            return choices != null ? choices.Values : [];
        }
    }

    private static IEnumerable<GroupingInfo> TraverseGroupInfos(ContentInfo info)
    {
        if (info is not GroupingInfo g) yield break;
        yield return g;
        foreach (var x in g.Children.SelectMany(TraverseGroupInfos)) 
            yield return x;
    }
}

public class CElementWrapper(ClrWrapperTypeInfo info, CClass wrapped, string innerTypeName) : CClass(info)
{
    public string Fqn => QualifiedName(Namespace!.Name, Name);
    public CClass WrappedType => wrapped;
    public string WrappedName => innerTypeName; // same-ish as wrapped.Name but sometimes prefixed with namespace, sometimes not
    public bool WrappedIsAbstract => wrapped.IsAbstract;
    public bool IsDerived => info.IsDerived;
    public string BaseTypeName => QualifiedName(info.baseTypeClrNs, info.baseTypeClrName, global: true) ?? "XTypedElement";
    public bool HasSaveMethods => info.typeOrigin == SchemaOrigin.Element && !info.IsDerived;
    public IEnumerable<CContent> Content => WalkBaseTypes(wrapped);
    public bool IsSubstitutionHead => info.IsSubstitutionHead;
    public bool IsSubstitutionMember => info.IsSubstitutionMember();

    private static IEnumerable<CContent> WalkBaseTypes(CClass? wrapped)
    {
        // FIXME: this is more complicated than it needs, but it matches legacy codegen 1:1.
        //        All properties are found in e.Content if we don't filter on ShouldGenerate.
        //        The concrete type regex for the comments is also more complete/correct.
        //        The "new" property modifier could simply be derived from ShouldGenerate.
        // FIXME FIXME: actually... those inherited members hide their base counterparts (sometimes with 'new', sometimes not),
        //              why not just skip their generation as they are already declared in base type??
        while (wrapped is CElement e) 
        {
            foreach (var c in e.Content) yield return c;
            wrapped = e.BaseType;
        }
    }
}

/// <summary>
/// Represents a class depicting an xsd simple type wrapper in generated code
/// </summary>
public class CSimpleType(
    ClrSimpleTypeInfo info,
    Dictionary<XmlSchemaObject, string> nameMappings,
    LinqToXsdSettings settings)
    : CClass(info)
{
    public override bool IsSimpleType => true;

    public bool IsGlobal => info.IsGlobal;

    // For enums, the clrtypeName is used for `enum` declaration, 
    // so suffix "Validator" is added to the simple type class holding the `TypeDefinition`.
    public override string Name { get; } = info.clrtypeName + (info is EnumSimpleTypeInfo ? "Validator" : "");

    public string FullyQualifiedName { get; } = info.FullyQualifiedName(nameMappings, settings);

    public XmlSchemaDatatypeVariety Variety => info.Variety;
    public XmlTypeCode XmlTypeCode => info.TypeCode;

    public CompiledFacets Restrictions => info.RestrictionFacets;
    
    // public IEnumerable<string> Comments 
    //     => Namespace is null 
    //     ? []    // no comments on nested simple types
    //     : info.Annotations?.Select(a => a.Text) ?? [];

    public bool IsEnum => info is EnumSimpleTypeInfo;
    public string EnumName => info.clrtypeName;
    public IEnumerable<string> EnumValues 
        => ((EnumSimpleTypeInfo)info).InnerType
            .GetEnumFacets()
            .Select(x => x.Member);

    public bool IsList => info is ListSimpleTypeInfo;
    public CSimpleType ListItemType => new CSimpleType(
        ((ListSimpleTypeInfo)info).ItemType,
        nameMappings,
        settings);

    public bool IsUnion => info is UnionSimpleTypeInfo;
    public IEnumerable<CSimpleType> UnionTypes 
        => ((UnionSimpleTypeInfo)info).MemberTypes
            .Select(x => new CSimpleType(x, nameMappings, settings));
}

public class CSimpleTypeWrapper(ClrWrapperTypeInfo info, ClrPropertyInfo propertyInfo) : CClass(info)
{
    public override bool IsSimpleType => true;

    public string Fqn => QualifiedName(Namespace!.Name, Name);
    public bool HasSaveMethods => info.typeOrigin == SchemaOrigin.Element && !info.IsDerived;
    public bool IsDerived => info.IsDerived;
    public string BaseTypeName => QualifiedName(info.baseTypeClrNs, info.baseTypeClrName, global: true) ?? "XTypedElement";
    public string WrappedName => info.InnerType.IsSchemaList 
        ? $"IList<{propertyInfo.ClrTypeName}>" 
        : propertyInfo.ClrTypeName;
    public bool IsSubstitutionHead => info.IsSubstitutionHead;
    public bool IsSubstitutionMember => info.IsSubstitutionMember();
    public bool NeedsEnumParse => info.InnerType.IsEnum && !info.InnerType.IsEquivalentTo(info.clrtypeName);
    public string EnumType => info.InnerType.ClrFullTypeName;
    public CAttribute TypedValueProperty => new CAttribute(propertyInfo, this);
    public bool HasWildcard => info.HasElementWildCard; // Unused?    
    // public XmlTypeCode XmlTypeCode => info.InnerType.TypeCode;
}