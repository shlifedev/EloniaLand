#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class ActorRow
    {
        [MemoryPackInclude, MemoryPackOrder(0)] private string _id = "";
        [MemoryPackInclude, MemoryPackOrder(1)] private string _name = "";
        [MemoryPackInclude, MemoryPackOrder(2)] private int _maxHp;
        [MemoryPackInclude, MemoryPackOrder(3)] private int _attack;
        [MemoryPackInclude, MemoryPackOrder(4)] private int _speed;
        [MemoryPackInclude, MemoryPackOrder(5)] private bool _isHostile;
        [MemoryPackInclude, MemoryPackOrder(6)] private int _senseRange;
        [MemoryPackInclude, MemoryPackOrder(7)] private EActorBehavior _behavior;

        [MemoryPackConstructor]
        private ActorRow()
        {
        }

        [MemoryPackIgnore] public string Id => _id;

        [MemoryPackIgnore] public string Name => _name;

        [MemoryPackIgnore] public int MaxHp => _maxHp;

        [MemoryPackIgnore] public int Attack => _attack;

        [MemoryPackIgnore] public int Speed => _speed;

        [MemoryPackIgnore] public bool IsHostile => _isHostile;

        [MemoryPackIgnore] public int SenseRange => _senseRange;

        [MemoryPackIgnore] public EActorBehavior Behavior => _behavior;

        public override string ToString() => "Actor(" + _id + ")";
    }
}
