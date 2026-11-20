#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elonia.Data
{
    public sealed partial class GameTables : global::HolySheet.ITableSet
    {
        public const string SchemaHash = "beb1d68525bb449ef0df5e02d96ca18d0cb52210c0a2d7ec54cb4218c08927a8";

        public const string Serializer = "memorypack";

        public const string ManifestFileName = "tables.manifest.json";

        public const string? PublicKey = null;

        public static global::HolySheet.TableSetDefinition Definition { get; } = new global::HolySheet.TableSetDefinition(Serializer, SchemaHash, ManifestFileName, PublicKey, new global::HolySheet.TableDefinition[]
        {
            new global::HolySheet.TableDefinition(ActorTable.TableName, ActorTable.SchemaHash, ActorTable.Decode),
            new global::HolySheet.TableDefinition(FloorTable.TableName, FloorTable.SchemaHash, FloorTable.Decode),
            new global::HolySheet.TableDefinition(ItemTable.TableName, ItemTable.SchemaHash, ItemTable.Decode),
            new global::HolySheet.TableDefinition(PropTable.TableName, PropTable.SchemaHash, PropTable.Decode),
            new global::HolySheet.TableDefinition(ThingTable.TableName, ThingTable.SchemaHash, ThingTable.Decode),
            new global::HolySheet.TableDefinition(TownLegendTable.TableName, TownLegendTable.SchemaHash, TownLegendTable.Decode),
        });

        private readonly global::HolySheet.ITable[] _tables;

        public global::HolySheet.TableManifest Manifest { get; }

        public IReadOnlyList<global::HolySheet.ITable> Tables => _tables;

        public IReadOnlyList<string> LoadedTables { get; }

        public ActorTable Actor { get; }

        public FloorTable Floor { get; }

        public ItemTable Item { get; }

        public PropTable Prop { get; }

        public ThingTable Thing { get; }

        public TownLegendTable TownLegend { get; }

        private GameTables(global::HolySheet.TableSetData data)
        {
            Manifest = data.Manifest;
            LoadedTables = data.LoadedTables;
            _tables = data.Tables.ToArray();
            Actor = (ActorTable)_tables[0];
            Floor = (FloorTable)_tables[1];
            Item = (ItemTable)_tables[2];
            Prop = (PropTable)_tables[3];
            Thing = (ThingTable)_tables[4];
            TownLegend = (TownLegendTable)_tables[5];
        }

        public static GameTables Load(global::HolySheet.TableSource source, global::HolySheet.TableLoadOptions? options = null)
        {
            return new GameTables(global::HolySheet.TableLoader.Load(Definition, source, options));
        }

        public static GameTables Load(string directory, global::HolySheet.TableLoadOptions? options = null)
        {
            return Load(global::HolySheet.TableSource.FromDirectory(directory), options);
        }

        public static async ValueTask<GameTables> LoadAsync(global::HolySheet.TableSource source, global::HolySheet.TableLoadOptions? options = null, CancellationToken cancellationToken = default)
        {
            return new GameTables(await global::HolySheet.TableLoader.LoadAsync(Definition, source, options, null, cancellationToken).ConfigureAwait(false));
        }

        public GameTables Reload(global::HolySheet.TableSource source, global::HolySheet.TableLoadOptions? options = null)
        {
            return new GameTables(global::HolySheet.TableLoader.Load(Definition, source, options, this));
        }

        public static global::HolySheet.TableWatcher<GameTables> Watch(string directory, global::HolySheet.TableWatchOptions? options = null)
        {
            return new global::HolySheet.TableWatcher<GameTables>(directory, ManifestFileName,
                (source, loadOptions, previous) => previous == null ? Load(source, loadOptions) : previous.Reload(source, loadOptions), options);
        }
    }
}
