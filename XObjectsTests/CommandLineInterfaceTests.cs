using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class CommandLineInterfaceTests
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
            Assert.IsNotEmpty(_copyOfSchemasFolder.GetFiles("*.xsd.cs", SearchOption.AllDirectories));

            var generatedCsFile = _copyOfSchemasFolder.GetFiles($"{microsoftBuildXsd}.cs", SearchOption.AllDirectories);

            Assert.IsTrue(generatedCsFile.Any());
            var _ = generatedCsFile.Single();
        }

        /// <summary>
        /// Tests "linqtoxsd gen '(folder)' -a"
        /// </summary>
        [Test]
        public void TestGenerateCodeFromSingleDirectoryWithAutoConfig()
        {
            _copyOfSchemasFolder.Refresh();
            var microsoftBuildXsd = "Microsoft.Build.xsd";
            var msBuildXsd = _copyOfSchemasFolder.GetFiles(microsoftBuildXsd, SearchOption.AllDirectories).Single();

            var genConfigResult = LinqToXsd.Program.Main(new[] { "config", "-e", _copyOfSchemasFolder.FullName });
            Assert.IsTrue(genConfigResult == 0);
            Assert.IsNotEmpty(_copyOfSchemasFolder.GetFiles("*.xsd.config", SearchOption.AllDirectories));

            var directoryName = Path.GetDirectoryName(msBuildXsd.FullName);
            var genCodeResult = LinqToXsd.Program.Main(new[] {"gen", directoryName, "-a"});

            Assert.IsTrue(genCodeResult == 0);
            Assert.IsNotEmpty(_copyOfSchemasFolder.GetFiles("*.xsd.cs", SearchOption.AllDirectories));

            var generatedCsFile = _copyOfSchemasFolder.GetFiles($"{microsoftBuildXsd}.cs", SearchOption.AllDirectories);

            Assert.IsTrue(generatedCsFile.Any());
            var _ = generatedCsFile.Single();
        }

        /// <summary>
        /// Delete the test Schemas folder if it exists.
        /// </summary>
        [SetUp]
        public void Up()
        {
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