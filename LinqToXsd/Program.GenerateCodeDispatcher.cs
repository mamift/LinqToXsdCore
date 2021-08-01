using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Alba.CsConsoleFormat.Fluent;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    public static partial class Program
    {
        /// <summary>
        /// Handles the logic for generating code.
        /// </summary>
        internal static class GenerateCodeDispatcher
        {
            /// <summary>
            /// Writes generated code for to multiple output files, inferred by <see cref="GenerateOptions.Output"/>.
            /// </summary>
            internal static void HandleWriteOutputToMultipleFiles(GenerateOptions options, Dictionary<string, List<(string, StringWriter)>> textWriters)
            {
                var possibleOutputFolder = options.Output;

                PrintLn($"Outputting code for {textWriters.Count} XSD file(s)...".Gray());
                foreach (var currentSchemaWriters in textWriters) {
                    var outputFilename = Path.GetFileName($"{currentSchemaWriters.Key}.cs");
                    string outputFilePath;

                    if (possibleOutputFolder == "-1")
                        outputFilePath = Path.Combine(Path.GetDirectoryName(currentSchemaWriters.Key), outputFilename);
                    else if (possibleOutputFolder.IsNotEmpty())
                        outputFilePath = Path.Combine(possibleOutputFolder, outputFilename);
                    else
                        outputFilePath = Path.GetFullPath(outputFilename);

                    var outputDir = Path.GetDirectoryName(outputFilePath);

                    if (!Directory.Exists(outputDir)) {
                        PrintLn($"Creating directory: {outputDir}".Yellow());
                        Directory.CreateDirectory(outputDir);
                    }
                    
                    var schemaCodeWriters = currentSchemaWriters.Value;

                    if (schemaCodeWriters.Count > 1) {
                        PrintLn($"Outputing {schemaCodeWriters.Count} C# code files...".Gray());
                        foreach (var (id, codeWriter) in schemaCodeWriters) {
                            var specificOutputFilePath = Path.Combine(outputDir, id.AppendIfNotPresent(".cs"));
                            using (var outputFileStream = File.Open(specificOutputFilePath, FileMode.Create, FileAccess.ReadWrite))
                            using (var fileWriter = new StreamWriter(outputFileStream)) {
                                fileWriter.Write(codeWriter);
                            }
                        }
                    }
                    else {
                        PrintLn($"Outputing to {outputFilePath}...".Gray());
                        using (var outputFileStream = File.Open(outputFilePath, FileMode.Create, FileAccess.ReadWrite))
                        using (var fileWriter = new StreamWriter(outputFileStream)) {
                            fileWriter.Write(schemaCodeWriters.Single().Item2);
                        }
                    }
                }
            }

            /// <summary>
            /// Writes generated code to a single output file.
            /// </summary>
            /// <param name="options"></param>
            /// <param name="schemaTextWriters"></param>
            internal static void HandleWriteOutputToSingleFile(GenerateOptions options, Dictionary<string, List<(string, StringWriter)>> schemaTextWriters)
            {
                var outputFile = options.Output;
                // add .cs extension to filename if it doesn't have it already.
                var target = outputFile.EndsWith(".cs") ? outputFile : $"{outputFile}.cs";

                var extractFileNameOnlyFunctor = new Func<string, string>(k => $"'{Path.GetFileName(k)}'");
                PrintLn($"{schemaTextWriters.Keys.ToDelimitedString(extractFileNameOnlyFunctor).Yellow()}");
                PrintLn($"\toutput to \n{target}");

                using (var fileStream = File.Open(target, FileMode.Create, FileAccess.ReadWrite))
                using (var fileWriter = new StreamWriter(fileStream, Encoding.UTF8)) {
                    foreach (var schemaTextWriter in schemaTextWriters) {
                        foreach (var writerTuple in schemaTextWriter.Value) {
                            fileWriter.Write(writerTuple.Item2);
                        }
                    }
                }
            }

            /// <summary>
            /// Writes generated code to a single output file.
            /// </summary>
            /// <param name="options"></param>
            /// <param name="textWriters"></param>
            internal static void HandleWriteOutputForMultipleTextWriters(GenerateOptions options, Dictionary<string, List<(string,
                TextWriter)>> textWriters)
            {
                foreach (var schemaTextWriterPair in textWriters) {
                    var outputFile = schemaTextWriterPair.Key;
                    // add .cs extension to filename if it doesn't have it already.
                    var target = outputFile.EndsWith(".cs") ? outputFile : $"{outputFile}.cs";
                
                    PrintLn($"{textWriters.Keys.ToDelimitedString(k => $"'{Path.GetFileName(k)}'").Yellow()}");
                    PrintLn($"\toutput to \n{target}");

                    using (FileStream fileStream = File.Open(target, FileMode.Create, FileAccess.ReadWrite))
                    using (var fileWriter = new StreamWriter(fileStream, Encoding.UTF8)) {
                        foreach (var writer in schemaTextWriterPair.Value) {
                            fileWriter.Write(writer);
                        }
                    }
                }
            }
        }
    }
}