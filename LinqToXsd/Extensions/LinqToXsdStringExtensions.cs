using System.Collections.Generic;
using System.Linq;

namespace LinqToXsd;

public static class LinqToXsdStringExtensions
{
    public static int CountWords(this string str) 
    {
        IEnumerable<string> query = from word in str.Split(' ')
                    where !string.IsNullOrWhiteSpace(word)
                    select word;
        return query.Count();
    }
}