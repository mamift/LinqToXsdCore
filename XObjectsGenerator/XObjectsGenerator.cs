//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml;
using System.Xml.Schema;
using System.CodeDom;
using System.CodeDom.Compiler;
using Xml.Schema.Linq;
using Xml.Schema.Linq.CodeGen;
using System.IO;
using System.Globalization;
using Microsoft.CSharp;
using System.Reflection;

namespace XObjectsGenerator
{
    public class Program 
    {
        public static Assembly ThisAssembly;

        public static int Main(string[] args) 
        {
            ThisAssembly  = Assembly.GetExecutingAssembly();
            var set = new XmlSchemaSet();
            set.XmlResolver = new XmlUrlResolver();
            var veh = new ValidationEventHandler(ValidationCallback);
            set.ValidationEventHandler += veh;
            
            string csFileName = string.Empty;
            string configFileName = null;
            string assemblyName = string.Empty;
            bool fSourceNameProvided = false;
            bool xmlSerializable = false;
            bool nameMangler2 = false;

            if (args.Length == 0) 
            {
                PrintHelp();
                return 0;
            }
            for (int i = 0; i < args.Length; i++) 
            {
                string arg = args[i];
                string value = string.Empty;
                bool argument = false;
                
                if (arg.StartsWith("/") || arg.StartsWith("-")) 
                {
                    argument = true;
                    int colonPos = arg.IndexOf(":");
                    if (colonPos != -1) 
                    {
                        value = arg.Substring(colonPos + 1);
                        arg = arg.Substring(0, colonPos);
                    }
                }
                arg = arg.ToLower(CultureInfo.InvariantCulture);
                if (!argument) 
                {
                    try
                    {
                        set.Add(null, CreateReader(arg));
                    }
                    catch(Exception e)
                    {
                        PrintErrorMessage(e.ToString());
                        return 1;
                    }
                    if (csFileName == string.Empty) 
                    {
                        csFileName = Path.ChangeExtension(arg, "cs");
                    }
                }
                else if (ArgumentMatch(arg, "?") || ArgumentMatch(arg, "help")) 
                {
                    PrintHelp();
                    return 0;
                }
                else if (ArgumentMatch(arg, "config")) 
                {
                    configFileName = value;
                }
                else if (ArgumentMatch(arg, "filename")) 
                {
                    csFileName = value;
                    fSourceNameProvided = true;
                }
                else if (ArgumentMatch(arg, "enableservicereference")) 
                {
                    xmlSerializable = true; 
                }
                else if (ArgumentMatch(arg, "lib"))
                {
                    assemblyName = value;
                }
                else if (ArgumentMatch(arg, "namemangler2"))
                {
                    nameMangler2 = true;
                }
            }
            if(assemblyName != string.Empty && !fSourceNameProvided)
            { 
                //only generate assembly
                csFileName = string.Empty;
            }
            set.Compile();
            set.ValidationEventHandler  -= veh;
            if (set.Count > 0 && set.IsCompiled) 
            {
                /*
                GenerateXObjects(
                    set, csFileName, configFileName, assemblyName, xmlSerializable, nameMangler2);
                */
                try 
                {
                    GenerateXObjects(
                        set, 
                        csFileName, 
                        configFileName, 
                        assemblyName, 
                        xmlSerializable, 
                        nameMangler2);
                }
                catch(Exception e) 
                {
                    PrintErrorMessage(e.ToString());
                    return 1;
                }
            }
            return 0;
        }

        public static void GenerateXObjects(
            XmlSchemaSet set, 
            string csFileName, 
            string configFileName, 
            string assemblyName, 
            bool xmlSerializable, 
            bool nameMangler2) 
        {
            var configSettings = new LinqToXsdSettings(nameMangler2);
            if (configFileName != null) 
            {
                configSettings.Load(configFileName);
            }
            configSettings.EnableServiceReference = xmlSerializable;
            var xsdConverter = new XsdToTypesConverter(configSettings);
            var mapping = xsdConverter.GenerateMapping(set);

            var codeGenerator = new CodeDomTypesGenerator(configSettings);
            var ccu = new CodeCompileUnit();
            foreach(var codeNs in codeGenerator.GenerateTypes(mapping))
            {
                ccu.Namespaces.Add(codeNs);
            }
            //Write to file
            var provider = new CSharpCodeProvider();
            if (csFileName != string.Empty && csFileName != null)
            {
                using (var update = 
                    new Update(csFileName, System.Text.Encoding.UTF8))
                {
                    provider.GenerateCodeFromCompileUnit(
                        ccu, update.Writer, new CodeGeneratorOptions());
                }
                PrintMessage(csFileName);
            }
            if(assemblyName != string.Empty)
            {
                var options = new CompilerParameters
                {
                    OutputAssembly = assemblyName,
                    IncludeDebugInformation = true,
                    TreatWarningsAsErrors = true,
                };
                options.TempFiles.KeepFiles = true;
                {
                    var r = options.ReferencedAssemblies;
                    r.Add("System.dll");
                    r.Add("System.Core.dll");
                    r.Add("System.Xml.dll");
                    r.Add("System.Xml.Linq.dll");
                    r.Add("Xml.Schema.Linq.dll");
                }
                var results = provider.CompileAssemblyFromDom(options, ccu);
                if (results.Errors.Count > 0)
                {
                    PrintErrorMessage("compilation error(s): ");
                    for (int i = 0; i < results.Errors.Count; i++)
                    {
                        PrintErrorMessage(results.Errors[i].ToString());
                    }
                    throw new Exception("compilation error(s)");
                }
                else
                {
                    PrintMessage(
                        "Generated Assembly: " +
                        results.CompiledAssembly.ToString());
                }
            }
        }

        private static XmlReader CreateReader(string xsdFile) 
        {
            return XmlReader.Create(
                xsdFile, 
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse });
        }
        
        private static void PrintMessage(string csFileName) 
        {
            PrintHeader();
            Console.WriteLine("Generated " + csFileName + "...");
        }

        private static void PrintErrorMessage(String e)
        {
            Console.Error.WriteLine("LinqToXsd: error TX0001: {0}", e);
        }

        private static void PrintErrorMessage(ValidationEventArgs args)
        {
            Console.Error.WriteLine(
                "{0}({1},{2}): {3} TX0001: {4}", 
                args.Exception.SourceUri.
                    Replace("file:///", "").Replace('/', '\\'),
                args.Exception.LineNumber,
                args.Exception.LinePosition, 
                args.Severity == XmlSeverityType.Warning ? "warning" : "error",
                args.Message);
        }

        private static void PrintHeader() 
        {
            Console.WriteLine(
                String.Format(
                    CultureInfo.CurrentCulture, 
                    "[Microsoft (R) .NET Framework, Version {0}]", 
                    ThisAssembly.ImageRuntimeVersion));
        }

        private static void PrintHelp() 
        {
            PrintHeader();
            string name = ThisAssembly.GetName().Name;
            Console.WriteLine();
            Console.WriteLine(
                name + 
                " - " + 
                "Utility to generate typed wrapper classes from a XML Schema");
            Console.WriteLine(
                "Usage: " + 
                name + 
                " <schemaFile> [one or more schema files] [/fileName:<csFileName>.cs] [/lib:<assemblyName>] [/config:<configFileName>.xml] [/enableServiceReference] [/nameMangler2]");
        }

        private static void ValidationCallback(
            object sender, ValidationEventArgs args) 
        {
            PrintErrorMessage(args);
        }

        private static bool ArgumentMatch(string arg, string toMatch) 
        {
            switch (arg[0])
            {
                case '/':
                case '-':
                    return arg.Substring(1) == toMatch;
                default:
                    return false;
            }
        }
    }
}
