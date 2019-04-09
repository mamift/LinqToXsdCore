using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Xml.Schema.Linq;

namespace LinqToXsd
{
    public static partial class Program
    {
        public static partial class GenerateHandler
        {
            /// <summary>
            /// Generates C# code.
            /// </summary>
            /// <param name="generateOptions"></param>
            internal static void GenerateCode(GenerateOptions generateOptions)
            {
                var files = generateOptions.SchemaFiles;

                var settings = generateOptions.ConfigInstance;

                settings.EnableServiceReference = generateOptions.EnableServiceReference;

                foreach (var kvp in XObjectsCoreGenerator.Generate(files, settings))
                {
                    var outputFile = $"{kvp.Key}.cs";

                    Console.WriteLine($"Outputting to {Path.GetFullPath(outputFile)}");

                    using (var outputFileStream = File.Open(outputFile, FileMode.Create, FileAccess.ReadWrite))
                    using (var fileWriter = new StreamWriter(outputFileStream))
                    {
                        fileWriter.Write(kvp.Value);
                    }
                }
            }

            /// <summary>
            /// Generates DLLs instead of C# code.
            /// </summary>
            /// <param name="generateOptions"></param>
            internal static void GenerateAssemblies(GenerateOptions generateOptions)
            {
                var provider = new CSharpCodeProvider();
                var options = new CompilerParameters {
                    OutputAssembly = generateOptions.Assembly,
                    IncludeDebugInformation = true,
                    TreatWarningsAsErrors = true,
                    TempFiles = {
                        KeepFiles = true
                    }
                };
                var refs = options.ReferencedAssemblies;
                refs.Add("System.dll");
                refs.Add("System.Core.dll");
                refs.Add("System.Xml.dll");
                refs.Add("System.Xml.Linq.dll");
                refs.Add("Xml.Schema.Linq.dll");

                var codeCompileUnits = XObjectsCoreGenerator.GenerateCodeCompileUnits(generateOptions.SchemaFiles,
                    generateOptions.ConfigInstance);

                foreach (var ccu in codeCompileUnits)
                {
                    var results = provider.CompileAssemblyFromDom(options, ccu.Value);
                    if (results.Errors.Count > 0)
                    {
                        for (var i = 0; i < results.Errors.Count; i++)
                        {
                            Console.WriteLine(results.Errors[i].ToString());
                        }

                        throw new Exception("compilation error(s)");
                    }

                    Console.WriteLine("Generated Assembly: " + results.CompiledAssembly);
                }
            }
        }
    }
}