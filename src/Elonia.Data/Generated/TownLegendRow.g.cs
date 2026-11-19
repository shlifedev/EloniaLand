#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class TownLegendRow
    {
        [MemoryPackOrder(0)]
        public string Id { get; }

        [MemoryPackOrder(1)]
        public string Symbol { get; }

        [MemoryPackOrder(2)]
        public bool HasFloor { get; }

        [MemoryPackOrder(3)]
        public string FloorKey { get; }

        [MemoryPackIgnore]
        public FloorRef Floor => new FloorRef(HasFloor, FloorKey);

        [MemoryPackOrder(4)]
        public bool HasProp { get; }

        [MemoryPackOrder(5)]
        public string PropKey { get; }

        [MemoryPackIgnore]
        public PropRef Prop => new PropRef(HasProp, PropKey);

        [MemoryPackOrder(6)]
        public bool HasActor { get; }

        [MemoryPackOrder(7)]
        public string ActorKey { get; }

        [MemoryPackIgnore]
        public ActorRef Actor => new ActorRef(HasActor, ActorKey);

        [MemoryPackOrder(8)]
        public bool HasThing { get; }

        [MemoryPackOrder(9)]
        public string ThingKey { get; }

        [MemoryPackIgnore]
        public ThingRef Thing => new ThingRef(HasThing, ThingKey);

        [MemoryPackOrder(10)]
        public EHouseStyle HouseStyle { get; }

        [MemoryPackConstructor]
        public TownLegendRow(string id, string symbol, bool hasFloor, string floorKey, bool hasProp, string propKey, bool hasActor, string actorKey, bool hasThing, string thingKey, EHouseStyle houseStyle)
        {
            Id = id;
            Symbol = symbol;
            HasFloor = hasFloor;
            FloorKey = floorKey;
            HasProp = hasProp;
            PropKey = propKey;
            HasActor = hasActor;
            ActorKey = actorKey;
            HasThing = hasThing;
            ThingKey = thingKey;
            HouseStyle = houseStyle;
        }
    }
}
