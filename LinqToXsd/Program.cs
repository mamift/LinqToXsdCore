using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Xml.Schema.Linq;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    public static partial class Program
    {
        internal static int ReturnCode { get; private set; }

        /// <summary>
        /// CLI will parse arguments here and then dispatch to the right method.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(string[] args)
        {
            var parserResult =
                Parser.Default.ParseArguments<CommandLineOptions, ConfigurationOptions, GenerateOptions>(args);

            parserResult.WithParsed<GenerateOptions>(HandleGenerateCode);
            parserResult.WithParsed<ConfigurationOptions>(HandleConfigurationOptions);

            parserResult.WithNotParsed(ErrorHandler);

            return ReturnCode;
        }

        private static void ErrorHandler(IEnumerable<Error> errors)
        {
            ReturnCode = 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configOpts"></param>
        internal static void HandleConfigurationOptions(ConfigurationOptions configOpts)
        {
            if (!configOpts.Example) return;
            if (!configOpts.SchemaFiles.Any()) 
                ConfigurationDispatcher.HandleGenerateExampleConfig();
            else 
                ConfigurationDispatcher.HandleAutoGenConfig(configOpts);
        }

        /// <summary>
        /// Generates code for one or multiple XSD input files.
        /// </summary>
        /// <param name="generateOptions"></param>
        internal static void HandleGenerateCode(GenerateOptions generateOptions)
        {
            string possibleOutputFolder = null;
            var files = generateOptions.SchemaFiles;

            var settings = generateOptions.ConfigInstance ?? XObjectsCoreGenerator.LoadLinqToXsdSettings();
            settings.EnableServiceReference = generateOptions.EnableServiceReference;

            var textWriters = XObjectsCoreGenerator.Generate(files, settings);

            // merge the output into a single file
            if (generateOptions.Output.IsNotEmpty())
            {
                if (Path.GetExtension(generateOptions.Output).IsEmpty()) // most likely a directory
                {
                    possibleOutputFolder = Path.GetFullPath(generateOptions.Output);
                }
                else
                {
                    var target = generateOptions.Output.EndsWith(".cs") ? generateOptions.Output : $"{generateOptions.Output}.cs";

                    Console.WriteLine($"Outputting to {target}");

                    using (var fileStream = File.Open(target, FileMode.Create, FileAccess.ReadWrite))
                    using (var fileWriter = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        foreach (var kvp in textWriters) fileWriter.Write(kvp.Value);
                    }

                    return;
                }
            }

            Console.WriteLine($"Outputting {textWriters.Count} files...");
            foreach (var kvp in textWriters)
            {
                var outputFilename = Path.GetFileName($"{kvp.Key}.cs");
                var outputFilePath = possibleOutputFolder.IsNotEmpty()
                    ? Path.Combine(possibleOutputFolder, outputFilename)
                    : Path.GetFullPath(outputFilename);

                var fullPathOfContainingDir = Path.GetDirectoryName(outputFilePath);

                if (!Directory.Exists(fullPathOfContainingDir))
                {
                    Console.WriteLine($"Creating directory: {fullPathOfContainingDir}");
                    Directory.CreateDirectory(fullPathOfContainingDir);
                }

                Console.WriteLine($"Outputting to {outputFilePath}");

                using (var outputFileStream = File.Open(outputFilePath, FileMode.Create, FileAccess.ReadWrite))
                using (var fileWriter = new StreamWriter(outputFileStream))
                {
                    fileWriter.Write(kvp.Value);
                }
            }
        }
    }
}