using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var parserResult = Parser.Default.ParseArguments<CommandLineOptions, ConfigurationOptions, GenerateOptions>(args);

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
        /// Dispatches to the right handler method for creating configuration files.
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
        /// Dispatches to the right handler method for writing generated code to output file(s).
        /// </summary>
        /// <param name="generateOptions"></param>
        internal static void HandleGenerateCode(GenerateOptions generateOptions)
        {
            var files = generateOptions.SchemaFiles;

            var settings = generateOptions.ConfigInstance ?? XObjectsCoreGenerator.LoadLinqToXsdSettings();
            if (generateOptions.ConfigInstance != null)
                Console.WriteLine($"Reading configuration file: {generateOptions.Config}");

            settings.EnableServiceReference = generateOptions.EnableServiceReference;

            var textWriters = XObjectsCoreGenerator.Generate(files, settings);

            if (generateOptions.Output.IsEmpty())
            {
                generateOptions.Output = Environment.CurrentDirectory;
                Console.WriteLine("No output directory given: defaulting to current working directory.");
            }

            // most likely a directory, output to multiple files
            if (!Path.GetExtension(generateOptions.Output).EndsWith(".cs"))
            {
                var possibleOutputFolder = Path.GetFullPath(generateOptions.Output);
                GenerateCodeDispatcher.HandleWriteOutputToMultipleFiles(possibleOutputFolder, textWriters);
            }
            else // merge the output into a single file
                GenerateCodeDispatcher.HandleWriteOutputToSingleFile(generateOptions.Output, textWriters);
        }
    }
}