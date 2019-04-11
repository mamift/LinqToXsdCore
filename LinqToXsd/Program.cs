using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Xml.Schema.Linq;

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
            var files = generateOptions.SchemaFiles;

            var settings = generateOptions.ConfigInstance ?? XObjectsCoreGenerator.LoadLinqToXsdSettings();

            settings.EnableServiceReference = generateOptions.EnableServiceReference;

            foreach (var kvp in XObjectsCoreGenerator.Generate(files, settings))
            {
                var outputFile = Path.GetFullPath($"{kvp.Key}.cs");

                Console.WriteLine($"Outputting to {outputFile}");

                using (var outputFileStream = File.Open(outputFile, FileMode.Create, FileAccess.ReadWrite))
                using (var fileWriter = new StreamWriter(outputFileStream))
                {
                    fileWriter.Write(kvp.Value);
                }
            }
        }
    }
}