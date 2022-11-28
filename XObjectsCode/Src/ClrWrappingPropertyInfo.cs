using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using XObjects;

namespace Xml.Schema.Linq.CodeGen;

public class ClrWrappingPropertyInfo : ClrBasePropertyInfo
{
    string wrappedFieldName;
    const int propertySuffixIndex = 1;
    MemberAttributes wrappedPropertyAttributes;
    CodeCommentStatementCollection codeCommentStatementCollection;

    public ClrWrappingPropertyInfo()
    {
        this.contentType = ContentType.Property;
    }

    public void Init(CodeMemberProperty property)
    {
        propertyName = property.Name;
        returnType = (XCodeTypeReference) property.Type;
        if (returnType.fullTypeName != null && returnType.fullTypeName != returnType.BaseType)
        {
            returnType = new XCodeTypeReference(returnType.fullTypeName);
        }

        hasSet = property.HasSet;
        wrappedPropertyAttributes = property.Attributes;

        codeCommentStatementCollection = property.Comments;
    }

    public string WrappedFieldName
    {
        get { return wrappedFieldName; }
        set { wrappedFieldName = value; }
    }

    public override CodeMemberProperty AddToType(CodeTypeDeclaration typeDecl, List<ClrAnnotation> annotations, 
        GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public)
    {
        CodeMemberProperty wrapperProperty = CodeDomHelper.CreateProperty(this.returnType, this.hasSet, visibility.ToMemberAttribute());
        wrapperProperty.Name = CheckPropertyName(typeDecl.Name);
        wrapperProperty.Attributes = this.wrappedPropertyAttributes;

        AddGetStatements(wrapperProperty.GetStatements);

        if (this.hasSet)
        {
            AddSetStatements(wrapperProperty.SetStatements);
        }

        ApplyAnnotations(wrapperProperty, annotations);

        typeDecl.Members.Add(wrapperProperty);
        return wrapperProperty;
    }

    private string CheckPropertyName(string className)
    {
        if (this.propertyName.Equals(className))
        {
            //This can happen as property names are checked against the wrapped complex type name and not against the global element name
            return this.propertyName + propertySuffixIndex.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        return this.propertyName;
    }

    private void AddGetStatements(CodeStatementCollection getStatements)
    {
        getStatements.Add(new CodeMethodReturnStatement
        (new CodePropertyReferenceExpression(
            new CodeFieldReferenceExpression(
                new CodeThisReferenceExpression(),
                this.wrappedFieldName),
            this.propertyName)));
    }

    private void AddSetStatements(CodeStatementCollection setStatements)
    {
        setStatements.Add(new CodeAssignStatement(
            new CodePropertyReferenceExpression(
                new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(),
                    this.wrappedFieldName),
                this.propertyName),
            CodeDomHelper.SetValue()));
    }

    public override void AddToConstructor(CodeConstructor functionalConstructor)
    {
        //Do nothing for now
        //If wrapped property has setter, then add it to func const
    }

    public override void AddToContentModel(CodeObjectCreateExpression contentModelExpression)
    {
        throw new InvalidOperationException();
    }

    public override void ApplyAnnotations(CodeMemberProperty propDecl, List<ClrAnnotation> annotations)
    {
        foreach (CodeCommentStatement comm in codeCommentStatementCollection)
        {
            propDecl.Comments.Add(new CodeCommentStatement(comm.Comment.Text, comm.Comment.DocComment));
        }
    }
}