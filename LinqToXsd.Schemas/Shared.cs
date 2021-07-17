using System;
using System.Xml.Schema;
using Xml.Schema.Linq;

namespace LinqToXsd.Schemas
{
    /// <summary>
    /// Provides quick access to compiled XML schema sets for sample XSDs.
    /// </summary>
    public static class Shared
    {
        public static readonly string XmlSpecXsdFileName = @"XMLSpec\xmlspec.xsd";

        public static readonly Lazy<XmlSchemaSet> XmlSpecSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(XmlSpecXsdFileName));

        public static readonly Lazy<LinqToXsdSettings> XmlSpecLinqToXsdSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(XmlSpecXsdFileName + ".config").ToLinqToXsdSettings());

        public static string W3CXmlschemaV1XsdFileName = @"XSD\W3C XMLSchema v1.xsd";

        public static readonly Lazy<XmlSchemaSet> W3CXmlSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(W3CXmlschemaV1XsdFileName));

        public static readonly Lazy<LinqToXsdSettings> W3CXmlSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(W3CXmlschemaV1XsdFileName + ".config").ToLinqToXsdSettings());

        public static string WssXsdFileName = @"SharePoint2010\wss.xsd";

        public static readonly Lazy<XmlSchemaSet> WssXsdSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(WssXsdFileName));

        public static readonly Lazy<LinqToXsdSettings> WssXsdSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(WssXsdFileName + ".config").ToLinqToXsdSettings());

        public static string AspNetSiteMapXsdFileName = @"AspNetSiteMaps\SiteMapSchema.xsd";

        public static readonly Lazy<XmlSchemaSet> AspNetSiteMapXsdSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(AspNetSiteMapXsdFileName));

        public static readonly Lazy<LinqToXsdSettings> AspNetSiteMapXsdSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(AspNetSiteMapXsdFileName + ".config").ToLinqToXsdSettings());

        public static string AtomXsdFileName = @"Atom\atom.xsd";

        public static readonly Lazy<XmlSchemaSet> AtomXsdSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(AtomXsdFileName));

        public static readonly Lazy<LinqToXsdSettings> AtomXsdSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(AtomXsdFileName + ".config").ToLinqToXsdSettings());

        public static string MsBuildXsdFileName = @"MSBuild\Microsoft.Build.xsd";

        public static readonly Lazy<XmlSchemaSet> MsBuildXsdSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(MsBuildXsdFileName));

        public static readonly Lazy<LinqToXsdSettings> MsBuildXsdSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(MsBuildXsdFileName + ".config").ToLinqToXsdSettings());

        public static string OpmlXsdFileName = @"Opml\opml2.xsd";

        public static readonly Lazy<XmlSchemaSet> OpmlXsdSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(OpmlXsdFileName));

        public static readonly Lazy<LinqToXsdSettings> OpmlXsdSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(OpmlXsdFileName + ".config").ToLinqToXsdSettings());

        public static string PubmedCollectionsXsdFileName = @"Pubmed\collections.xsd";

        public static readonly Lazy<XmlSchemaSet> PubmedCollectionsXsdSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(PubmedCollectionsXsdFileName));

        public static readonly Lazy<LinqToXsdSettings> PubmedCollectionsXsdSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(PubmedCollectionsXsdFileName + ".config").ToLinqToXsdSettings());

        public static string PubmedEfetchXsdFileName = @"Pubmed\efetch-pubmed.xsd";

        public static readonly Lazy<XmlSchemaSet> PubmedEfetchXsdSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(PubmedEfetchXsdFileName));

        public static readonly Lazy<LinqToXsdSettings> PubmedEfetchXsdSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(PubmedEfetchXsdFileName + ".config").ToLinqToXsdSettings());

        public static string Rss2XsdFileName = @"Rss\rss-2_0.xsd";

        public static readonly Lazy<XmlSchemaSet> Rss2XsdSchemaSet =
            new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(Rss2XsdFileName));

        public static readonly Lazy<LinqToXsdSettings> Rss2XsdSchemaSetSettings =
            new Lazy<LinqToXsdSettings>(() => Configuration.Load(Rss2XsdFileName + ".config").ToLinqToXsdSettings());

        public static class ToySchemas
        {
            public static string NameMangledXsdFileName = @"NameMangled.xsd";

            public static readonly Lazy<XmlSchemaSet> NameMangledXsdSchemaSet =
                new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(NameMangledXsdFileName));

            public static readonly Lazy<LinqToXsdSettings> NameMangledXsdSchemaSetSettings =
                new Lazy<LinqToXsdSettings>(() => Configuration.Load(NameMangledXsdFileName + ".config").ToLinqToXsdSettings());

            public static string AbstractTypeTestXsdFileName = @"AbstractTypeTest\abstracttest.xsd";

            public static readonly Lazy<XmlSchemaSet> AbstractTypeTestXsdSchemaSet =
                new Lazy<XmlSchemaSet>(() => FileSystemUtilities.PreLoadXmlSchemas(AbstractTypeTestXsdFileName));

            public static readonly Lazy<LinqToXsdSettings> AbstractTypeTestXsdSchemaSetSettings =
                new Lazy<LinqToXsdSettings>(() => Configuration.Load(AbstractTypeTestXsdFileName + ".config").ToLinqToXsdSettings());
        }
    }
}