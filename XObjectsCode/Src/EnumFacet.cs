//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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
        public string   Member  { get; internal set; }

        public override string ToString() => this.IsValid ? this.Value : $"{this.Value}:{this.Member}";

        /// <summary>
        /// Creates the <see cref="EnumFacet"/> list for an enumeration, guaranteeing that every facet
        /// receives a member name that is unique within the enumeration.
        /// <para>Distinct schema values can map onto the same candidate member name - for instance the
        /// value "*" expands to "Asterisk", which collides with a literal "Asterisk" value, and both
        /// would be emitted as the same enum member and fail to compile. Colliding values are
        /// disambiguated with a deterministic suffix derived from the schema value, so the generated
        /// enum members and the 'value:member' strings stored in the validator facets always agree.</para>
        /// </summary>
        public static List<EnumFacet> CreateUniqueFacets(IEnumerable<string> values)
        {
            var facets = values.Distinct().Select(value => new EnumFacet(value)).ToList();

            foreach (var duplicates in facets.GroupBy(facet => facet.Member).Where(group => group.Count() > 1))
            {
                // prefer the facet whose schema value is already a valid identifier (e.g. "Asterisk" over "*")
                var kept = duplicates.FirstOrDefault(facet => facet.IsValid) ?? duplicates.First();
                foreach (var facet in duplicates.Where(facet => facet != kept))
                {
                    facet.Member = $"{kept.Member}_{GetDeterministicSuffix(facet.Value)}";
                }
            }

            // a crafted schema could still collide with a deterministic suffix (e.g. a literal value
            // spelled like one); fall back to a numbered suffix to keep the members unique
            var taken = new HashSet<string>(StringComparer.Ordinal);
            foreach (var facet in facets)
            {
                var member = facet.Member;
                var index = 1;
                while (!taken.Add(member))
                {
                    member = $"{facet.Member}{index++}";
                }
                facet.Member = member;
            }

            return facets;
        }

        private static string GetDeterministicSuffix(string value)
        {
            // FNV-1a: stable across runs and platforms, unlike String.GetHashCode
            uint hash = 2166136261;
            foreach (char ch in value)
            {
                hash ^= ch;
                hash *= 16777619;
            }

            return hash.ToString("X8", CultureInfo.InvariantCulture);
        }

        private static string CreateValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (NameGenerator.IsKeyword(value)) {
                return $"@{value}";
            }

            if (value.Length == 1) {
                char ch = value[0];
                if ((char.IsSymbol(ch) || char.IsPunctuation(ch)) &&
                    NameGenerator.TryExpandSymbolToFullWord(ch, out var singleSymbolWord)) {
                    return singleSymbolWord;
                }
            }

            // A value made up entirely of characters that cannot appear in a C# identifier
            // (e.g. "==", "<=", "&&") would have every character replaced with '_' by the
            // fallback below, so distinct values would collapse onto the same member name.
            // Expand each character to its full word name instead, keeping the members
            // distinct and readable (e.g. "==" becomes "EqualsEquals").
            if (value.All(ch => !ValidUnicodeCategories.Contains(char.GetUnicodeCategory(ch)))) {
                var expanded = new StringBuilder();
                foreach (char ch in value) {
                    if (!NameGenerator.TryExpandSymbolToFullWord(ch, out var symbolWord)) {
                        // unknown symbol; fall back to the underscore replacement below
                        expanded = null;
                        break;
                    }
                    expanded.Append(symbolWord);
                }

                if (expanded != null) {
                    return CodeDomHelper.CodeProvider.CreateValidIdentifier(expanded.ToString());
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