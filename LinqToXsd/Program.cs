#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Alba.CsConsoleFormat.Fluent;
using CommandLine;
using Xml.Schema.Linq;
using Xml.Schema.Linq.CodeGen;
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

        /// <summary>
        /// This is currently unused, but it is intended to be used for logging warnings to the console without propagating an exception.
        /// </summary>
        public static IWarnableObserver<string> ProgramObserver { get; } = new LinqToXsdProgramObserver();

        public static bool IsConsolePresent
        {
            get
            {
                try {
                    var thereIsBuffer = Console.BufferHeight > 0 | Console.BufferWidth > 0;
                    return (Console.WindowHeight > 0) && thereIsBuffer && Environment.UserInteractive;
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
            var consoleIsPresent = IsConsolePresent && Environment.UserInteractive;
            if (consoleIsPresent) {
                try {
                    Colors.WriteLine(element);
                }
                catch (IOException ioe) {
                    if (ioe.Message == "The handle is invalid") return;
                }
            }
            else
                Debug.WriteLine(element);
        }

        /// <summary>
        /// CLI will parse arguments here and then dispatch to the right method.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(string[] args)
        {
            ReturnCode = 0; // reset in case Main is invoked more than once in the same process (as in unit testing)
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown";
            PrintLn(("LinqToXsdCore v" + version).DarkGray());
            PrintLn(($"Copyright (C) 2008-2011 Microsoft Corp, (C) 2019-{DateTime.Now.Year} github.com/mamift et al" + Environment.NewLine).White());
#if !DEBUG
            try {
                ParseCliArgsAndDispatch(args);
            }
            catch (Exception e) {
                PrintLn(e.ToString().DarkRed());
                ReturnCode = 1;
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
            using (var parser = Parser.Default)
            {
                var parserResult = parser.ParseArguments<CommandLineOptions, ConfigurationOptions, GenerateOptions, SummaryOptions>(args);

                var generateHandlerAction = GenerateDisposalWrapper<GenerateOptions>(HandleGenerateCode);
                parserResult.WithParsed<GenerateOptions>(generateHandlerAction);

                var configHandlerAction = GenerateDisposalWrapper<ConfigurationOptions>(HandleConfigurationOptions);
                parserResult.WithParsed<ConfigurationOptions>(configHandlerAction);

                var summaryHandlerAction = GenerateDisposalWrapper<SummaryOptions>(SummaryDispatcher.HandleSummary);
                parserResult.WithParsed<SummaryOptions>(summaryHandlerAction);

                //parserResult.WithNotParsed(ErrorHandler);
            }
        }

        /// <summary>
        /// Because the <see cref="OptionsAbstract"/> implements <see cref="IDisposable"/> for those <see cref="OptionsAbstract.SchemaReaders"/>,
        /// this helper function will ensure that the <see cref="TOptions"/> is disposed of automatically.
        /// <para>Necessary when <see cref="Main"/> is invoked from code and not from the OS (as in unit testing).</para>
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        private static Action<TOptions> GenerateDisposalWrapper<TOptions>(Action<TOptions> handler)
            where TOptions: OptionsAbstract, IDisposable
        {
            return opts => {
                using (opts) {
                    handler(opts);
                }
            };
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
            LinqToXsdSettings? settingsFromFile = generateOptions.GetConfigInstance(ProgressReporter);

            if (generateOptions.AutoConfig)
            {
                ProgressReporter.Report("NOTE: As of v3.4.24 the -a flag (which searches for a config file matching the filename of an XSD) is now always automatically applied. If an .xsd.config is not found, default config values are applied.");
            }

            // A single output file can't hold code that the configuration asks to be split across files,
            // so refuse to run rather than write a file that doesn't compile.
            var splitCodeFilesBy = SplittingConfiguredFor(generateOptions, settingsFromFile);
            if (generateOptions.OutputIsFile && splitCodeFilesBy != null)
                throw new LinqToXsdException(
                    $"{nameof(GenerateOptions.Output)} refers to a single file ('{generateOptions.Output}'), but the configuration sets " +
                    $"SplitCodeFiles By=\"{splitCodeFilesBy}\", which writes the generated code to multiple files. " +
                    $"Either specify a folder for {nameof(GenerateOptions.Output)}, or remove the SplitCodeFiles element from the configuration.");

            Dictionary<string, TextWriter> textWriters;
            if (settingsFromFile is null)
            {
                textWriters = XObjectsCoreGenerator.Generate(generateOptions.SchemaFiles, ProgramObserver);
            }
            else
            {
                settingsFromFile.EnableServiceReference = generateOptions.EnableServiceReference;
                Debug.Assert(settingsFromFile != null, nameof(settingsFromFile) + " != null");
                textWriters = XObjectsCoreGenerator.Generate(generateOptions.SchemaFiles, settingsFromFile, ProgramObserver);
            }

            if (generateOptions.Output.IsEmpty())
            {
                PrintLn("No output directory given: defaulting to same directory as XSD file(s).".Gray());
                generateOptions.Output = "-1";
            }

            Debug.Assert(generateOptions.Output != null);
            if (splitCodeFilesBy != null && !generateOptions.OutputIsFile)
                WarnAboutStaleCombinedOutputFile(generateOptions, splitCodeFilesBy.Value);

            // merge the output into a single file
            if (generateOptions.OutputIsFile)
                GenerateCodeDispatcher.HandleWriteOutputToSingleFile(generateOptions, textWriters);
            else
                GenerateCodeDispatcher.HandleWriteOutputToMultipleFiles(generateOptions, textWriters);
        }

        /// <summary>
        /// Returns the split mode that applies to the given <paramref name="generateOptions"/>, or <c>null</c> when either
        /// no configuration sets <c>SplitCodeFiles</c>, or all the configurations that were read agree on one output file.
        /// </summary>
        private static SplitCodeFiles.ByEnum? SplittingConfiguredFor(GenerateOptions generateOptions, LinqToXsdSettings? settingsFromFile)
        {
            if (settingsFromFile != null) return settingsFromFile.SplitCodeFilesBy;

            // Without a -c argument the generator reads a 'schema.xsd.config' beside each schema, so look there for settings.
            foreach (var settings in generateOptions.LoadConfigInstancesBesideSchemas())
            {
                if (settings.SplitCodeFilesBy != null) return settings.SplitCodeFilesBy;
            }

            return null;
        }

        /// <summary>
        /// Tells the user to delete the single file that earlier runs generated, because it declares the same types that are
        /// now spread across the split output files, and would cause duplicate definition errors if it were left in place.
        /// </summary>
        private static void WarnAboutStaleCombinedOutputFile(GenerateOptions generateOptions, SplitCodeFiles.ByEnum splitCodeFilesBy)
        {
            var outputFolder = generateOptions.Output.IsEmpty() || generateOptions.Output == "-1" ? null : generateOptions.Output;

            foreach (var schemaFile in generateOptions.SchemaFiles)
            {
                var folder = outputFolder ?? Path.GetDirectoryName(schemaFile) ?? string.Empty;
                // 'schema.xsd' is written to 'schema.xsd-g.cs' when the generated code is not split.
                var combinedOutputFile = Path.Combine(folder, Path.GetFileName(schemaFile) + GenerateCodeExtension);

                if (File.Exists(combinedOutputFile))
                    PrintLn($"NOTE: Generated code is now split into multiple files (SplitCodeFiles By=\"{splitCodeFilesBy}\"). Delete the previously generated file: {combinedOutputFile}".Yellow());
            }
        }
    }
}