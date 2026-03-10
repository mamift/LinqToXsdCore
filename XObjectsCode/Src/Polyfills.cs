using System.Collections.Generic;

namespace Xml.Schema.Linq.CodeGen;

internal static class Polyfills
{
    extension<TKey, TValue> (IReadOnlyDictionary<TKey, TValue> src)
    {
        #if !NET8_0_OR_GREATER
        
        public TValue GetValueOrDefault(TKey key, TValue @default)
            => src.TryGetValue(key, out var value) ? value : @default;

        #endif
    }
}