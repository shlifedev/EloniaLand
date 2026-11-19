#nullable enable

using System;
using Elonia.DataManager.Runtime;

namespace Elonia.Data
{
    public sealed partial class GameTables
    {
        public const string SCHEMA_HASH = "a24c0978e5735f581076035078ca03663feead66eb9f968903616bb2448e1410";

        public ActorTable Actor { get; }
        public FloorTable Floor { get; }
        public ItemTable Item { get; }
        public PropTable Prop { get; }
        public ThingTable Thing { get; }
        public TownLegendTable TownLegend { get; }

        public GameTables(ActorTable actor, FloorTable floor, ItemTable item, PropTable prop, ThingTable thing, TownLegendTable townLegend)
        {
            Actor = actor;
            Floor = floor;
            Item = item;
            Prop = prop;
            Thing = thing;
            TownLegend = townLegend;
        }

        public static GameTables Load(string manifestText, Func<string, byte[]> readFile)
        {
            TableManifest manifest = TableManifest.Parse(manifestText);
            manifest.ThrowIfSchemaDiffers(SCHEMA_HASH);
            ActorTable actor = new ActorTable(TableFile.ReadRows<ActorRow>(manifest, ActorTable.TABLE_NAME, readFile));
            FloorTable floor = new FloorTable(TableFile.ReadRows<FloorRow>(manifest, FloorTable.TABLE_NAME, readFile));
            ItemTable item = new ItemTable(TableFile.ReadRows<ItemRow>(manifest, ItemTable.TABLE_NAME, readFile));
            PropTable prop = new PropTable(TableFile.ReadRows<PropRow>(manifest, PropTable.TABLE_NAME, readFile));
            ThingTable thing = new ThingTable(TableFile.ReadRows<ThingRow>(manifest, ThingTable.TABLE_NAME, readFile));
            TownLegendTable townLegend = new TownLegendTable(TableFile.ReadRows<TownLegendRow>(manifest, TownLegendTable.TABLE_NAME, readFile));
            return new GameTables(actor, floor, item, prop, thing, townLegend);
        }
    }
}
