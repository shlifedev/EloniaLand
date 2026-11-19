#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class PropRow
    {
        [MemoryPackOrder(0)]
        public string Id { get; }

        [MemoryPackOrder(1)]
        public string Name { get; }

        [MemoryPackOrder(2)]
        public bool IsWalkable { get; }

        [MemoryPackOrder(3)]
        public bool HasDropThing { get; }

        [MemoryPackOrder(4)]
        public string DropThingKey { get; }

        [MemoryPackIgnore]
        public ThingRef DropThing => new ThingRef(HasDropThing, DropThingKey);

        [MemoryPackOrder(5)]
        public int DropAmount { get; }

        [MemoryPackConstructor]
        public PropRow(string id, string name, bool isWalkable, bool hasDropThing, string dropThingKey, int dropAmount)
        {
            Id = id;
            Name = name;
            IsWalkable = isWalkable;
            HasDropThing = hasDropThing;
            DropThingKey = dropThingKey;
            DropAmount = dropAmount;
        }
    }
}
