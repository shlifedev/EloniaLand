#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Checks.Generated
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class EmptyRow
    {
        [MemoryPackOrder(0)]
        public int Value { get; }

        [MemoryPackOrder(1)]
        public string Note { get; }

        [MemoryPackConstructor]
        public EmptyRow(int value, string note)
        {
            Value = value;
            Note = note;
        }
    }
}
