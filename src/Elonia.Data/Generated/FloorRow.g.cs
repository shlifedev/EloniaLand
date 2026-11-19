#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class FloorRow
    {
        [MemoryPackOrder(0)]
        public string Id { get; }

        [MemoryPackOrder(1)]
        public string Name { get; }

        [MemoryPackOrder(2)]
        public bool IsWalkable { get; }

        [MemoryPackConstructor]
        public FloorRow(string id, string name, bool isWalkable)
        {
            Id = id;
            Name = name;
            IsWalkable = isWalkable;
        }
    }
}
