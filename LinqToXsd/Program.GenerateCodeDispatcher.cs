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
        /// The extension to insert into the file name for generated code files. Should result in "filename.xsd-g.cs".
        /// </summary>
        internal static readonly string GenerateCodeExtension = "-g.cs";
        
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
                string possibleOutputFolder = options.Output;

                PrintLn(($"Outputting {textWriters.Count} files..." + Environment.NewLine).Gray());
                foreach (var kvp in textWriters)
                {
                    var outputFilename = Path.GetFileName(kvp.Key);
                    var legacyOutputFileName = Path.GetFileName(kvp.Key) + ".cs";
                    if (!outputFilename.EndsWith(".cs"))
                    {
                        outputFilename += GenerateCodeExtension;
                    }

                    string outputFilePath;
                    string legacyOutputFilePath;
                    if (possibleOutputFolder == "-1")
                    {
                        string directoryName = Path.GetDirectoryName(kvp.Key) ??
                                               throw new InvalidOperationException("Unable to get dir for: " + kvp.Key);
                        outputFilePath = Path.Combine(directoryName, outputFilename);
                        legacyOutputFilePath = Path.Combine(directoryName, legacyOutputFileName);
                    }
                    else
                    {
                        if (possibleOutputFolder.IsNotEmpty())
                        {
                            outputFilePath = Path.Combine(possibleOutputFolder, outputFilename);
                            legacyOutputFilePath = Path.Combine(possibleOutputFolder, legacyOutputFileName);
                        }
                        else
                        {
                            outputFilePath = Path.GetFullPath(outputFilename);
                            legacyOutputFilePath = Path.GetFullPath(legacyOutputFileName);
                        }
                    }

                    if (File.Exists(legacyOutputFilePath))
                    {
                        PrintLn("NOTE: Since v3.4.17, the default output file extension has changed. ".Yellow());
                        PrintLn($"You should delete the existing source code file: {legacyOutputFileName}".DarkYellow());
                    }

                    var fullPathOfContainingDir = Path.GetDirectoryName(outputFilePath);

                    if (!Directory.Exists(fullPathOfContainingDir))
                    {
                        PrintLn($"Creating directory: {fullPathOfContainingDir}".Yellow());
                        Directory.CreateDirectory(fullPathOfContainingDir);
                    }


                    PrintLn($"Source:".White());
                    PrintLn(kvp.Key.Gray());
                    
                    
                    PrintLn($"Output:".White());
                    PrintLn(outputFilePath.DarkGreen());

                    using (var outputFileStream = File.Open(outputFilePath, FileMode.Create, FileAccess.ReadWrite))
                    using (var fileWriter = new StreamWriter(outputFileStream))
                    {
                        fileWriter.Write(kvp.Value);
                    }

                    PrintLn("");
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