using System.Xml.Schema;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
	public class FileSystemUtilitiesTests
	{
		[Test]
		public void PreLoadXmlSchemasTest()
		{
			var xmlSpecXsd = @"XMLSpec\xmlspec.xsd";
			XmlSchemaSet xmlSpecSchemaSet = FileSystemUtilities.PreLoadXmlSchemas(xmlSpecXsd);

			Assert.IsNotNull(xmlSpecSchemaSet);
			Assert.IsTrue(xmlSpecSchemaSet.IsCompiled);
			Assert.IsTrue(xmlSpecSchemaSet.Count == 3);
		}
	}
}