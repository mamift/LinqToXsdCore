using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            /// Writes generated code for to multiple output files, given by a <paramref name="possibleOutputFolder"/>.
            /// </summary>
            internal static void HandleWriteOutputToMultipleFiles(string possibleOutputFolder, Dictionary<string, TextWriter> textWriters)
            {
                Console.WriteLine($"Outputting {textWriters.Count} files...");
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
                        Console.WriteLine($"Creating directory: {fullPathOfContainingDir}");
                        Directory.CreateDirectory(fullPathOfContainingDir);
                    }

                    Console.WriteLine($"{kvp.Key} => {outputFilePath}");

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
            /// <param name="outputFile"></param>
            /// <param name="textWriters"></param>
            internal static void HandleWriteOutputToSingleFile(string outputFile, Dictionary<string, TextWriter> textWriters)
            {
                // add .cs extension to filename if it doesn't have it already.
                var target = outputFile.EndsWith(".cs") ? outputFile : $"{outputFile}.cs";

                var extractFileNameOnlyFunctor = new Func<string, string>(k => $"'{Path.GetFileName(k)}'");
                Console.WriteLine($"{textWriters.Keys.ToDelimitedString(extractFileNameOnlyFunctor)}\n output to {target}");

                using (var fileStream = File.Open(target, FileMode.Create, FileAccess.ReadWrite))
                using (var fileWriter = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    foreach (var kvp in textWriters) fileWriter.Write(kvp.Value);
                }
            }
        }
    }
}