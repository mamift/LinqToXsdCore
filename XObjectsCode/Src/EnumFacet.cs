//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Globalization;
using System.Linq;

namespace Xml.Schema.Linq.CodeGen
{
    /// <summary>
    /// Represents a facet of an enumeration, including its original value, validity, and a valid identifier.
    /// </summary>
    /// <remarks>This class is designed to handle enumeration member names, ensuring they conform to valid
    /// identifier rules. If the provided value is not a valid identifier, a valid identifier is generated.<br/>
    /// For invalid values, the string representation includes both the <see cref="Value"/> and <see cref="Member"/>
    /// properties, separated by a colon.<br/>
    /// This format is useful to convert invalid string values into valid enum values and vice versa.<br/>
    /// See also the <see cref="EnumFacetMapping"/> class that is used during the runtime conversion.
    /// </remarks>
    public class EnumFacet
    {
        public EnumFacet(string value)
        {
            this.Value   = value;
            this.IsValid = string.IsNullOrWhiteSpace(value) || CodeDomHelper.CodeProvider.IsValidIdentifier(value);
            this.Member  = this.IsValid ? value : CreateValidIdentifier(value);
        }

        public string   Value   { get; }
        public bool     IsValid { get; }
        public string   Member  { get; }

        public override string ToString() => this.IsValid ? this.Value : $"{this.Value}:{this.Member}";

        private static string CreateValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (NameGenerator.IsKeyword(value)) {
                return $"@{value}";
            }

            if (value.Length == 1) {
                char ch = value[0];
                if (char.IsSymbol(ch) || char.IsPunctuation(ch)) {
                    return NameGenerator.ExpandSymbolToFullWord(value[0]);
                }
            }

            var invalidChars = value
                .GroupBy(char.GetUnicodeCategory)
                .Where(g => !ValidUnicodeCategories.Contains(g.Key))
                .SelectMany(_ => _)
                .Distinct();

            foreach(var c in invalidChars)
            {
                value = value.Replace(c, '_');
            }
            if (char.IsDigit(value[0]))
            {
                value = '_' + value;
            }

            value = CodeDomHelper.CodeProvider.CreateValidIdentifier(value);

            return value;
        }

        // allows letter (Lu, Ll, Lt, Lm, or Nl), digit (Nd), connecting (Pc), combining (Mn or Mc), and formatting (Cf) categories
        private static readonly UnicodeCategory[] ValidUnicodeCategories = new UnicodeCategory[]
        {
            UnicodeCategory.UppercaseLetter,
            UnicodeCategory.LowercaseLetter,
            UnicodeCategory.TitlecaseLetter,
            UnicodeCategory.ModifierLetter,
            UnicodeCategory.NonSpacingMark,
            UnicodeCategory.SpacingCombiningMark,
            UnicodeCategory.DecimalDigitNumber,
            UnicodeCategory.LetterNumber,
            UnicodeCategory.Format,
            UnicodeCategory.ConnectorPunctuation,
        };
    }
}