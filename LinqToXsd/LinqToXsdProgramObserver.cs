using System;
using System.Text;
using Xml.Schema.Linq.CodeGen;

namespace LinqToXsd;

public class LinqToXsdProgramObserver: IWarnableObserver<string>
{
    private int errorCount = 0;
    private int warningCount = 0;

    public void OnNext(string value)
    {
        Console.WriteLine(value);
    }

    public void OnError(Exception error)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(error.ToString());
        Console.ForegroundColor = original;
        errorCount++;
    }

    public void OnCompleted()
    {
        var msg = new StringBuilder("Completed generation process;");
        if (errorCount > 0) {
            msg.AppendFormat($"There were {errorCount} errors.");
        }
        Console.WriteLine(msg.ToString());
    }

    public void OnWarn(string value, string message = null)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(value);
        Console.ForegroundColor = original;
        warningCount++;
    }
}