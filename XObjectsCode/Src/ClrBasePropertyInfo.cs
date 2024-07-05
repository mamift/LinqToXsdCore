using System;
using System.CodeDom;
using System.Collections.Generic;

namespace Xml.Schema.Linq.CodeGen;

public abstract class ClrBasePropertyInfo : ContentInfo
{
    protected string propertyName;
    protected string schemaName;
    protected string propertyNs;

    protected bool hasSet;
    protected XCodeTypeReference returnType;
    protected bool isVirtual;
    protected bool isOverride;

    //Intellisense type information
    protected List<ClrAnnotation> annotations;

    public ClrBasePropertyInfo()
    {
        IsVirtual = false;
        isOverride = false;
        returnType = null;
        annotations = new List<ClrAnnotation>();
    }

    public string PropertyName
    {
        get { return propertyName; }
        set { propertyName = value; }
    }

    public string SchemaName
    {
        get { return schemaName; }
        set { schemaName = value; }
    }

    public string PropertyNs
    {
        get { return propertyNs; }
        set { propertyNs = value; }
    }

    public virtual bool IsList
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    public bool HasSet
    {
        get { return hasSet; }
        set { hasSet = value; }
    }

    public virtual bool FromBaseType
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    public virtual bool IsNew
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    public virtual bool IsDuplicate
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    public virtual bool IsNullable
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    public virtual bool VerifyRequired
    {
        get { return false; }
        set { throw new InvalidOperationException(); }
    }

    public virtual XCodeTypeReference ReturnType
    {
        get { return returnType; }
        set { returnType = value; }
    }

    public virtual string ClrTypeName
    {
        get { return null; }
    }

    public bool IsVirtual
    {
        get { return this.isVirtual; }
        set { this.isVirtual = value; }
    }

    public bool IsOverride
    {
        get { return this.isOverride; }
        set { this.isOverride = value; }
    }

    public virtual bool IsUnion
    {
        get { throw new InvalidOperationException(); }
        set { throw new InvalidOperationException(); }
    }

    public virtual bool IsEnum
    {
        get { throw new InvalidOperationException(); }
        set { throw new InvalidOperationException(); }
    }

    public virtual bool IsSchemaList
    {
        get { throw new InvalidOperationException(); }
        set { throw new InvalidOperationException(); }
    }

    public List<ClrAnnotation> Annotations
    {
        get { return annotations; }
    }

    public bool ShouldGenerate => !IsDuplicate && (!FromBaseType || IsNew);

    public abstract CodeMemberProperty AddToType(CodeTypeDeclaration decl, List<ClrAnnotation> annotations, GeneratedTypesVisibility visibility = GeneratedTypesVisibility.Public);
    public abstract void AddToContentModel(CodeObjectCreateExpression contentModelExpression);
    public abstract void AddToConstructor(CodeConstructor functionalConstructor);

    public virtual void ApplyAnnotations(CodeMemberProperty propDecl, List<ClrAnnotation> typeAnnotations)
    {
        TypeBuilder.ApplyAnnotations(propDecl, this, typeAnnotations);
    }

    public virtual CodeExpression GetXName()
    {
        return CodeDomHelper.XNameGetExpression(SchemaName, PropertyNs);
    }

    public override string ToString() => this.propertyName;
}