using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static IProgress<string> ProgressReporter { get; } = new Progress<string>(s => 
        {
            Console.WriteLine(s);
        });

        public static bool IsConsolePresent
        {
            get {
                try {
                    return (Console.Title.Length > 0 || Console.WindowHeight > 0) && Environment.UserInteractive;
                } catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Prints to either colored console output or <see cref="Debug"/>.
        /// </summary>
        /// <param name="element"></param>
        public static void PrintLn(object element)
        {
#if TEST
            Debug.WriteLine(element);
#else
            var consoleIsPresent = IsConsolePresent && Environment.UserInteractive;
            if (consoleIsPresent) Colors.WriteLine(element);
            else
                Debug.WriteLine(element);
#endif
        }

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
                PrintLn(e.ToString().DarkRed());
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
        internal static void ParseCliArgsAndDispatch(string[] args)
        {
            var parserResult =
                Parser.Default.ParseArguments<CommandLineOptions, ConfigurationOptions, GenerateOptions>(args);

#if TEST
            var generateHandlerAction = 
                InvokeWithAutomaticDisposalToCorrectHandler<GenerateOptions>(HandleGenerateCode);
            parserResult.WithParsed<GenerateOptions>(generateHandlerAction);

            var configHandlerAction =
                InvokeWithAutomaticDisposalToCorrectHandler<ConfigurationOptions>(HandleConfigurationOptions);
            parserResult.WithParsed<ConfigurationOptions>(configHandlerAction);
#else
            parserResult.WithParsed<GenerateOptions>(HandleGenerateCode);
            parserResult.WithParsed<ConfigurationOptions>(HandleConfigurationOptions);
#endif

            parserResult.WithNotParsed(ErrorHandler);
        }

#if TEST
        /// <summary>
        /// Because the <see cref="OptionsAbstract"/> implements <see cref="IDisposable"/> for those <see cref="OptionsAbstract.SchemaReaders"/>,
        /// this helper function will ensure that the <see cref="TOptions"/> is disposed of automatically.
        /// <para>Necessary when <see cref="Main"/> is invoked from code and not from the OS (as in unit testing).</para>
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        private static Action<TOptions> InvokeWithAutomaticDisposalToCorrectHandler<TOptions>(Action<TOptions> handler)
            where TOptions: OptionsAbstract, IDisposable
        {
            return delegate(TOptions opts) {
                using (opts) {
                    handler(opts);
                }
            };
        }
#endif

        /// <summary>
        /// Prints out CLI argument parsing errors.
        /// </summary>
        /// <param name="errors"></param>
        private static void ErrorHandler(IEnumerable<Error> errors)
        {
            foreach (var error in errors) {
                if (error is SetValueExceptionError setValueException && 
                    setValueException.Exception is IncompatibleArgumentException iae) {
                    PrintLn("Errors occurred: ".Red());
                    PrintLn(iae.Message.Yellow());
                    if (iae.InnerException != null)
                        PrintLn(iae.InnerException.ToString().Red());

                    ReturnCode = 1;
                    return;
                }

                PrintLn($"{error}".Red());
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
                PrintLn("Configuration file(s) loaded...".Gray());

            settings.EnableServiceReference = generateOptions.EnableServiceReference;

            var textWriters = generateOptions.AutoConfig
                ? XObjectsCoreGenerator.Generate(generateOptions.SchemaFiles)
                : XObjectsCoreGenerator.Generate(generateOptions.SchemaFiles, settings);

            if (generateOptions.Output.IsEmpty()) {
                if (generateOptions.FoldersWereGiven) {
                    PrintLn("No output directory given: defaulting to same directory as XSD file(s).".Gray());
                    generateOptions.Output = "-1";
                }
                else {
                    generateOptions.Output = Environment.CurrentDirectory;
                    PrintLn("No output directory given: defaulting to current working directory:".Gray());
                    PrintLn($"{Environment.CurrentDirectory}.".Yellow());
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