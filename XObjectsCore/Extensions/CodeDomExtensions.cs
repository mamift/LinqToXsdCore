using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;

namespace Xml.Schema.Linq.Extensions
{
    public static class CodeDomExtensions
    {
        /// <summary>
        /// Generates code string from the current <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="ccu"></param>
        /// <returns></returns>
        public static StringWriter ToStringWriter(this CodeCompileUnit ccu)
        {
            var stringWriter = new StringWriter();

            var provider = new CSharpCodeProvider();
            var codeGeneratorOptions = new CodeGeneratorOptions();
            provider.GenerateCodeFromCompileUnit(ccu, stringWriter, codeGeneratorOptions);

            return stringWriter;
        }
    }
}