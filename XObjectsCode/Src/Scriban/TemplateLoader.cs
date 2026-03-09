using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.IO;
using System.Threading.Tasks;

namespace Xml.Schema.Linq.CodeGen.Scriban;

class TemplateLoader : ITemplateLoader
{
    static string FullPath(string name)
    {
        return Path.Combine(
            Path.GetDirectoryName(typeof(TemplateLoader).Assembly.Location),
            "Templates",
            name);
    }

    public static Template Load(string name)
    {
        var file = FullPath(name);
        return Template.Parse(File.ReadAllText(file), file);
    }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        => FullPath(templateName);

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        => File.ReadAllText(templatePath);

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        => new ValueTask<string>(File.ReadAllText(templatePath));
}
