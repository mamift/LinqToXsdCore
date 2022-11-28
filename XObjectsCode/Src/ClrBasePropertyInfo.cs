using System;
using System.CodeDom;
using System.Collections.Generic;

namespace Xml.Schema.Linq.CodeGen;

internal abstract class ClrBasePropertyInfo : ContentInfo
{
    protected string propertyName;
    protected string schemaName;
    protected string propertyNs;

    protected bool hasSet;
    protected XCodeTypeReference returnType;
    protected XCodeTypeReference defaultValueType;
    protected bool isVirtual;
    protected bool isOverride;

    //Intellisense type information
    protected List<ClrAnnotation> annotations;

    public ClrBasePropertyInfo()
    {
        this.IsVirtual = false;
        this.isOverride = false;
        this.returnType = null;
        this.defaultValueType = null;
        annotations = new List<ClrAnnotation>();
    }

    internal string PropertyName
    {
        get { return propertyName; }
        set { propertyName = value; }
    }

    internal string SchemaName
    {
        get { return schemaName; }
        set { schemaName = value; }
    }

    internal string PropertyNs
    {
        get { return propertyNs; }
        set { propertyNs = value; }
    }

    internal virtual bool IsList
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    internal bool HasSet
    {
        get { return hasSet; }
        set { hasSet = value; }
    }

    internal virtual bool FromBaseType
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    internal virtual bool IsNew
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    internal virtual bool IsDuplicate
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    internal virtual bool IsNullable
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    internal virtual bool VerifyRequired
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    internal virtual XCodeTypeReference ReturnType
    {
        get { return returnType; }
        set { returnType = value; }
    }

    internal virtual XCodeTypeReference DefaultValueType
    {
        get { return defaultValueType; }
        set { defaultValueType = value; }
    }

    internal virtual string ClrTypeName
    {
        get { return null; }
    }

    internal bool IsVirtual
    {
        get { return this.isVirtual; }
        set { this.isVirtual = value; }
    }

    internal bool IsOverride
    {
        get { return this.isOverride; }
        set { this.isOverride = value; }
    }

    internal virtual bool IsUnion
    {
        get { throw new InvalidOperationException(); }
        set { throw new InvalidOperationException(); }
    }

    internal virtual bool IsEnum
    {
        get { throw new InvalidOperationException(); }
        set { throw new InvalidOperationException(); }
    }

    internal virtual bool IsSchemaList
    {
        get { throw new InvalidOperationException(); }
        set { throw new InvalidOperationException(); }
    }

    internal List<ClrAnnotation> Annotations
    {
        get { return annotations; }
    }

    internal bool ShouldGenerate => !IsDuplicate && (!FromBaseType || IsNew);

    internal abstract CodeMemberProperty AddToType(CodeTypeDeclaration decl, List<ClrAnnotation> annotations, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public);
    internal abstract void AddToContentModel(CodeObjectCreateExpression contentModelExpression);
    internal abstract void AddToConstructor(CodeConstructor functionalConstructor);

    internal virtual void ApplyAnnotations(CodeMemberProperty propDecl, List<ClrAnnotation> typeAnnotations)
    {
        TypeBuilder.ApplyAnnotations(propDecl, this, typeAnnotations);
    }

    internal virtual CodeExpression GetXName()
    {
        return CodeDomHelper.XNameGetExpression(SchemaName, PropertyNs);
    }

    public override string ToString() => this.propertyName;
}