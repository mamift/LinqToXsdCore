using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Schemas.Developer.MsBuild;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    [SetUpFixture]
    public class SetupAndCleanup
    {
        public static readonly StringWriter RedirectedOutput = new StringWriter(new StringBuilder());

        public static List<string> Guids { get; } = new List<string>();

        [OneTimeSetUp]
        public static void Setup()
        {
            Console.SetOut(RedirectedOutput);
        }

        /// <summary>
        /// Cleans up any schema folders from previous runs that may not have been cleaned up due to errors.
        /// </summary>
        [OneTimeTearDown]
        public static void Cleanup()
        {
            foreach (var guid in Guids) {
                var dir = new DirectoryInfo("schemas_" + guid);
                if (dir.Exists) {
                    dir.Delete(true);
                }
            }
            
            const string guidRegex = @"(?im)[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?";
            var currentDir = new DirectoryInfo(@".").EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
            var dirStrings = currentDir
                .Where(d => d.Name.StartsWith("schemas_") && Regex.Match(d.Name, guidRegex).Success);
            foreach (var dir in dirStrings) {
                dir.Refresh();
                if (dir.Exists) {
                    dir.Delete(true);
                }
            }

            var mergedConfig = @".\merged.config";
            if (File.Exists(mergedConfig)) {
                File.Delete(mergedConfig);
            }
        }
    }
}