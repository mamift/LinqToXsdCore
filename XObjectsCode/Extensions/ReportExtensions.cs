#nullable enable
using System;

namespace Xml.Schema.Linq.CodeGen;

public static class ReportExtensions
{
    public static void ReportError(this IProgress<string> progress, string message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ForegroundColor = original;
    }

    public static void ReportWarning(this IProgress<string> progress, string message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = original;
    }
}