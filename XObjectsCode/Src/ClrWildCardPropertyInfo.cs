using System;
using System.CodeDom;
using System.Collections.Generic;
using XObjects;

namespace Xml.Schema.Linq.CodeGen;

internal partial class ClrWildCardPropertyInfo : ClrBasePropertyInfo
{
    string namespaces;
    string targetNamespace;

    bool addToTypeDef;

    public bool AddToTypeDef
    {
        get { return addToTypeDef; }
        set { addToTypeDef = value; }
    }


    internal string Namespaces
    {
        get { return namespaces; }
    }

    internal string TargetNamespace
    {
        get { return targetNamespace; }
    }

    internal override XCodeTypeReference ReturnType
    {
        get { return new XCodeTypeReference("IEnumerable", new CodeTypeReference(Constants.XElement)); }
    }

    internal ClrWildCardPropertyInfo(string ns, string targetNs, bool addToType, Occurs schemaOccurs)
    {
        namespaces = ns;
        targetNamespace = targetNs;
        this.contentType = ContentType.WildCardProperty;
        this.addToTypeDef = addToType;
        this.occursInSchema = schemaOccurs;
    }

    internal override CodeMemberProperty AddToType(CodeTypeDeclaration decl, List<ClrAnnotation> annotations,
        GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
    {
        if (!addToTypeDef) return null;

        CodeMemberProperty property = CodeDomHelper.CreateProperty(ReturnType, false, visibility.ToMemberAttribute());
        property.Name = PropertyName;
        //property.Attributes = (property.Attributes & ~MemberAttributes.AccessMask) | visibility.ToMemberAttribute();

        AddGetStatements(property.GetStatements);

        ApplyAnnotations(property, annotations);

        decl.Members.Add(property);
        return property;
    }

    internal void AddGetStatements(CodeStatementCollection getStatements)
    {
        getStatements.Add(
            new CodeMethodReturnStatement(CodeDomHelper.CreateMethodCall(CodeDomHelper.This(), "GetWildCards",
                CodeDomHelper.CreateFieldReference(Constants.WildCard, "DefaultWildCard")))
        );
    }

    internal override void AddToConstructor(CodeConstructor functionalConstructor)
    {
        throw new InvalidOperationException();
    }

    internal override void AddToContentModel(CodeObjectCreateExpression contentModelExpression)
    {
        throw new InvalidOperationException();
    }
}