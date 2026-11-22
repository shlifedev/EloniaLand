#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class ItemRow
    {
        [MemoryPackInclude, MemoryPackOrder(0)] private string _id = "";
        [MemoryPackInclude, MemoryPackOrder(1)] private EItemEffect _effect;
        [MemoryPackInclude, MemoryPackOrder(2)] private int _amount;

        [MemoryPackConstructor]
        private ItemRow()
        {
        }

        [MemoryPackIgnore] public string Id => _id;

        [MemoryPackIgnore] public EItemEffect Effect => _effect;

        [MemoryPackIgnore] public int Amount => _amount;

        public override string ToString() => "Item(" + _id + ")";
    }
}
