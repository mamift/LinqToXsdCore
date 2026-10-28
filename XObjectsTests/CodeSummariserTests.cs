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
        public static global::Xml.Schema.Linq.SimpleTypeValidator TypeDefinition = new global::Xml.Schema.Linq.AtomicSimpleTypeValidator();
        protected internal static readonly System.Xml.Linq.XName idXName = System.Xml.Linq.XName.Get(""id"");
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

            // top-level file summary is not a markdown heading
            StringAssert.StartsWith("Summary of `sample.xsd-g.cs`", summary);
            StringAssert.DoesNotContain("# Summary of", summary);

            // type headings use single #
            StringAssert.Contains("# `Sample.Namespace.SampleType` — class", summary);
            StringAssert.Contains("# `Sample.Namespace.SampleType.NestedEnum` — enum", summary);
            StringAssert.Contains("# `Sample.Namespace.TopLevelEnum` — enum", summary);

            // class declaration includes base types and interfaces
            StringAssert.Contains("public partial class SampleType : global::Sample.BaseType, ISample", summary);
            // non-private constructors are included under Constructors, along with the static constructor
            StringAssert.Contains("### Constructors", summary);
            StringAssert.Contains("public SampleType(int count) : base(count)", summary);
            StringAssert.Contains("static SampleType()", summary);
            // methods are grouped by modifiers into section headers and shortened to signatures
            StringAssert.Contains("### Public, static, methods", summary);
            StringAssert.Contains("- `SampleType Load(string xmlFile)`", summary);
            StringAssert.Contains("### Internal methods", summary);
            StringAssert.Contains("- `int InternalMethod()`", summary);
            StringAssert.Contains("### Protected, internal, static, methods", summary);
            StringAssert.Contains("- `void ProtectedInternalMethod(ref int value, params string[] names)`", summary);
            // properties are grouped by modifiers into section headers
            StringAssert.Contains("### Public, virtual, properties", summary);
            StringAssert.Contains("- `string Name { get; private set; }`", summary);
            StringAssert.Contains("### Public properties", summary);
            StringAssert.Contains("- `string Name2 { get; set; }`", summary);
            StringAssert.Contains("### Public, static, properties", summary);
            StringAssert.Contains("- `int StaticProperty { get; }`", summary);
            // enums are summarised with their values
            StringAssert.Contains("### Values", summary);
            StringAssert.Contains("One = 1", summary);
            StringAssert.Contains("public enum TopLevelEnum : long", summary);
            StringAssert.Contains("B = 2", summary);
            // type validator fields are grouped under type validators with hoisted modifiers
            StringAssert.Contains("### Public, static, type validators", summary);
            StringAssert.Contains("- `global::Xml.Schema.Linq.SimpleTypeValidator TypeDefinition`", summary);
            StringAssert.DoesNotContain("AtomicSimpleTypeValidator()", summary);
            // other fields (XName constants) are not part of the summary
            StringAssert.DoesNotContain("idXName", summary);
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

            StringAssert.Contains("### Public, virtual, properties", summary);
            StringAssert.Contains("- `System.String? Name { get; set; }`", summary);
        }

        [Test]
        public void WhenClassHasOverrideMethodAndVirtualPropertiesThenMatchesExpectedFormat()
        {
            const string source = @"
namespace Omg.Xtce {
    public partial class SpaceSystemType : global::Omg.Xtce.NameDescriptionType, IXMetaData {
        public SpaceSystemType() {}
        static SpaceSystemType() {}
        public override XTypedElement Clone() => null;
        public virtual HeaderType Header { get; set; }
        public virtual TelemetryMetaDataType TelemetryMetaData { get; set; }
        public virtual CommandMetaDataType CommandMetaData { get; set; }
        public virtual ServiceSetType ServiceSet { get; set; }
        public virtual IList<SpaceSystem> SpaceSystem { get; set; }
        public virtual Omg.Xtce.SystemTypeType systemType { get; set; }
        public virtual string assetType { get; set; }
        public virtual string operationalStatus { get; set; }
        public virtual System.Uri @base { get; set; }
    }
}
";

            var summary = CodeSummariser.SummariseSource(source, "SpaceSystem.xsd-g.cs");

            var expected = @"Summary of `SpaceSystem.xsd-g.cs`

Generated by `linqtoxsd sum`.

# `Omg.Xtce.SpaceSystemType` — class

- **Declaration:** `public partial class SpaceSystemType : global::Omg.Xtce.NameDescriptionType, IXMetaData`

### Constructors

- `public SpaceSystemType()`
- `static SpaceSystemType()`

### Public methods

- `override XTypedElement Clone()`

### Public, virtual, properties

- `HeaderType Header { get; set; }`
- `TelemetryMetaDataType TelemetryMetaData { get; set; }`
- `CommandMetaDataType CommandMetaData { get; set; }`
- `ServiceSetType ServiceSet { get; set; }`
- `IList<SpaceSystem> SpaceSystem { get; set; }`
- `Omg.Xtce.SystemTypeType systemType { get; set; }`
- `string assetType { get; set; }`
- `string operationalStatus { get; set; }`
- `System.Uri @base { get; set; }`

";

            Assert.AreEqual(expected.Replace("\r\n", "\n"), summary.Replace("\r\n", "\n"));
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
