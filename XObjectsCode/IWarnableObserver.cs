#nullable enable
using System;

namespace Xml.Schema.Linq.CodeGen;

public interface IWarnableObserver<in T>: IObserver<T>
{
    void OnWarn(T value, string? message = null);
}