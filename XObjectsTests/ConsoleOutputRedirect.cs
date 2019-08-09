using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    [SetUpFixture]
    public class ConsoleOutputRedirect
    {
        private static readonly StringWriter RedirectedOutput = new StringWriter(new StringBuilder());

        [OneTimeSetUp]
        public static void RedirectOutput()
        {
            Console.SetOut(RedirectedOutput);
        }
    }
}