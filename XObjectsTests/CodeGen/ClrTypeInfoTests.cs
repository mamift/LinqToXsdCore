using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using Xml.Schema.Linq.CodeGen;
using Xml.Schema.Linq.Extensions;
using XObjects;

namespace Xml.Schema.Linq.Tests.CodeGen;

public class ClrTypeInfoTests: BaseTester
{
    /// <summary>
    /// Tests the required methods for generating a type definition and type validator for a simple type that is a union of other simple types.
    /// <para>
    /// Used for anonymous simple type unions that are declared inline in an attribute or element (i.e. not defined with a name in the global scope of a compiled <see cref="XmlSchemaSet.GlobalTypes"/>).
    /// </para>
    /// </summary>
    /// <param name="endsWithFilePattern"></param>
    [Test, TestCase("StuDateAndTime.xsd")]
    public void CreateSimpleTypeForAnonymousSimpleTypeUnion(string endsWithFilePattern)
    {
        XmlSchemaSet xsd = GetTestFileAsXmlSchemaSet(endsWithFilePattern);

        var anonUnionTypes = xsd.RetrieveAllAnonymousSimpleUnionTypes();

        Assert.NotNull(anonUnionTypes);

        foreach (var simpleType in anonUnionTypes) {
            //Assert.IsInstanceOf<XmlSchemaSimpleTypeUnion>(simpleType.Value);
            // create the simple type from the XmlSchemaSimpleType
            XmlSchemaSimpleType xmlSimpleType = simpleType.Value;

            ClrSimpleTypeInfo? type = ClrSimpleTypeInfo.CreateSimpleTypeInfo(xmlSimpleType);
            Assert.NotNull(type);

            // the method ClrSimpleTypeInfo.CreateSimpleTypeInfo(simpleType.Value) should return a UnionSimpleTypeInfo
            Assert.IsInstanceOf<UnionSimpleTypeInfo>(type);
            var unionTypeInfo = type as UnionSimpleTypeInfo;
            Assert.True(unionTypeInfo != null);

            //the name gets filled out later on
            Assert.IsNull(unionTypeInfo!.clrtypeName);

            unionTypeInfo!.clrtypeName = xmlSimpleType.GenerateAdHocNameForSimpleUnionType();
            Assert.IsNotNull(unionTypeInfo.clrtypeName);
            Assert.IsNotEmpty(unionTypeInfo.clrtypeName);

            CodeTypeDeclaration? typeDef = TypeBuilder.CreateSimpleType(unionTypeInfo, new Dictionary<XmlSchemaObject, string>(),
                new LinqToXsdSettings());
            typeDef.ChangeVisibility(TypeAttributes.NestedPrivate);
            Assert.AreEqual(typeDef.Name, unionTypeInfo.clrtypeName);

            string typeDefCodeStr = typeDef.ToCodeString();
            Assert.NotNull(typeDefCodeStr);

            SourceText text = SourceText.From(typeDefCodeStr);
            CSharpSyntaxTree tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot() as CompilationUnitSyntax;

            Assert.NotNull(root);
            Assert.True(root!.Members.Count == 1);
            var classDef = root!.Members.Single() as ClassDeclarationSyntax;
            Assert.NotNull(classDef);
        }
    }
}