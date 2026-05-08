using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Schemas.SharePoint;
using NUnit.Framework;

namespace Xml.Schema.Linq.Tests
{
    /// <summary>
    /// Tests the use of generated code emitted from 'wss.xsd'.
    /// </summary>
    [TestFixture]
    public class WssApiUseTests
    {
        /// <summary>
        /// Under the <see cref="ruleDesignerType.FieldBindLocalType"/> class there is an inline declared enum
        /// called <see cref="ruleDesignerType.FieldBindLocalType.DesignerTypeEnum"/>.
        /// </summary>
        [Test]
        public void TestUseOfDesignerTypeEnum1()
        {
            ruleDesignerType.FieldBindLocalType newFieldBind = null;
            ruleDesignerType designerType = null;

            Assert.DoesNotThrow(() => {
                newFieldBind = new ruleDesignerType.FieldBindLocalType() {
                    DesignerType = ruleDesignerType.FieldBindLocalType.DesignerTypeEnum.AddPermission
                };

                designerType = new ruleDesignerType() {
                    FieldBind = {
                        newFieldBind
                    }
                };
            });

            XName designerTypeAttr = XName.Get(nameof(ruleDesignerType.FieldBindLocalType.DesignerType));
            XAttribute underlyingValue = newFieldBind.Untyped.Attributes(designerTypeAttr).First();
            const string expectedValue = nameof(ruleDesignerType.FieldBindLocalType.DesignerTypeEnum.AddPermission);
            Assert.IsTrue(underlyingValue.Value == expectedValue);
        }

        /// <summary>
        /// Under the <see cref="parametersType.ParameterLocalType"/> class there is a second inline declared enum
        /// called <see cref="parametersType.ParameterLocalType.DesignerTypeEnum"/>.
        /// </summary>
        [Test]
        public void TestUseOfDesignerTypeEnum2()
        {
            parametersType.ParameterLocalType newParamLocal = null;
            parametersType newParameters = null;

            Assert.DoesNotThrow(() => {
                newParamLocal = new parametersType.ParameterLocalType() {
                    DesignerType = parametersType.ParameterLocalType.DesignerTypeEnum.DataSourceFieldNames
                };

                newParameters = new parametersType() {
                    Parameter = new List<parametersType.ParameterLocalType>() {
                        newParamLocal
                    }
                };
            });

            XName designerTypeAttr = XName.Get(nameof(parametersType.ParameterLocalType.DesignerType));
            XAttribute underlyingValue = newParamLocal.Untyped.Attributes(designerTypeAttr).First();
            const string expectedValue = nameof(parametersType.ParameterLocalType.DesignerTypeEnum.DataSourceFieldNames);
            Assert.IsTrue(underlyingValue.Value == expectedValue);
        }

        /// <summary>
        /// The purpose of this test is to ensure when getting the value of <see cref="Feature.Hidden"/> (which is an enum type), no exceptions occur when parsing and returning
        /// the enum string value (which is what happens under the hood with the XObjects API as it invokes <see cref="Enum.Parse(System.Type,System.string)"/>).
        /// <para>lower case 'false' does clash with C# keyword.</para>
        /// </summary>
        [Test]
        public void TrueFalseTest_EnumValueClashesWithCSharpKeyword()
        {
            var newF = new Feature(new FeatureDefinition() {
                Hidden = TRUEFALSE.@false
            });

            var isHidden = newF.Hidden;

            Assert.NotNull(isHidden);
            Assert.AreEqual(isHidden, TRUEFALSE.@false);
        }

        /// <summary>
        /// The purpose of this test is to ensure when getting the value of <see cref="Feature.Hidden"/> (which is an enum type), no exceptions occur when parsing and returning
        /// the enum string value (which is what happens under the hood with the XObjects API as it invokes <see cref="Enum.Parse(System.Type,System.string)"/>).
        /// <para>Upper case 'TRUE' does not clash with C# keyword.</para>
        /// </summary>
        [Test]
        public void TrueFalseTest_EnumValueIsNotCSharpKeyword()
        {
            var newF = new Feature(new FeatureDefinition() {
                Hidden = TRUEFALSE.TRUE
            });

            var isHidden = newF.Hidden;

            Assert.NotNull(isHidden);
            Assert.AreEqual(isHidden, TRUEFALSE.TRUE);
        }
    }
}