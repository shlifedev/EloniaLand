#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class TileRow
    {
        [MemoryPackOrder(0)]
        public string Id { get; }

        [MemoryPackOrder(1)]
        public string Name { get; }

        [MemoryPackOrder(2)]
        public bool IsWalkable { get; }

        [MemoryPackOrder(3)]
        public bool HasHarvestResult { get; }

        [MemoryPackOrder(4)]
        public string HarvestResultKey { get; }

        [MemoryPackIgnore]
        public TileRef HarvestResult => new TileRef(HasHarvestResult, HarvestResultKey);

        [MemoryPackOrder(5)]
        public bool HasDropItem { get; }

        [MemoryPackOrder(6)]
        public string DropItemKey { get; }

        [MemoryPackIgnore]
        public ItemRef DropItem => new ItemRef(HasDropItem, DropItemKey);

        [MemoryPackOrder(7)]
        public int DropAmount { get; }

        [MemoryPackConstructor]
        public TileRow(string id, string name, bool isWalkable, bool hasHarvestResult, string harvestResultKey, bool hasDropItem, string dropItemKey, int dropAmount)
        {
            Id = id;
            Name = name;
            IsWalkable = isWalkable;
            HasHarvestResult = hasHarvestResult;
            HarvestResultKey = harvestResultKey;
            HasDropItem = hasDropItem;
            DropItemKey = dropItemKey;
            DropAmount = dropAmount;
        }
    }
}
