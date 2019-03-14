using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Xml.Schema.Linq;

namespace LinqToXsd
{
    public class Program
    {
        internal static int ReturnCode { get; private set; }

        public static int Main(string[] args)
        {
            var parserResult =
                Parser.Default.ParseArguments<CommandLineOptions, ConfigurationOptions, GenerateOptions>(args);

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
            var files = generateOptions.SchemaReaders;
            var config = new LinqToXsdSettings(true);
            var file = generateOptions.Output;

            var writers = files.Select(f => XObjectsCoreGenerator.Generate(f, config));

            var stringBuilder = new StringBuilder();

            foreach (var writer in writers)
                stringBuilder.Append(writer);

            File.WriteAllTextAsync(generateOptions.Output, stringBuilder.ToString());
        }
    }
}