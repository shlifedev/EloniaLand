#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class ItemRow
    {
        [MemoryPackOrder(0)]
        public string Id { get; }

        [MemoryPackOrder(1)]
        public string Name { get; }

        [MemoryPackOrder(2)]
        public EItemEffect Effect { get; }

        [MemoryPackOrder(3)]
        public int Amount { get; }

        [MemoryPackConstructor]
        public ItemRow(string id, string name, EItemEffect effect, int amount)
        {
            Id = id;
            Name = name;
            Effect = effect;
            Amount = amount;
        }
    }
}
