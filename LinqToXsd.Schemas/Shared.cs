using System;
using System.Xml.Schema;
using Xml.Schema.Linq;

namespace LinqToXsd.Schemas
{
	/// <summary>
	/// 
	/// </summary>
	public static class Shared
	{
		public static readonly string XmlSpecXsdFileName = @"XMLSpec\xmlspec.xsd";
		public static readonly string XmlSpecXsdConfigFileName = @"XMLSpec\xmlspec.xsd.config";

		public static string W3CXmlschemaV1XsdFileName = @"XSD\W3C XMLSchema v1.xsd";
		public static string W3CXmlschemaV1XsdConfigFileName = @"XSD\W3C XMLSchema v1.xsd.config";
		
		public static string WssXsdFileName = @"SharePoint2010\wss.xsd";
		public static string WssXsdConfigFileName = @"SharePoint2010\wss.xsd.config";

		public static readonly Lazy<XmlSchemaSet> XmlSpecSchemaSet =
			new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(XmlSpecXsdFileName));

		public static readonly Lazy<LinqToXsdSettings> XmlSpecLinqToXsdSettings =
			new Lazy<LinqToXsdSettings>(() => Configuration.Load(XmlSpecXsdConfigFileName).ToLinqToXsdSettings());

		public static readonly Lazy<XmlSchemaSet> W3CXmlSchemaSet =
			new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(W3CXmlschemaV1XsdFileName));

		public static readonly Lazy<XmlSchemaSet> WssXsdSchemaSet =
			new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(WssXsdFileName));
	}
}