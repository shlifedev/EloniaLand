#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class ActorRow
    {
        [MemoryPackOrder(0)]
        public string Id { get; }

        [MemoryPackOrder(1)]
        public string Name { get; }

        [MemoryPackOrder(2)]
        public int MaxHp { get; }

        [MemoryPackOrder(3)]
        public int Attack { get; }

        [MemoryPackOrder(4)]
        public int Speed { get; }

        [MemoryPackOrder(5)]
        public bool IsHostile { get; }

        [MemoryPackOrder(6)]
        public int SenseRange { get; }

        [MemoryPackOrder(7)]
        public EActorBehavior Behavior { get; }

        [MemoryPackConstructor]
        public ActorRow(string id, string name, int maxHp, int attack, int speed, bool isHostile, int senseRange, EActorBehavior behavior)
        {
            Id = id;
            Name = name;
            MaxHp = maxHp;
            Attack = attack;
            Speed = speed;
            IsHostile = isHostile;
            SenseRange = senseRange;
            Behavior = behavior;
        }
    }
}
