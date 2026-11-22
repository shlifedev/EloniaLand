#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class ThingRow
    {
        [MemoryPackInclude, MemoryPackOrder(0)] private string _id = "";
        [MemoryPackInclude, MemoryPackOrder(1)] private string _name = "";
        [MemoryPackInclude, MemoryPackOrder(2)] private bool _item__has;
        [MemoryPackInclude, MemoryPackOrder(3)] private string _item = "";
        [MemoryPackInclude, MemoryPackOrder(4)] private int _stepDamage;

        [MemoryPackConstructor]
        private ThingRow()
        {
        }

        [MemoryPackIgnore] public string Id => _id;

        [MemoryPackIgnore] public string Name => _name;

        [MemoryPackIgnore] public ItemRef Item => new ItemRef(_item__has, _item);

        [MemoryPackIgnore] public int StepDamage => _stepDamage;

        public override string ToString() => "Thing(" + _id + ")";
    }
}
