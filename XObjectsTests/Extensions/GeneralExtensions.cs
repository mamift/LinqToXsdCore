using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
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
    public static string SerializeToXml<T>(this T thing)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter();
        serializer.Serialize(writer, thing);

        return writer.ToString();
    }

    public static string ToXml(this ClrMappingInfo mapping)
    {
        IExtendedXmlSerializer serializer = new ConfigurationContainer()
            .EnableAllConstructors()
            .EnableParameterizedContent()
            .Type<ClrTypeReference>().Register().Serializer().Using(new ClrTypeRefSerializer())
            .Type<ClrPropertyInfo>().Register().Serializer().Using(new ClrPropertyInfoSerializer())
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
            var xml = instance.ToXDoc();
            writer.Content(xml.ToString(SaveOptions.DisableFormatting));
        }
    }

    public class ClrPropertyInfoSerializer : ISerializer<ClrPropertyInfo>
    {
        public ClrPropertyInfo Get(IFormatReader parameter)
        {
            throw new NotImplementedException();
        }
        public void Write(IFormatWriter writer, ClrPropertyInfo instance)
        {
            var xml = instance.ToXml(FormatOptions.None);
            writer.Content(xml);
        }
    }
}