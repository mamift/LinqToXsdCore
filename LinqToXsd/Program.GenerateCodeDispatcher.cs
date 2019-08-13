using System;
using System.Collections.Generic;
using System.IO;
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
            internal static void HandleWriteOutputToMultipleFiles(GenerateOptions options, Dictionary<string, TextWriter> textWriters)
            {
                var possibleOutputFolder = options.Output;

                PrintLn($"Outputting {textWriters.Count} files...".Gray());
                foreach (var kvp in textWriters)
                {
                    var outputFilename = Path.GetFileName($"{kvp.Key}.cs");
                    string outputFilePath;

                    if (possibleOutputFolder == "-1")
                        outputFilePath = Path.Combine(Path.GetDirectoryName(kvp.Key), outputFilename);
                    else if (possibleOutputFolder.IsNotEmpty())
                        outputFilePath = Path.Combine(possibleOutputFolder, outputFilename);
                    else
                        outputFilePath = Path.GetFullPath(outputFilename);

                    var fullPathOfContainingDir = Path.GetDirectoryName(outputFilePath);

                    if (!Directory.Exists(fullPathOfContainingDir))
                    {
                        PrintLn($"Creating directory: {fullPathOfContainingDir}".Yellow());
                        Directory.CreateDirectory(fullPathOfContainingDir);
                    }

                    PrintLn($"{kvp.Key} => {outputFilePath}".Gray());

                    using (var outputFileStream = File.Open(outputFilePath, FileMode.Create, FileAccess.ReadWrite))
                    using (var fileWriter = new StreamWriter(outputFileStream))
                    {
                        fileWriter.Write(kvp.Value);
                    }
                }
            }

            /// <summary>
            /// Writes generated code to a single output file.
            /// </summary>
            /// <param name="options"></param>
            /// <param name="textWriters"></param>
            internal static void HandleWriteOutputToSingleFile(GenerateOptions options, Dictionary<string, TextWriter> textWriters)
            {
                var outputFile = options.Output;
                // add .cs extension to filename if it doesn't have it already.
                var target = outputFile.EndsWith(".cs") ? outputFile : $"{outputFile}.cs";

                var extractFileNameOnlyFunctor = new Func<string, string>(k => $"'{Path.GetFileName(k)}'");
                PrintLn($"{textWriters.Keys.ToDelimitedString(extractFileNameOnlyFunctor).Yellow()}");
                PrintLn($"\toutput to \n{target}");

                using (var fileStream = File.Open(target, FileMode.Create, FileAccess.ReadWrite))
                using (var fileWriter = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    foreach (var kvp in textWriters) fileWriter.Write(kvp.Value);
                }
            }
        }
    }
}