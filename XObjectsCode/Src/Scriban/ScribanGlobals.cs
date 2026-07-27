#nullable enable 

using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using Scriban;

namespace Xml.Schema.Linq.CodeGen.Scriban;

static class ScribanGlobals
{
    private static Scope scope = new();
    public static void ScopeInit(params string[] names) => scope = new Scope().Init(names);
    public static string ScopeRename(string name) => scope.Add(name);

    public static bool HasRestriction(CompiledFacets facets, RestrictionFlags test)
        => facets.Flags.HasFlag(test);

    public static string ValueLiteral(object value)
    {
        return value switch 
        {
            DateTime dt => $"new System.DateTime({dt.Ticks})",
            TimeSpan ts => $"new TimeSpan({ts.Ticks})",
            Uri uri => $"new Uri({JsonSerializer.Serialize(uri.OriginalString)})",
            byte[] bytes => $"new byte[] { string.Join(", ", bytes) }",
            // For most types JSON serialization and C# agree on representation :)
            _ => JsonSerializer.Serialize(value),
        };
    }

    public static string LocalName(string fqn)
    {
        int dot = fqn.LastIndexOf('.');
        return dot < 0 ? fqn : fqn.Substring(dot + 1);
    }

    public static string? Builtin(string? type)
    {
        if (type is null) return null;
        if (type == "") return "void";

        // FIXME: it would just be nicer to remove this, but I'm matching the legacy generation 1:1 for now
        if (type.EndsWith("?")) return type;

        return Regex.Replace(
            type,
             @"\bSystem\.[A-Za-z0-9]+",
             match => AsKeyword(match.Value)
        );
        
        static string AsKeyword(string type) => type switch 
        {
            "System.Int16" => "short",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.String" => "string",
            "System.Object" => "object",
            "System.Boolean" => "bool",
            "System.Void" => "void",
            "System.Char" => "char",
            "System.Byte" => "byte",
            "System.UInt16" => "ushort",
            "System.UInt32" => "uint",
            "System.UInt64" => "ulong",
            "System.SByte" => "sbyte",
            "System.Single" => "float",
            "System.Double" => "double",
            "System.Decimal" => "decimal",
            _ => type,
        };
    }    

    // Unfortunate usage of non-generic IEnumerable here to account for Scriban ScriptRange object
    // created in attribute.scriban-cs by using array.concat from several comment sources :(
    public static void Comments(TemplateContext ctx, System.Collections.IEnumerable? lines, string prefix)
    {
        if (lines is null) return;

        var enumerator = lines.GetEnumerator();
        if (!enumerator.MoveNext()) return;

        ctx.Write(prefix).Write(" <summary>\n");
        do 
        {
            var line = ((string)enumerator.Current).Replace("\n", "\n" + prefix);
            ctx.Write(prefix).Write(" <para>\n");
            ctx.Write(prefix).Write(" ").Write(line).Write("\n");
            ctx.Write(prefix).Write(" </para>\n");
        } while (enumerator.MoveNext());     
        ctx.Write(prefix).Write(" </summary>\n");
        ctx.ResetPreviousNewLine();
        ctx.Write(ctx.CurrentIndent);
    }
}