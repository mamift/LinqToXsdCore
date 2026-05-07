using System;
using System.Collections.Generic;

namespace Xml.Schema.Linq.CodeGen.Scriban;

// Can be instantiated in templates using the global Scope() function in ScribanGlobals 
public class Scope()
{
    private HashSet<string> locals = new(StringComparer.OrdinalIgnoreCase);

    public Scope Init(params string[] names)
    {
        foreach (var name in names)
            locals.Add(name);
        return this;
    }

    public string Add(string name)
    {
        var suffixedName = name;
        int suffix = 1;
        while (!locals.Add(suffixedName))
            suffixedName = name + (suffix++);
        return suffixedName;
    }
}