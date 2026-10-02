using System;
using System.IO;
using LinqToXsd;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class CodeSummariserTests
    {
        private const string SampleSource = @"
namespace Sample.Namespace {
    public partial class SampleType : global::Sample.BaseType, ISample {
        private int hiddenField;
        private SampleType() {
        }
        public SampleType(int count) : base(count) {
        }
        static SampleType() {
        }
        public static SampleType Load(string xmlFile) {
            var x = new SampleType(0);
            return x;
        }
        internal int InternalMethod() {
            return hiddenField;
        }
        private void HiddenMethod() {
        }
        protected internal static void ProtectedInternalMethod(ref int value, params string[] names) {
        }
        public virtual string Name {
            get {
                return ""name"";
            }
            private set {
            }
        }
        public string Name2 { get; set; }
        private string HiddenProperty { get; set; }
        public static int StaticProperty {
            get {
                return 0;
            }
        }
        public enum NestedEnum {
            One = 1,
            Two
        }
    }
    public enum TopLevelEnum : long {
        A,
        B = 2
    }
}
";

        [Test]
        public void WhenSummarisingSourceThenClassMembersAreShortenedToSignatures()
        {
            var summary = CodeSummariser.SummariseSource(SampleSource, "sample.xsd-g.cs");

            // class declaration includes base types and interfaces
            StringAssert.Contains("public partial class SampleType : global::Sample.BaseType, ISample", summary);
            // non-private constructors are included, along with the static constructor
            StringAssert.Contains("public SampleType(int count) : base(count)", summary);
            StringAssert.Contains("static SampleType()", summary);
            // methods are shortened to signatures only
            StringAssert.Contains("public static SampleType Load(string xmlFile)", summary);
            StringAssert.Contains("internal int InternalMethod()", summary);
            StringAssert.Contains("protected internal static void ProtectedInternalMethod(ref int value, params string[] names)", summary);
            // properties keep their types and accessor lists, but no bodies
            StringAssert.Contains("public virtual string Name { get; private set; }", summary);
            StringAssert.Contains("public string Name2 { get; set; }", summary);
            StringAssert.Contains("public static int StaticProperty { get; }", summary);
            // enums are summarised with their values
            StringAssert.Contains("Sample.Namespace.SampleType.NestedEnum", summary);
            StringAssert.Contains("One = 1", summary);
            StringAssert.Contains("public enum TopLevelEnum : long", summary);
            StringAssert.Contains("B = 2", summary);
            // private members are omitted
            StringAssert.DoesNotContain("hiddenField", summary);
            StringAssert.DoesNotContain("HiddenMethod", summary);
            StringAssert.DoesNotContain("HiddenProperty", summary);
            StringAssert.DoesNotContain("private SampleType()", summary);
            // method bodies are omitted
            StringAssert.DoesNotContain("new SampleType(0)", summary);
        }

        [Test]
        public void WhenSourceContainsParsingErrorThenThrows()
        {
            const string invalidSource = "public class Broken { public void Method( { } }";

            var exception = Assert.Throws<InvalidOperationException>(
                () => CodeSummariser.SummariseSource(invalidSource, "broken.xsd-g.cs"));

            StringAssert.Contains("broken.xsd-g.cs", exception.Message);
        }

        [Test]
        public void WhenSourceUsesCSharp8SyntaxThenSucceeds()
        {
            const string csharp8Source = @"
#nullable enable annotations
namespace Sample.Namespace {
    public class NullableType {
        public virtual System.String? Name { get; set; }
    }
}
";

            var summary = CodeSummariser.SummariseSource(csharp8Source, "nullable.xsd-g.cs");

            StringAssert.Contains("public virtual System.String? Name { get; set; }", summary);
        }

        [Test]
        public void WhenSumVerbGivenSingleFileThenWritesMarkdownFile()
        {
            var tempFolder = CreateTempFolder();
            try {
                var sourceFile = Path.Combine(tempFolder, "sample.xsd-g.cs");
                File.WriteAllText(sourceFile, SampleSource);

                var result = LinqToXsd.Program.Main(new[] { "sum", sourceFile });

                Assert.AreEqual(0, result);
                var markdownFile = Path.Combine(tempFolder, "sample.md");
                Assert.IsTrue(File.Exists(markdownFile), "Expected 'sample.md' to be written next to 'sample.xsd-g.cs'.");
                StringAssert.Contains("SampleType", File.ReadAllText(markdownFile));
            } finally {
                Directory.Delete(tempFolder, true);
            }
        }

        [Test]
        public void WhenSumVerbGivenFolderThenSummarisesEveryGeneratedFile()
        {
            var tempFolder = CreateTempFolder();
            try {
                File.WriteAllText(Path.Combine(tempFolder, "first.xsd-g.cs"), SampleSource);
                File.WriteAllText(Path.Combine(tempFolder, "second.xsd-g.cs"), SampleSource);
                File.WriteAllText(Path.Combine(tempFolder, "ignored.txt"), "not code");

                var result = LinqToXsd.Program.Main(new[] { "sum", tempFolder });

                Assert.AreEqual(0, result);
                Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "first.md")));
                Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "second.md")));
                Assert.IsFalse(File.Exists(Path.Combine(tempFolder, "ignored.md")));
            } finally {
                Directory.Delete(tempFolder, true);
            }
        }

        [Test]
        public void WhenSumVerbGivenFileWithParsingErrorThenReturnsNonZero()
        {
            var tempFolder = CreateTempFolder();
            try {
                var brokenFile = Path.Combine(tempFolder, "broken.xsd-g.cs");
                File.WriteAllText(brokenFile, "public class Broken { public void Method( { } }");

                var result = 1;
                try {
                    result = LinqToXsd.Program.Main(new[] { "sum", brokenFile });
                } catch (InvalidOperationException) {
                    // DEBUG builds of Program.Main do not catch exceptions (see the #if !DEBUG block);
                    // in that configuration the exception propagates instead of being converted to a return code.
                    result = 1;
                }

                Assert.AreEqual(1, result);
                Assert.IsFalse(File.Exists(Path.Combine(tempFolder, "broken.md")));
            } finally {
                Directory.Delete(tempFolder, true);
            }
        }

        [Test]
        public void WhenSumVerbGivenMissingFileThenReturnsNonZero()
        {
            var missingFile = Path.Combine(Path.GetTempPath(), "linqtoxsd-sum-" + Guid.NewGuid().ToString("N"), "does-not-exist.xsd-g.cs");

            var result = LinqToXsd.Program.Main(new[] { "sum", missingFile });

            Assert.AreEqual(1, result);
        }

        private static string CreateTempFolder()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "linqtoxsd-sum-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }
    }
}
