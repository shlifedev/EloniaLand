#nullable enable

using System;
using Elonia.DataManager.Runtime;

namespace Elonia.Data
{
    public sealed partial class GameTables
    {
        public const string SCHEMA_HASH = "9d928411494ebc689e7dfb6ef42e07e83a68c99e0fc17e901e7f595e11a2096e";

        public ActorTable Actor { get; }
        public ItemTable Item { get; }
        public TileTable Tile { get; }

        public GameTables(ActorTable actor, ItemTable item, TileTable tile)
        {
            Actor = actor;
            Item = item;
            Tile = tile;
        }

        public static GameTables Load(string manifestText, Func<string, byte[]> readFile)
        {
            TableManifest manifest = TableManifest.Parse(manifestText);
            manifest.ThrowIfSchemaDiffers(SCHEMA_HASH);
            ActorTable actor = new ActorTable(TableFile.ReadRows<ActorRow>(manifest, ActorTable.TABLE_NAME, readFile));
            ItemTable item = new ItemTable(TableFile.ReadRows<ItemRow>(manifest, ItemTable.TABLE_NAME, readFile));
            TileTable tile = new TileTable(TableFile.ReadRows<TileRow>(manifest, TileTable.TABLE_NAME, readFile));
            return new GameTables(actor, item, tile);
        }
    }
}
