#nullable enable

using System.Collections.Generic;

namespace Xml.Schema.Linq.CodeGen.Model;

public interface IHasTypes
{
    List<CClass> Types { get; }

    void Add(CClass type);
}