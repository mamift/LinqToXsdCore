using System.Collections.Generic;

namespace Xml.Schema.Linq.CodeGen;

public static class GraphExtensions
{
    /// <summary>
    /// Traverses a list of schemas and then compares against the given <paramref name="graph"/> that they are indeed entry point schemas.
    /// <para>Returns the list of schemas that are reachable from the entry point schemas.</para>
    /// </summary>
    /// <param name="schemas"></param>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static List<string> TraverseSchemas(this IEnumerable<Schema> schemas, Graph graph)
    {
        return new List<string>();
    }
}