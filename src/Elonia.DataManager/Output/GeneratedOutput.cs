using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public sealed class GeneratedOutput
    {
        public SortedDictionary<string, string> CodeFiles { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal);
        public SortedDictionary<string, byte[]> DataFiles { get; } = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
    }
}
