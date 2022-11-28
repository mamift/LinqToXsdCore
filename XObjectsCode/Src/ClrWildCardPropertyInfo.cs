using System;
using System.CodeDom;
using System.Collections.Generic;
using XObjects;

namespace Xml.Schema.Linq.CodeGen;

public partial class ClrWildCardPropertyInfo : ClrBasePropertyInfo
{
    string namespaces;
    string targetNamespace;

    bool addToTypeDef;

    public bool AddToTypeDef
    {
        get { return addToTypeDef; }
        set { addToTypeDef = value; }
    }


    public string Namespaces
    {
        get { return namespaces; }
    }

    public string TargetNamespace
    {
        get { return targetNamespace; }
    }

    public override XCodeTypeReference ReturnType
    {
        get { return new XCodeTypeReference("IEnumerable", new CodeTypeReference(Constants.XElement)); }
    }

    public ClrWildCardPropertyInfo(string ns, string targetNs, bool addToType, Occurs schemaOccurs)
    {
        namespaces = ns;
        targetNamespace = targetNs;
        this.contentType = ContentType.WildCardProperty;
        this.addToTypeDef = addToType;
        this.occursInSchema = schemaOccurs;
    }

    public override CodeMemberProperty AddToType(CodeTypeDeclaration decl, List<ClrAnnotation> annotations,
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

    public void AddGetStatements(CodeStatementCollection getStatements)
    {
        getStatements.Add(
            new CodeMethodReturnStatement(CodeDomHelper.CreateMethodCall(CodeDomHelper.This(), "GetWildCards",
                CodeDomHelper.CreateFieldReference(Constants.WildCard, "DefaultWildCard")))
        );
    }

    public override void AddToConstructor(CodeConstructor functionalConstructor)
    {
        throw new InvalidOperationException();
    }

    public override void AddToContentModel(CodeObjectCreateExpression contentModelExpression)
    {
        throw new InvalidOperationException();
    }
}