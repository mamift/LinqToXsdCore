using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class CommandLineInterfaceTests: BaseTester
    {
        private static readonly DirectoryInfo CurrentFolder = new DirectoryInfo(@".");
        
        private static string GetSchemasCopy()
        {
            var tempFolderName = "schemas_" + Guid.NewGuid().ToString("N");
            SetupAndCleanup.Guids.Add(tempFolderName);
            return tempFolderName;
        }

        private static DirectoryInfo _copyOfSchemasFolder = new DirectoryInfo(GetSchemasCopy());

        public static void CopySchemasFolder()
        {
            CurrentFolder.Refresh();
            var fileExtFilters = new []{ "dll", "exe", "json", "pdb", "log" };
            _copyOfSchemasFolder = CurrentFolder.Copy(GetSchemasCopy(), overwrite: true, copySubDirs: false, 
                fileExtensionFilters: fileExtFilters);
            _copyOfSchemasFolder.DeleteFilesInside("*.config"); // delete any config files
            _copyOfSchemasFolder.DeleteFilesInside("*.cs"); // and delete any generated code
        }

        public static void DeleteSchemasFolder()
        {
            try {
                if (_copyOfSchemasFolder.Exists) _copyOfSchemasFolder.Delete(true);
            }
            catch(IOException ioe) { // this only is important when running unit tests
                TestContext.Out.WriteLine($"Could not delete temporary folder: {_copyOfSchemasFolder.FullName}:\nError: {ioe.Message}");
            }

            _copyOfSchemasFolder.Refresh();
        }

        /// <summary>
        /// Tests "linqtoxsd config -e '(folderpath)'".
        /// </summary>
        [Test]
        public void TestGenerateConfigurationFilesFromFolder()
        {
            var configFiles = _copyOfSchemasFolder.GetFiles("*.config", SearchOption.AllDirectories);

            Assert.IsFalse(configFiles.Any());

            Assert.IsNotNull(_copyOfSchemasFolder);
            Assert.IsTrue(_copyOfSchemasFolder.Exists);
            
            var programResult = LinqToXsd.Program.Main(new[] {"config", _copyOfSchemasFolder.FullName, "-e"});
            Assert.IsTrue(programResult == 0);

            configFiles = _copyOfSchemasFolder.GetFiles("*.config", SearchOption.AllDirectories);

            Assert.IsTrue(configFiles.Any());
            Assert.IsTrue(configFiles.Length >= 9); // new config files may get added
        }

        /// <summary>
        /// Tests "linqtoxsd config -e '(folderpath)' -o '(mergedoutput.config)'".
        /// </summary>
        [Test]
        public void TestGenerateConfigurationFilesFromFolderAndMergeOutput()
        {
            var configFiles = _copyOfSchemasFolder.GetFiles("*.config", SearchOption.AllDirectories);

            Assert.IsFalse(configFiles.Any());

            Assert.IsNotNull(_copyOfSchemasFolder);
            Assert.IsTrue(_copyOfSchemasFolder.Exists);

            var mergedConfigFilename = "merged.config";
            var programResult = LinqToXsd.Program.Main(new[] {"config", _copyOfSchemasFolder.FullName, "-e", "-o", mergedConfigFilename});
            Assert.IsTrue(programResult == 0);

            configFiles = new DirectoryInfo(Environment.CurrentDirectory)
                .GetFiles("*.config", SearchOption.AllDirectories);

            Assert.IsTrue(configFiles.Any());
            Assert.IsTrue(configFiles.Length > 1);
            Assert.IsTrue(configFiles.Any(c => c.Name == mergedConfigFilename));
        }

        /// <summary>
        /// Tests "linqtoxsd gen '(file)' -a"
        /// </summary>
        [Test]
        public void TestGenerateCodeFromSingleFileWithAutoConfig()
        {
            _copyOfSchemasFolder.Refresh();
            
            var microsoftBuildXsd = "Microsoft.Build.xsd";
            var msBuildXsd = Directory.GetFiles(_copyOfSchemasFolder.FullName, microsoftBuildXsd, SearchOption.AllDirectories).Single();

            var genConfigResult = LinqToXsd.Program.Main(new[] { "config", "-e", _copyOfSchemasFolder.FullName });
            Assert.IsTrue(genConfigResult == 0);
            var generatedConfig = _copyOfSchemasFolder.GetFiles("*.xsd.config", SearchOption.AllDirectories);
            Assert.IsNotEmpty(generatedConfig);

            var genCodeResult = LinqToXsd.Program.Main(new[] {"gen", msBuildXsd, "-a"});
            Assert.IsTrue(genCodeResult == 0);
            Assert.IsNotEmpty(_copyOfSchemasFolder.GetFiles("*.xsd-g.cs", SearchOption.AllDirectories));

            var generatedCsFile = _copyOfSchemasFolder.GetFiles($"{microsoftBuildXsd}-g.cs", SearchOption.AllDirectories);

            Assert.IsTrue(generatedCsFile.Any());
            var _ = generatedCsFile.Single();
        }

        /// <summary>
        /// Tests "linqtoxsd gen '(folder)' -a"
        /// </summary>
        [Test]
        public async Task TestGenerateCodeFromSingleDirectoryWithAutoConfig()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            _copyOfSchemasFolder.Refresh();
            var microsoftBuildXsd = "Microsoft.Build.xsd";
            var msBuildXsd = _copyOfSchemasFolder.GetFiles(microsoftBuildXsd, SearchOption.AllDirectories).Single();

            var genConfigResult = LinqToXsd.Program.Main(new[] { "config", "-e", _copyOfSchemasFolder.FullName });
            Assert.IsTrue(genConfigResult == 0);
            Assert.IsNotEmpty(_copyOfSchemasFolder.GetFiles("*.xsd.config", SearchOption.AllDirectories));

            var directoryName = Path.GetDirectoryName(msBuildXsd.FullName);
            var genCodeResult = LinqToXsd.Program.Main(new[] {"gen", directoryName, "-a"});

            Assert.IsTrue(genCodeResult == 0);
            Assert.IsNotEmpty(_copyOfSchemasFolder.GetFiles("*.xsd-g.cs", SearchOption.AllDirectories));

            var generatedCsFile = _copyOfSchemasFolder.GetFiles($"{microsoftBuildXsd}-g.cs", SearchOption.AllDirectories);

            Assert.IsTrue(generatedCsFile.Any());
            var _ = generatedCsFile.Single();

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Tests that "gen '(file)' -a" removes a legacy *.xsd.cs output file before
        /// emitting the new *.xsd-g.cs file.
        /// </summary>
        [Test]
        public void TestGenerateCodeDeletesLegacyOutputFileWithAutoConfig()
        {
            _copyOfSchemasFolder.Refresh();
            var microsoftBuildXsd = "Microsoft.Build.xsd";
            var msBuildXsd = _copyOfSchemasFolder.GetFiles(microsoftBuildXsd, SearchOption.AllDirectories).Single();

            var legacyOutputFile = Path.Combine(Path.GetDirectoryName(msBuildXsd.FullName), $"{microsoftBuildXsd}.cs");
            File.WriteAllText(legacyOutputFile, "// legacy generated code");
            Assert.IsTrue(File.Exists(legacyOutputFile));

            var genConfigResult = LinqToXsd.Program.Main(new[] { "config", "-e", _copyOfSchemasFolder.FullName });
            Assert.IsTrue(genConfigResult == 0);

            var genCodeResult = LinqToXsd.Program.Main(new[] {"gen", msBuildXsd.FullName, "-a"});

            Assert.IsTrue(genCodeResult == 0);
            Assert.IsFalse(File.Exists(legacyOutputFile));
            Assert.IsTrue(File.Exists(Path.Combine(Path.GetDirectoryName(msBuildXsd.FullName), $"{microsoftBuildXsd}-g.cs")));
        }

        /// <summary>
        /// Tests that "gen '(file)'" without the -a flag keeps the legacy *.xsd.cs output file.
        /// </summary>
        [Test]
        public void TestGenerateCodeWithoutAutoConfigKeepsLegacyOutputFile()
        {
            _copyOfSchemasFolder.Refresh();
            var microsoftBuildXsd = "Microsoft.Build.xsd";
            var msBuildXsd = _copyOfSchemasFolder.GetFiles(microsoftBuildXsd, SearchOption.AllDirectories).Single();

            var legacyOutputFile = Path.Combine(Path.GetDirectoryName(msBuildXsd.FullName), $"{microsoftBuildXsd}.cs");
            File.WriteAllText(legacyOutputFile, "// legacy generated code");
            Assert.IsTrue(File.Exists(legacyOutputFile));

            var genCodeResult = LinqToXsd.Program.Main(new[] {"gen", msBuildXsd.FullName});

            Assert.IsTrue(genCodeResult == 0);
            Assert.IsTrue(File.Exists(legacyOutputFile));
            Assert.IsTrue(File.Exists(Path.Combine(Path.GetDirectoryName(msBuildXsd.FullName), $"{microsoftBuildXsd}-g.cs")));
        }

        /// <summary>
        /// Tests that "gen '(file)'" splits the generated code into one file per class when the configuration sets
        /// "&lt;SplitCodeFiles By="Class" /&gt;".
        /// </summary>
        [Test]
        public void TestGenerateCodeSplitsByClassWhenConfigured()
        {
            _copyOfSchemasFolder.Refresh();
            const string schemaFileName = "abstracttest.xsd";
            var schemaFile = _copyOfSchemasFolder.GetFiles(schemaFileName, SearchOption.AllDirectories).Single();
            var outputFolder = schemaFile.Directory.FullName;

            var configFile = WriteConfigWithSplitCodeFiles(schemaFile.FullName, SplitCodeFiles.ByEnum.Class);

            var genCodeResult = LinqToXsd.Program.Main(new[] { "gen", schemaFile.FullName, "-c", configFile });
            Assert.IsTrue(genCodeResult == 0);

            var generatedFiles = _copyOfSchemasFolder.GetFiles("*.xsd-g.cs", SearchOption.AllDirectories);

            // one file per type declaration, rather than a single 'abstracttest.xsd-g.cs'
            Assert.IsTrue(generatedFiles.Length > 1);
            Assert.IsFalse(File.Exists(Path.Combine(outputFolder, $"{schemaFileName}{LinqToXsd.Program.GenerateCodeExtension}")));
            Assert.IsTrue(generatedFiles.Any(f => f.Name == $"LinqToXsdTypeManager{XObjectsCoreGenerator.SplitCodeFileSuffix}"));

            foreach (var generatedFile in generatedFiles) {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(generatedFile.FullName));
                tree.GetDiagnostics().CheckForZeroDiagnosticsAndErrorsOrThrow(generatedFile.Name);

                var declaredTypes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>();
                Assert.AreEqual(1, declaredTypes.Count(), $"Expected one type declaration in {generatedFile.Name}");
            }
        }

        /// <summary>
        /// Tests that "gen '(file)'" splits the generated code into one file per CLR namespace when the configuration sets
        /// "&lt;SplitCodeFiles By="Namespace" /&gt;", using the file name each Namespace element maps its CLR namespace to.
        /// </summary>
        [Test]
        public void TestGenerateCodeSplitsByNamespaceWhenConfigured()
        {
            _copyOfSchemasFolder.Refresh();
            const string schemaFileName = "abstracttest.xsd";
            var schemaFile = _copyOfSchemasFolder.GetFiles(schemaFileName, SearchOption.AllDirectories).Single();

            var configFile = WriteConfigWithSplitCodeFiles(schemaFile.FullName, SplitCodeFiles.ByEnum.Namespace,
                config => {
                    foreach (var ns in config.Namespaces.Namespace) ns.File = $"split.{ns.Clr}.cs";
                });

            var genCodeResult = LinqToXsd.Program.Main(new[] { "gen", schemaFile.FullName, "-c", configFile });
            Assert.IsTrue(genCodeResult == 0);

            var clrNamespace = Configuration.Load(configFile).Namespaces.Namespace.Single().Clr;
            var expectedOutputFile = Path.Combine(schemaFile.Directory.FullName, $"split.{clrNamespace}.cs");
            Assert.IsTrue(File.Exists(expectedOutputFile), $"Expected the Namespace/@File output file: {expectedOutputFile}");
            Assert.IsFalse(File.Exists(Path.Combine(schemaFile.Directory.FullName, $"{schemaFileName}{LinqToXsd.Program.GenerateCodeExtension}")));

            // every type of the schema belongs in that one output file, because it declares one CLR namespace
            var generatedCode = File.ReadAllText(expectedOutputFile);
            StringAssert.Contains($"namespace {clrNamespace}", generatedCode);
            StringAssert.Contains("partial class UpdateCommand", generatedCode);
            // the only output is that file: no combined 'abstracttest.xsd-g.cs' and no other split file names
            Assert.IsTrue(_copyOfSchemasFolder.GetFiles("*.xsd-g.cs", SearchOption.AllDirectories).Length == 0);
        }

        /// <summary>
        /// Tests that "gen '(file)' -o '(file).cs'" fails when the configuration asks for the generated code to be split,
        /// because one output file cannot hold code that belongs in several files.
        /// </summary>
        [Test]
        public void TestGenerateCodeThrowsWhenSplittingConfiguredAndOutputIsAFile()
        {
            _copyOfSchemasFolder.Refresh();
            var schemaFile = _copyOfSchemasFolder.GetFiles("abstracttest.xsd", SearchOption.AllDirectories).Single();
            var configFile = WriteConfigWithSplitCodeFiles(schemaFile.FullName, SplitCodeFiles.ByEnum.Class);

            var outputFile = Path.Combine(schemaFile.Directory.FullName, "all-in-one.cs");

            Assert.Throws<LinqToXsdException>(() => LinqToXsd.Program.Main(new[] { "gen", schemaFile.FullName, "-c", configFile, "-o", outputFile }));
            Assert.IsFalse(File.Exists(outputFile));
        }

        /// <summary>
        /// Tests that "gen '(file1),(file2)'" splits by class for several schemas at once without dropping the types of
        /// either schema, even though both of them generate types with the same names (such as LinqToXsdTypeManager).
        /// </summary>
        [Test]
        public void TestGenerateCodeSplitsByClassForMultipleSchemasWithoutDroppingTypes()
        {
            _copyOfSchemasFolder.Refresh();
            var abstractTestSchema = _copyOfSchemasFolder.GetFiles("abstracttest.xsd", SearchOption.AllDirectories).Single();
            var taskSchedulerSchema = _copyOfSchemasFolder.GetFiles("windowsTaskSched.xsd", SearchOption.AllDirectories).Single();

            var configFile = WriteConfigWithSplitCodeFiles(abstractTestSchema.FullName, SplitCodeFiles.ByEnum.Class);

            var schemaFiles = $"{abstractTestSchema.FullName},{taskSchedulerSchema.FullName}";
            var genCodeResult = LinqToXsd.Program.Main(new[] { "gen", schemaFiles, "-c", configFile });
            Assert.AreEqual(0, genCodeResult);

            var generatedFileNames = _copyOfSchemasFolder
                .GetFiles("*.xsd-g.cs", SearchOption.AllDirectories)
                .Select(f => f.Name)
                .ToList();

            // file names that both schemas would have produced are prefixed with the name of the schema they came from
            CollectionAssert.Contains(generatedFileNames, $"abstracttest.LinqToXsdTypeManager{XObjectsCoreGenerator.SplitCodeFileSuffix}");
            CollectionAssert.Contains(generatedFileNames, $"windowsTaskSched.LinqToXsdTypeManager{XObjectsCoreGenerator.SplitCodeFileSuffix}");
            CollectionAssert.Contains(generatedFileNames, $"abstracttest.XRoot{XObjectsCoreGenerator.SplitCodeFileSuffix}");
            CollectionAssert.Contains(generatedFileNames, $"windowsTaskSched.XRoot{XObjectsCoreGenerator.SplitCodeFileSuffix}");
        }

        /// <summary>
        /// Writes a configuration file beside the given schema, as "config -e" would, but with a SplitCodeFiles element.
        /// </summary>
        /// <returns>The full path of the configuration file.</returns>
        private static string WriteConfigWithSplitCodeFiles(string xsdFilePath, SplitCodeFiles.ByEnum splitBy,
            Action<Configuration> customiseConfig = null)
        {
            var config = Configuration.LoadForSchema(XDocument.Load(xsdFilePath));
            config.CodeGeneration.SplitCodeFiles = new SplitCodeFiles { By = splitBy };

            customiseConfig?.Invoke(config);

            var configFile = $"{xsdFilePath}.config";
            config.Save(configFile);

            return configFile;
        }

        /// <summary>
        /// Delete the test Schemas folder if it exists.
        /// </summary>
        [SetUp]
        public void Up()
        {
            //var fs = GetFileSystemForAssemblyName("MSBuild");
            //fs.CopyToRealFileSystem(Environment.CurrentDirectory);

            var names = new[] { "XSD", "XQueryX", "XMLSpec", "Windows", "TraML", "ThermoML", "MSBuild", "Atom", "AbstractTypeTest" };
            foreach (var mockFileSystem in GetFileSystemForAssemblyNames(names)) {
                mockFileSystem.CopyToRealFileSystem(Environment.CurrentDirectory);
            }

            CopySchemasFolder();
        }

        /// <summary>
        /// Delete the test Schemas folder always.
        /// </summary>
        [TearDown]
        public void Down()
        {
            DeleteSchemasFolder();
        }
    }
}