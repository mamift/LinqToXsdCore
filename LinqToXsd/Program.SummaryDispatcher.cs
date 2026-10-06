#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat.Fluent;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    public static partial class Program
    {
        /// <summary>
        /// Handles the logic for the 'sum' verb: writing markdown summaries of generated code files.
        /// </summary>
        internal static class SummaryDispatcher
        {
            /// <summary>
            /// Summarises each given .xsd-g.cs file into a markdown document of the same filename with a .md extension.
            /// </summary>
            /// <param name="summaryOptions"></param>
            internal static void HandleSummary(SummaryOptions summaryOptions)
            {
                var inputFiles = ResolveInputFiles(summaryOptions);
                if (!inputFiles.Any()) return; // an error was already reported

                var outputFolder = summaryOptions.Output;
                if (outputFolder.IsNotEmpty() && !Directory.Exists(outputFolder)) {
                    PrintLn($"Creating directory: {outputFolder}".Yellow());
                    Directory.CreateDirectory(outputFolder);
                }

                foreach (var inputFile in inputFiles)
                {
                    PrintLn("Source:".White());
                    PrintLn(inputFile.Gray());

                    // throws InvalidOperationException on C# parsing errors, which is caught by Program.Main
                    var markdown = CodeSummariser.Summarise(inputFile);
                    var outputFile = DetermineOutputPath(inputFile, outputFolder);

                    PrintLn("Output:".White());
                    PrintLn(outputFile.DarkGreen());

                    File.WriteAllText(outputFile, markdown);
                    PrintLn("");
                }
            }

            private static List<string> ResolveInputFiles(SummaryOptions summaryOptions)
            {
                var inputPaths = summaryOptions.FilesOrFolders.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (!inputPaths.Any()) {
                    PrintLn("No input files were given.".Red());
                    ReturnCode = 1;
                    return new List<string>();
                }

                var folders = inputPaths.Where(Directory.Exists).ToArray();
                var files = inputPaths.Where(path => !Directory.Exists(path)).ToArray();

                if (folders.Length > 1) {
                    PrintLn("Only a single folder may be given to the 'sum' verb.".Red());
                    ReturnCode = 1;
                    return new List<string>();
                }

                if (folders.Any() && files.Any()) {
                    PrintLn("The 'sum' verb accepts either file path(s) or a single folder; mixing the two is not supported.".Red());
                    ReturnCode = 1;
                    return new List<string>();
                }

                if (folders.Any()) {
                    var found = Directory.GetFiles(folders[0], "*.xsd-g.cs", SearchOption.TopDirectoryOnly)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (!found.Any()) {
                        PrintLn($"No .xsd-g.cs files were found inside: {folders[0]}".Red());
                        ReturnCode = 1;
                    }

                    return found;
                }

                var missingFiles = files.Where(f => !File.Exists(f)).ToArray();
                if (missingFiles.Any()) {
                    foreach (var missingFile in missingFiles) PrintLn($"File not found: {missingFile}".Red());
                    ReturnCode = 1;
                    return new List<string>();
                }

                var nonCodeFiles = files.Where(f => !f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (nonCodeFiles.Any()) {
                    foreach (var nonCodeFile in nonCodeFiles) PrintLn($"Not a C# source file: {nonCodeFile}".Red());
                    ReturnCode = 1;
                    return new List<string>();
                }

                return files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            }

            private static string DetermineOutputPath(string inputFile, string outputFolder)
            {
                var outputFilename = Path.GetFileName(inputFile);
                if (outputFilename.EndsWith(".xsd-g.cs", StringComparison.OrdinalIgnoreCase)) {
                    // replace the whole '.xsd-g.cs' suffix so that 'wss.xsd-g.cs' becomes 'wss.md'
                    outputFilename = outputFilename.Substring(0, outputFilename.Length - ".xsd-g.cs".Length) + ".md";
                } else {
                    outputFilename = Path.ChangeExtension(outputFilename, ".md");
                }

                return outputFolder.IsNotEmpty()
                    ? Path.Combine(outputFolder, outputFilename)
                    : Path.Combine(Path.GetDirectoryName(inputFile) ?? string.Empty, outputFilename);
            }
        }
    }
}
