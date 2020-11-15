namespace Xml.Schema.Linq.CodeGen
{
    /// <summary>
    /// Represents a interface to discern type name and namespace.
    /// </summary>
    internal interface ICommonTypeInfoBase
    {
        /// <summary>
        /// The type name; should not include any nested type names.
        /// </summary>
        string CommonTypeName { get; }

        /// <summary>
        /// Namespace this type is being generated in; can include class name if its nested under one.
        /// </summary>
        string CommonTypeNamespace { get; }
    }
}