#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using MemoryPack;

namespace Elonia.Data
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class TownLegendRow
    {
        [MemoryPackInclude, MemoryPackOrder(0)] private string _id = "";
        [MemoryPackInclude, MemoryPackOrder(1)] private string _symbol = "";
        [MemoryPackInclude, MemoryPackOrder(2)] private string _floor = "";
        [MemoryPackInclude, MemoryPackOrder(3)] private bool _prop__has;
        [MemoryPackInclude, MemoryPackOrder(4)] private string _prop = "";
        [MemoryPackInclude, MemoryPackOrder(5)] private bool _actor__has;
        [MemoryPackInclude, MemoryPackOrder(6)] private string _actor = "";
        [MemoryPackInclude, MemoryPackOrder(7)] private bool _thing__has;
        [MemoryPackInclude, MemoryPackOrder(8)] private string _thing = "";
        [MemoryPackInclude, MemoryPackOrder(9)] private EHouseStyle _houseStyle;

        [MemoryPackConstructor]
        private TownLegendRow()
        {
        }

        [MemoryPackIgnore] public string Id => _id;

        [MemoryPackIgnore] public string Symbol => _symbol;

        [MemoryPackIgnore] public FloorRef Floor => new FloorRef(true, _floor);

        [MemoryPackIgnore] public PropRef Prop => new PropRef(_prop__has, _prop);

        [MemoryPackIgnore] public ActorRef Actor => new ActorRef(_actor__has, _actor);

        [MemoryPackIgnore] public ThingRef Thing => new ThingRef(_thing__has, _thing);

        [MemoryPackIgnore] public EHouseStyle HouseStyle => _houseStyle;

        public override string ToString() => "TownLegend(" + _id + ")";
    }
}
