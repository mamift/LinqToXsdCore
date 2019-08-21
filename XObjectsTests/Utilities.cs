using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Xml.Schema.Linq.Tests
{
    public static class Utilities
    {
        public static SourceText GenerateSourceText(string filePath)
        {
            var possibleSettingsFile = $"{filePath}.config";
            KeyValuePair<string, TextWriter> code = File.Exists(possibleSettingsFile)
                ? XObjectsCoreGenerator.Generate(filePath, possibleSettingsFile)
                : XObjectsCoreGenerator.Generate(filePath, (string) null);

            return SourceText.From(code.Value.ToString());
        }
    }
}