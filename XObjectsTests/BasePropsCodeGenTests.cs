using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    public class BasePropsCodeGenTests: BaseTester
    {
        public MockFileSystem TestFiles { get; set; }
        private SyntaxTree Tree { get; set; }
        private List<ClassDeclarationSyntax> GeneratedTypes { get; set; }

        [SetUp]
        public void GenerateCode()
        {
            const string xsdFilePath = @"BasePropsTest\BasePropsTest.xsd";
            TestFiles = Utilities.GetAssemblyFileSystem(typeof(LinqToXsd.Schemas.Test.BasePropsTypes.Wrapper).Assembly);
            Tree = Utilities.GenerateSyntaxTree(xsdFilePath, TestFiles);

            Diagnostic[] diags = Utilities.GetSyntaxAndCompilationDiagnostics(Tree);
            diags.CheckForZeroDiagnosticsAndErrorsOrThrow(xsdFilePath);

            IEnumerable<SyntaxNode> nodes = Tree.GetNamespaceRoot().DescendantNodes();
            GeneratedTypes = nodes.OfType<ClassDeclarationSyntax>().ToList();
        }

        [Test]
        public void T1_WrapperShouldDeclarePropertiesOfBasesOfContentType()
        {
            ClassDeclarationSyntax type   = GeneratedTypes.Single(type => type.Identifier.Text == "Wrapper");
            PropertyDeclarationSyntax? prop1 = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "Base1Prop");
            PropertyDeclarationSyntax? prop2 = type.Members.OfType<PropertyDeclarationSyntax>().SingleOrDefault(prop => prop.Identifier.Text == "Base2Prop");
            Assert.IsNotNull(prop1);
            Assert.IsNotNull(prop2);
        }

        /// <summary>
        /// https://github.com/mamift/LinqToXsdCore/issues/106: when the content type re-declares an element of its base
        /// type, the wrapper must forward the content type's merged property once, not also the base type's property
        /// (which produced CS0102).
        /// </summary>
        [Test]
        public void T2_WhenContentTypeRedeclaresBaseElementThenWrapperForwardsItOnce()
        {
            ClassDeclarationSyntax type = GeneratedTypes.Single(type => type.Identifier.Text == "RedeclaringWrapper");
            List<PropertyDeclarationSyntax> props = type.Members.OfType<PropertyDeclarationSyntax>().Where(prop => prop.Identifier.Text == "Base1Prop").ToList();
            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("IList<string>", props[0].Type.ToString());

            Diagnostic[] duplicateMemberErrors = Utilities.GetSyntaxAndCompilationDiagnostics(Tree).Where(d => d.Id == "CS0102").ToArray();
            Assert.IsEmpty(duplicateMemberErrors, string.Join("\n", duplicateMemberErrors.Select(d => d.ToString())));
        }
    }
}