using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    public partial class XsdToTypesGeneratorTests
    {
        public static readonly DirectoryInfo ToySchemasDirectory =
            new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, @"Toy schemas\"));

        public static readonly List<FileInfo> ToySchemaFiles =
            ToySchemasDirectory.EnumerateFiles("*.xsd", SearchOption.AllDirectories).ToList();

        [Test]
        public void TestMappingsGeneratedForEnumsForElementsWithComplexType()
        {
            var xsd = ToySchemaFiles.First(f => f.Name == "EnumsForElementsWithComplexType.xsd");
            var mapping = Utilities.GenerateMapping(xsd);

            Assert.IsTrue(mapping.NameMappings.Count == 5);
            Assert.IsTrue(mapping.Types.Count == 5);
        }

        [Test]
        public void TestMappingsGeneratedForEnumsForAttributesWithComplexType()
        {
            var xsd = ToySchemaFiles.First(f => f.Name == "EnumsForAttributesWithComplexType.xsd");
            var mapping = Utilities.GenerateMapping(xsd);

            Assert.IsTrue(mapping.NameMappings.Count == 5);
            Assert.IsTrue(mapping.Types.Count == 5);
        }

        [Test]
        public void TestInnerTypeMappingsForEnumsForAttributesWithComplexType()
        {
            var xsd = ToySchemaFiles.First(f => f.Name == "EnumsForAttributesWithComplexType.xsd");
            var mapping = Utilities.GenerateMapping(xsd);

            Assert.IsTrue(mapping.NameMappings.Count == 5);
            Assert.IsTrue(mapping.Types.Count == 5);

            var wrapperElementTypes = mapping.Types.Where(t => t.CommonTypeName.StartsWith("Element") && t.IsWrapper).ToList();
            var complexTypes = mapping.Types.Where(t => t.CommonTypeName.StartsWith("Element") && !t.IsWrapper).ToList();

            Assert.IsTrue(wrapperElementTypes.Count == 2);
            Assert.IsTrue(complexTypes.Count == 2);

            Assert.IsTrue(wrapperElementTypes.All(et => et is ClrWrapperTypeInfo));
            Assert.IsTrue(complexTypes.All(ct => ct is ClrContentTypeInfo));
        }

        [Test]
        public void TestWrapperTypeMappingsGeneratedForEnumsForElementsWithComplexType()
        {
            var xsd = ToySchemaFiles.First(f => f.Name == "EnumsForElementsWithComplexType.xsd");
            ClrMappingInfo mapping = Utilities.GenerateMapping(xsd);

            Assert.IsTrue(mapping.NameMappings.Count == 5);
            Assert.IsTrue(mapping.Types.Count == 5);

            ClrTypeInfo element1Type = mapping.Types.Find(t => t.CommonTypeName == "Element1");
            Assert.IsNotNull(element1Type);
            Assert.IsInstanceOf<ClrWrapperTypeInfo>(element1Type);
            var element1WrapperType = (ClrWrapperTypeInfo)element1Type;
            Assert.IsTrue(element1WrapperType.InnerType.IsNamedComplexType);

            ClrTypeInfo element2Type = mapping.Types.Find(t => t.CommonTypeName == "Element2");
            Assert.IsNotNull(element2Type);
            Assert.IsInstanceOf<ClrWrapperTypeInfo>(element2Type);
            var element2WrapperType = (ClrWrapperTypeInfo)element2Type;
            Assert.IsTrue(element2WrapperType.InnerType.IsNamedComplexType);
        }

        [Test]
        public void TestClrTypesGeneratedForEnumsForElementsWithComplexTypes()
        {
            var xsd = ToySchemaFiles.First(f => f.Name == "EnumsForElementsWithComplexType.xsd");

            var namespaces = Utilities.GenerateTypes(xsd);
            Assert.IsTrue(namespaces.Count == 1);

            var theNamespace = namespaces.First();
            Assert.IsTrue(theNamespace.Types.Count == 8);

            var allEnumTypes = theNamespace.DescendentTypeScopedEnumDeclarations();

            Assert.IsTrue(allEnumTypes.Count > 0);

            var userTypes = theNamespace.Types.Cast<CodeTypeDeclaration>().Take(theNamespace.Types.Count - 3)
                .Concat(allEnumTypes).ToList();

            Assert.IsTrue(userTypes.Count == 5);

            var source = Utilities.GenerateSourceText(xsd.FullName);

            var csCode = new FileInfo(xsd.FullName + ".cs");
            using StreamWriter streamWriter = csCode.CreateText();
            source.Write(streamWriter);

            csCode.Refresh();
        }

        [Test]
        public void TestClrTypesGeneratedForEnumsForAttributesWithComplexTypes()
        {
            var xsd = ToySchemaFiles.First(f => f.Name == "EnumsForAttributesWithComplexType.xsd");

            var namespaces = Utilities.GenerateTypes(xsd);
            Assert.IsTrue(namespaces.Count == 1);

            var theNamespace = namespaces.First();
            Assert.IsTrue(theNamespace.Types.Count == 8);

            var allEnumTypes = theNamespace.DescendentTypeScopedEnumDeclarations();

            Assert.IsTrue(allEnumTypes.Count > 0);

            var userTypes = theNamespace.Types.Cast<CodeTypeDeclaration>().Take(theNamespace.Types.Count - 3)
                .Concat(allEnumTypes).ToList();

            Assert.IsTrue(userTypes.Count == 6);

            var source = Utilities.GenerateSourceText(xsd.FullName);

            var csCode = new FileInfo(xsd.FullName + ".cs");
            using StreamWriter streamWriter = csCode.CreateText();
            source.Write(streamWriter);

            csCode.Refresh();
        }
    }
}