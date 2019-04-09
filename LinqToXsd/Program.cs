using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Xml.Schema.Linq;

namespace LinqToXsd
{
    public static class Program
    {
        internal static int ReturnCode { get; private set; }

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

        internal static void HandleConfigurationOptions(ConfigurationOptions configOpts)
        {
            Console.WriteLine($"");
        }

        internal static void HandleGenerateCode(GenerateOptions generateOptions)
        {
            var files = generateOptions.SchemaFiles;

            var settings = generateOptions.ConfigInstance ?? XObjectsCoreGenerator.LoadLinqToXsdSettings();

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
    }
}