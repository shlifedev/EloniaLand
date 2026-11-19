#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class ThingRow
    {
        [MemoryPackOrder(0)]
        public string Id { get; }

        [MemoryPackOrder(1)]
        public string Name { get; }

        [MemoryPackOrder(2)]
        public bool HasItem { get; }

        [MemoryPackOrder(3)]
        public string ItemKey { get; }

        [MemoryPackIgnore]
        public ItemRef Item => new ItemRef(HasItem, ItemKey);

        [MemoryPackOrder(4)]
        public int StepDamage { get; }

        [MemoryPackConstructor]
        public ThingRow(string id, string name, bool hasItem, string itemKey, int stepDamage)
        {
            Id = id;
            Name = name;
            HasItem = hasItem;
            ItemKey = itemKey;
            StepDamage = stepDamage;
        }
    }
}
