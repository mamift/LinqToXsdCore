using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alba.CsConsoleFormat.Fluent;
using CommandLine;
using Xml.Schema.Linq;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    public static partial class Program
    {
        internal static int ReturnCode { get; private set; }

        internal static IProgress<string> ProgressReporter { get; } = new Progress<string>(s => 
        {
            Console.WriteLine(s);
        });

        /// <summary>
        /// CLI will parse arguments here and then dispatch to the right method.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(string[] args)
        {
#if !DEBUG
            try {
                ParseCliArgsAndDispatch(args);
            }
            catch (Exception e) {
                Colors.WriteLine(e.ToString().DarkRed());
            }
#else
            ParseCliArgsAndDispatch(args);
#endif
            return ReturnCode;
        }

        /// <summary>
        /// Parse CLI args and dispatch to the right method.
        /// </summary>
        /// <param name="args"></param>
        private static void ParseCliArgsAndDispatch(string[] args)
        {
            var parserResult =
                Parser.Default.ParseArguments<CommandLineOptions, ConfigurationOptions, GenerateOptions>(args);

            parserResult.WithParsed<GenerateOptions>(HandleGenerateCode);
            parserResult.WithParsed<ConfigurationOptions>(HandleConfigurationOptions);

            parserResult.WithNotParsed(ErrorHandler);
        }

        /// <summary>
        /// Prints out CLI argument parsing errors.
        /// </summary>
        /// <param name="errors"></param>
        private static void ErrorHandler(IEnumerable<Error> errors)
        {
            foreach (var error in errors) {
                if (error is SetValueExceptionError setValueException && 
                    setValueException.Exception is IncompatibleArgumentException iae) {
                    Colors.WriteLine("Errors occurred: ".Red());
                    Colors.WriteLine(iae.Message.Yellow());
                    if (iae.InnerException != null)
                        Colors.WriteLine(iae.InnerException.ToString().Red());

                    ReturnCode = 1;
                    return;
                }

                Colors.WriteLine($"{error}".Red());
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
                ConfigurationDispatcher.HandleGenerateExampleConfig(configOpts);
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
                Colors.WriteLine("Configuration file(s) loaded...".Gray());

            settings.EnableServiceReference = generateOptions.EnableServiceReference;

            var textWriters = generateOptions.AutoConfig
                ? XObjectsCoreGenerator.Generate(generateOptions.SchemaFiles)
                : XObjectsCoreGenerator.Generate(generateOptions.SchemaFiles, settings);

            if (generateOptions.Output.IsEmpty()) {
                if (generateOptions.FoldersWereGiven) {
                    Colors.WriteLine("No output directory given: defaulting to same directory as XSD file(s).".Gray());
                    generateOptions.Output = "-1";
                }
                else {
                    generateOptions.Output = Environment.CurrentDirectory;
                    Colors.WriteLine("No output directory given: defaulting to current working directory:".Gray());
                    Colors.WriteLine($"{Environment.CurrentDirectory}.".Yellow());
                }
            }

            var hasCsExt = Path.GetExtension(generateOptions.Output).EndsWith(".cs");
            // merge the output into a single file
            if (hasCsExt)
                GenerateCodeDispatcher.HandleWriteOutputToSingleFile(generateOptions.Output, textWriters);
            else
                GenerateCodeDispatcher.HandleWriteOutputToMultipleFiles(generateOptions.Output, textWriters);
        }
    }
}