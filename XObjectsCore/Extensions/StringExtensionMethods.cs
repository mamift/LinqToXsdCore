using System.Linq;

namespace Xml.Schema.Linq.Extensions
{
    public static class StringExtensionMethods
    {
        /// <summary>
        /// Determines if a string is <c>null</c>, empty or all whitespace.
        /// </summary>
        /// <param name="theString"></param>
        /// <returns></returns>
        public static bool IsEmpty(this string theString) => string.IsNullOrWhiteSpace(theString);
        
        /// <summary>
        /// Determines if a string is NOT <c>null</c>, empty or all whitespace. The inverse of <see cref="IsEmpty"/>.
        /// </summary>
        /// <remarks>Because I hate using <c>!</c></remarks>
        /// <param name="theString"></param>
        /// <returns></returns>
        public static bool IsNotEmpty(this string theString) => !string.IsNullOrWhiteSpace(theString);

        /// <summary>
        /// Adds a number to the end of a string.
        /// </summary>
        /// <param name="theString"></param>
        /// <param name="delimiter">A spacing character. Defaults to underscore.</param>
        /// <returns></returns>
        public static string AppendNumberToString(this string theString, char delimiter = '_')
        {
            var numbers = theString.Where(char.IsNumber);
            var numberString = new string(numbers.ToArray());
            var stringWithoutNumbers = new string(theString.Where(char.IsLetter).ToArray());

            var possibleNumber = numberString.ParseInt();

            if (possibleNumber == null) return $"{theString}{delimiter}1";

            possibleNumber += 1;

            return $"{stringWithoutNumbers}{delimiter}{possibleNumber}";
        }

        /// <summary>
        /// Attempts to parse an <see cref="int"/> from a string or returns null if it couldn't successfully parse.
        /// </summary>
        /// <param name="theString"></param>
        /// <returns></returns>
        public static int? ParseInt(this string theString)
        {
            var didParse = int.TryParse(theString, out var theNumber);
            if (!didParse) return null;
            return theNumber;
        }

    }
}
