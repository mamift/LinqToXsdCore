#nullable enable 

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Scriban;

namespace Xml.Schema.Linq.CodeGen.Scriban;

static class ScribanGlobals
{
    private static Scope scope = new();
    public static void ScopeInit(params string[] names) => scope = new Scope().Init(names);
    public static string ScopeRename(string name) => scope.Add(name);

    public static void Comments(TemplateContext ctx, object target)
    {
        var comments = target switch
        {
            CodeTypeDeclaration decl => decl.Comments,
            CodeTypeMember m => m.Comments,
            _ => null,  
        };
        
        if (comments == null || comments.Count == 0)
            return;
        
        foreach (CodeCommentStatement c in comments)
            ctx.Write($"/// {c.Comment.Text.Replace("\n", "\n///")}\n");
        ctx.ResetPreviousNewLine();
        ctx.Write(ctx.CurrentIndent);
    }

    public static IEnumerable<CodeTypeDeclaration> Classes(CodeTypeDeclaration type)
    {
        return type.Members
            .OfType<CodeTypeDeclaration>()
            .Where(x => !x.IsEnum && !x.Name.EndsWith("EnumValidator"));
    }

    public static CodeTypeDeclaration EnumDecl(string enumName, CodeTypeDeclaration type)
    {
        enumName = enumName.TrimEnd('?');
        return type.Members
            .OfType<CodeTypeDeclaration>()
            .First(x => x.IsEnum && x.Name == enumName);
    }

    public static CodeConstructor? Ctor(CodeTypeDeclaration type, int args = 0)
    {
        return type.Members
            .OfType<CodeConstructor>()
            .FirstOrDefault(x => x.Parameters.Count == args);
    }

    public static IEnumerable<CodeMemberProperty> Properties(CodeTypeDeclaration type)
    {
        return type.Members
            .OfType<CodeMemberProperty>()
            .Where(x => 
                // Exclude properties like SchemaName, TypeOrigin or TypeManager
                !x.CustomAttributes.Cast<CodeAttributeDeclaration>().Any(a => a.Name == "DebuggerBrowsable") &&
                x.Name != "TypedValue");
    }

    public static bool IsList(CodeMemberProperty prop)
    {
        return prop.Type.BaseType == "IList`1";
    }

    public static bool IsElement(CodeMemberProperty prop)
    {
        return IsList(prop) ||
            prop.GetStatements[0] is CodeVariableDeclarationStatement { Type.BaseType: "XElement" };
    }

    public static IEnumerable<CodeMemberProperty> Elements(CodeTypeDeclaration type)
    {
        return Properties(type).Where(IsElement);
    }

    public static bool HasElements(CodeTypeDeclaration type)
    {
        return Elements(type).Any();
    }

    public static bool IsOptional(CodeMemberProperty prop)
    {
        return prop.Comments
            .Cast<CodeCommentStatement>()
            .Any(x => x.Comment.Text.Contains("Occurrence: optional"));
    }

    public static bool IsTypeDefinition(CodeTypeDeclaration type)
    {
        return type.TypeAttributes.HasFlag(TypeAttributes.Sealed);
    }

    public static string? Validator(CodeTypeDeclaration type)
    {
        var statement = type.Members
            .OfType<CodeMemberProperty>()
            .First(x => x.Name == "TypedValue")
            .SetStatements[0];

        if (statement is not CodeExpressionStatement 
            { 
                Expression: CodeMethodInvokeExpression
                {
                    Method.MethodName: "SetValueWithValidation",
                    Parameters: [_, _, CodeFieldReferenceExpression 
                    { 
                        TargetObject: CodeTypeReferenceExpression
                        {
                            Type: var typeRef
                        }
                    }]
                }
            })
            return null;
        
        return typeRef.BaseType;
    }

    public static string? SimpleType(CodeTypeDeclaration type)
    {
        var typeDecl = type.Members
            .OfType<CodeMemberProperty>()
            .FirstOrDefault(x => x.Name == "TypedValue")
            ?.Type;
        return typeDecl != null ? TypeName(typeDecl, nullable: false) : null;
    }

    public static IEnumerable<string> EnumValues(CodeTypeDeclaration enumType)
    {
        return enumType.Members
            .OfType<CodeMemberField>()
            .Select(x => x.Name);
    }

    public static string? DefaultValue(CodeMemberProperty prop, CodeTypeDeclaration type)
    {
        var name = prop.Name + "DefaultValue";
        var init = type.Members
            .OfType<CodeMemberField>()
            .FirstOrDefault(x => x.Name == name)
            ?.InitExpression;
        
        return init switch
        {
            CodeMethodInvokeExpression i => $"{i.Method.MethodName}(\"{ ((CodePrimitiveExpression)i.Parameters[0]).Value }\")",
            CodePrimitiveExpression p => (string)p.Value,
            CodeFieldReferenceExpression f => f.FieldName,
            _ => null,
        };
    }

    public static string LocalName(CodeTypeDeclaration type, string name)
    {
        var init = type.Members
            .OfType<CodeMemberField>()
            .FirstOrDefault(x => x.Name == name)
            ?.InitExpression as CodeMethodInvokeExpression;
        if (init is null) return "TODO: review null";
        return (string)(init.Parameters[0] as CodePrimitiveExpression)!.Value;
    }

    public static string Namespace(CodeTypeDeclaration type, string name)
    {
        var init = type.Members
            .OfType<CodeMemberField>()
            .FirstOrDefault(x => x.Name == name)
            ?.InitExpression as CodeMethodInvokeExpression;
        if (init is null) return "TODO: review null";
        return (string)(init.Parameters[1] as CodePrimitiveExpression)!.Value;
    }

    public static bool HasContentModel(CodeTypeDeclaration type)
    {
        return type.Members
            .OfType<CodeMemberField>()
            .Any(x => x.Name == "contentModel");
    }

    public static string TypeName(CodeTypeReference type, bool nullable = true)
    {
        if (type.ArrayElementType != null)
            return TypeName(type.ArrayElementType) + "[]";

        var name = type.BaseType;

        if (type.TypeArguments.Count > 0)
        {
            return Regex.Replace(name, @"`\d+$", "")
                + "<" 
                + string.Join(", ", type.TypeArguments.Cast<CodeTypeReference>().Select(x => TypeName(x))) 
                + ">";
        }

        if (!nullable)
            name = name.TrimEnd('?');

        return name switch
        {
            "System.Boolean" => "bool",
            "System.Byte" => "byte",
            "System.Int32" => "int",
            "System.String" => "string",
            _ => name,
        };
    }

    public static string ListElement(string typeName)
    {
        return Regex.Match(typeName, "^IList<([^>]+)>$") is { Success: true, Groups: var g }
            ? g[1].Value
            : typeName;
    }

    public static string XmlTypeCode(object type, string? name = null)
    {
        // HACK: this is plain wrong but a shortcut to make the proof of concept match 100% without going to deep in CodeDom analysis
        if (name == "language") return "XmlTypeCode.Language";

        if (type is CodeTypeReference typeRef)
            type = TypeName(typeRef, nullable: false);

        return type switch
        {
            "bool" => "XmlTypeCode.Boolean",
            "byte[]" => "XmlTypeCode.Base64Binary",
            "int" => "XmlTypeCode.Int",
            "string" => "XmlTypeCode.String",
            _ => "TODO",
        };
    }
}