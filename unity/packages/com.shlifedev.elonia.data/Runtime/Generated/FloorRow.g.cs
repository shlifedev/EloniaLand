#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class FloorRow
    {
        [MemoryPackInclude, MemoryPackOrder(0)] private string _id = "";
        [MemoryPackInclude, MemoryPackOrder(1)] private string _name = "";
        [MemoryPackInclude, MemoryPackOrder(2)] private bool _isWalkable;

        [MemoryPackConstructor]
        private FloorRow()
        {
        }

        [MemoryPackIgnore] public string Id => _id;

        [MemoryPackIgnore] public string Name => _name;

        [MemoryPackIgnore] public bool IsWalkable => _isWalkable;

        public override string ToString() => "Floor(" + _id + ")";
    }
}
