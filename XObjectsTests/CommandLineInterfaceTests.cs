using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    [TestFixture]
    public class CommandLineInterfaceTests
    {
        private static readonly DirectoryInfo SchemasFolder = new DirectoryInfo(@".\Schemas");
        private const string SchemasCopy = @".\Schemas_Test";
        private static DirectoryInfo _copyOfSchemasFolder = new DirectoryInfo(SchemasCopy);

        public static void CopySchemasFolder()
        {
            if (_copyOfSchemasFolder.Exists) DeleteSchemasFolder();
            _copyOfSchemasFolder = SchemasFolder.Copy(@".\Schemas_test", overwrite: true);
            _copyOfSchemasFolder.DeleteFilesInside("*.config");
        }

        public static void DeleteSchemasFolder()
        {
            _copyOfSchemasFolder.Delete(true);
            _copyOfSchemasFolder.Refresh();
        }

        [Test]
        public void TestGenerateConfigurationFilesFromFolder()
        {
            Assert.IsFalse(_copyOfSchemasFolder.Exists);
            CopySchemasFolder();

            var configFiles = _copyOfSchemasFolder.GetFiles("*.config", SearchOption.AllDirectories);

            Assert.IsFalse(configFiles.Any());

            Assert.IsNotNull(_copyOfSchemasFolder);
            Assert.IsTrue(_copyOfSchemasFolder.Exists);
            
            var programResult = LinqToXsd.Program.Main(new[] {"config", "-e", _copyOfSchemasFolder.FullName});

            configFiles = _copyOfSchemasFolder.GetFiles("*.config", SearchOption.AllDirectories);

            Assert.IsTrue(configFiles.Any());
            Assert.IsTrue(configFiles.Length == 2);
        }

        [SetUp]
        public void Up()
        {
            if (_copyOfSchemasFolder.Exists) DeleteSchemasFolder();
        }

        [TearDown]
        public void Down()
        {
            DeleteSchemasFolder();
        }
    }
}