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
        /// Determines if a string is NOT <c>null</c>, empty or all whitespace.
        /// </summary>
        /// <remarks>Because I hate using <c>!</c></remarks>
        /// <param name="theString"></param>
        /// <returns></returns>
        public static bool IsNotEmpty(this string theString) => !string.IsNullOrWhiteSpace(theString);
    }
}
