using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Xml.Schema.Linq;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    public static partial class Program
    {
        internal static int ReturnCode { get; private set; }

        internal static IProgress<string> ProgressReporter { get; } = new Progress<string>(Console.WriteLine);

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
            foreach (var error in errors)
            {
                if (error is SetValueExceptionError setValueException && 
                    setValueException.Exception is IncompatibleArgumentException iae)
                {
                    ReturnCode = 1;
                    return;
                }
            }
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
            var settings = generateOptions.GetConfigInstance(ProgressReporter) ?? XObjectsCoreGenerator.LoadLinqToXsdSettings();
            if (generateOptions.GetConfigInstance() != null)
                Console.WriteLine("Configuration file(s) loaded...");

            settings.EnableServiceReference = generateOptions.EnableServiceReference;

            var textWriters = generateOptions.AutoConfig
                ? XObjectsCoreGenerator.Generate(generateOptions.SchemaFiles)
                : XObjectsCoreGenerator.Generate(generateOptions.SchemaFiles, settings);

            if (generateOptions.Output.IsEmpty())
            {
                if (generateOptions.FoldersWereGiven) {
                    Console.WriteLine("No output directory given: defaulting to same directory as XSD file(s).");
                    generateOptions.Output = "-1";
                }
                else {
                    generateOptions.Output = Environment.CurrentDirectory;
                    Console.WriteLine($"No output directory given: defaulting to current working directory: {Environment.CurrentDirectory}.");
                }
            }

            var hasCsExt = Path.GetExtension(generateOptions.Output).EndsWith(".cs");
            if (hasCsExt) // merge the output into a single file
                GenerateCodeDispatcher.HandleWriteOutputToSingleFile(generateOptions.Output, textWriters);
            else
            {
                GenerateCodeDispatcher.HandleWriteOutputToMultipleFiles(generateOptions.Output, textWriters);
            }
        }
    }
}