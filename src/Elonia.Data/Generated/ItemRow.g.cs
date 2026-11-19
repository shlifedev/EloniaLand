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
        public EItemEffect Effect { get; }

        [MemoryPackOrder(2)]
        public int Amount { get; }

        [MemoryPackConstructor]
        public ItemRow(string id, EItemEffect effect, int amount)
        {
            Id = id;
            Effect = effect;
            Amount = amount;
        }
    }
}
