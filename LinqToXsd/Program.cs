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
    public static class Program
    {
        internal static int ReturnCode { get; private set; }

        public static int Main(string[] args)
        {
            var cliParser = new Parser(settings =>
            {
                settings.CaseSensitive = false;
                settings.AutoHelp = false;
                settings.AutoVersion = false;
                settings.MaximumDisplayWidth = Console.WindowWidth - 1;
                settings.HelpWriter = Console.Out;
            });

            var parserResult =
                cliParser.ParseArguments<CommandLineOptions, ConfigurationOptions, GenerateOptions>(args);

            parserResult.WithParsed<GenerateOptions>(DispatchForGenerateOptions);
            parserResult.WithParsed<ConfigurationOptions>(DispatchForConfigurationOptions);

            parserResult.WithNotParsed(ErrorHandler);

            return ReturnCode;
        }

        private static void ErrorHandler(IEnumerable<Error> errors)
        {
            ReturnCode = 1;
        }

        private static void DispatchForConfigurationOptions(ConfigurationOptions configOpts)
        {
            Console.WriteLine($"");
        }

        private static void DispatchForGenerateOptions(GenerateOptions generateOptions)
        {
            void T()
            {
                var schemaFileReaders = generateOptions.SchemaReaders;
                var tConfig = new LinqToXsdSettings(true);
                var tOutputFile = generateOptions.Output.IsEmpty() ? generateOptions.SchemaFiles.First() : generateOptions.Output;

                var writers = schemaFileReaders.Select(f => XObjectsCoreGenerator.Generate(f, tConfig));

                var stringBuilder = new StringBuilder();

                foreach (var writer in writers)
                    stringBuilder.Append(writer);

                File.WriteAllTextAsync(tOutputFile, stringBuilder.ToString());
            }

            var files = generateOptions.SchemaFiles;

            var multipleFilesWriter = XObjectsCoreGenerator.Generate(files, generateOptions.Config);
            
            using (var outputFileStream = File.Open(generateOptions.Output, FileMode.Create))
            using (var fileWriter = new StreamWriter(outputFileStream))
            {
                fileWriter.Write(multipleFilesWriter);
            }
        }
    }
}