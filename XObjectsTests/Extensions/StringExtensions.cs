using System.Linq;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class StringExtensions
{
    public static string StripToDigits(this string str)
    {
        return new string(str.Where(char.IsDigit).ToArray());
    }
}