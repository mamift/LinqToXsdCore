using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using ExtendedXmlSerializer;
using ExtendedXmlSerializer.Configuration;
using ExtendedXmlSerializer.ContentModel;
using ExtendedXmlSerializer.ContentModel.Format;
using Fasterflect;
using Xml.Schema.Linq.CodeGen;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class GeneralExtensions
{
    public static string ToXml(this ClrMappingInfo mapping)
    {
        IExtendedXmlSerializer serializer = new ConfigurationContainer()
            .EnableAllConstructors()
            .EnableParameterizedContent()
            .Create();

        var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, mapping);

        return stringWriter.ToString();
    }

    // clrTypeReference

    public class ClrTypeRefSerializer : ISerializer<ClrTypeReference>
    {
        public ClrTypeReference Get(IFormatReader parameter)
        {
            throw new NotImplementedException();
        }

        public void Write(IFormatWriter writer, ClrTypeReference instance)
        {
            var xml = instance.ToXml();
            writer.Content(xml);
        }
    }
}