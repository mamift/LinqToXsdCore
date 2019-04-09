using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using Xml.Schema.Linq.Extensions;

namespace LinqToXsd
{
    public static partial class Program
    {
        internal static int ReturnCode { get; private set; }

        public static int Main(string[] args)
        {
            var cliParser = new Parser(settings => {
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
            if (generateOptions.Assembly.IsNotEmpty())
                GenerateHandler.GenerateAssemblies(generateOptions);
            else
                GenerateHandler.GenerateCode(generateOptions);
        }
    }
}