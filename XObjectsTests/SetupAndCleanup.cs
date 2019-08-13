using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        [OneTimeTearDown]
        public static void Cleanup()
        {
            
        }
    }
}