#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class PropRow
    {
        [MemoryPackInclude, MemoryPackOrder(0)] private string _id = "";
        [MemoryPackInclude, MemoryPackOrder(1)] private string _name = "";
        [MemoryPackInclude, MemoryPackOrder(2)] private bool _isWalkable;
        [MemoryPackInclude, MemoryPackOrder(3)] private bool _dropThing__has;
        [MemoryPackInclude, MemoryPackOrder(4)] private string _dropThing = "";
        [MemoryPackInclude, MemoryPackOrder(5)] private int _dropAmount;

        [MemoryPackConstructor]
        private PropRow()
        {
        }

        [MemoryPackIgnore] public string Id => _id;

        [MemoryPackIgnore] public string Name => _name;

        [MemoryPackIgnore] public bool IsWalkable => _isWalkable;

        [MemoryPackIgnore] public ThingRef DropThing => new ThingRef(_dropThing__has, _dropThing);

        [MemoryPackIgnore] public int DropAmount => _dropAmount;

        public override string ToString() => "Prop(" + _id + ")";
    }
}
