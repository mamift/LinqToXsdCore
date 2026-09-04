using System;
using System.Reflection;

namespace Xml.Schema.Linq.CodeGen.Extensions;

public static class AssemblyExtensions
{
    public static string GetAssemblyVersion(this Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        
        var infoVerAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoVerAttr != null && !string.IsNullOrEmpty(infoVerAttr.InformationalVersion)) {
            return infoVerAttr.InformationalVersion.Split('+')[0];
        }

        Version ver = assembly.GetName().Version;
        if (ver != null) {
            return ver.Build >= 0
                ? $"{ver.Major}.{ver.Minor}.{ver.Build}"
                : $"{ver.Major}.{ver.Minor}";
        }

        throw new InvalidOperationException($"Unable to determine Assembly version: {assembly.GetName()}");
    }
}