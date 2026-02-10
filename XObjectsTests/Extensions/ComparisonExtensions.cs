using System.Collections.Generic;
using System.Linq;
using ObjectsComparer;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class ComparisonExtensions
{
    public static List<Difference> CompareObjects<T>(this IEnumerable<T> one, IEnumerable<T> others, ComparisonSettings? settings = null)
    {
        var c = new Comparer(settings ?? new ComparisonSettings() {
            EmptyAndNullEnumerablesEqual = true,
            RecursiveComparison = true,
        });

        return c.CalculateDifferences(one, others).ToList();
    }
}