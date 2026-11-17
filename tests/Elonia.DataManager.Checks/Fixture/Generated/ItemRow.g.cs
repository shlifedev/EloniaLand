#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Checks.Generated
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class ItemRow
    {
        [MemoryPackOrder(0)]
        public string Id { get; }

        [MemoryPackOrder(1)]
        public string Name { get; }

        [MemoryPackOrder(2)]
        public int Price { get; }

        [MemoryPackOrder(3)]
        public EKind Kind { get; }

        [MemoryPackConstructor]
        public ItemRow(string id, string name, int price, EKind kind)
        {
            Id = id;
            Name = name;
            Price = price;
            Kind = kind;
        }
    }
}
