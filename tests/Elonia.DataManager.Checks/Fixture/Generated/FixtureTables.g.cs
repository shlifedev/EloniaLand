#nullable enable

using System;
using Elonia.DataManager.Runtime;

namespace Elonia.Checks.Generated
{
    public sealed partial class FixtureTables
    {
        public const string SCHEMA_HASH = "a14a2d5368b40b1f2150869dfa1d0a61fab1f445b790a087b873d11c7ebf21ea";

        public AllTypesTable AllTypes { get; }
        public EmptyTable Empty { get; }
        public ItemTable Item { get; }
        public MonsterTable Monster { get; }
        public PhysicsTable Physics { get; }

        public FixtureTables(AllTypesTable allTypes, EmptyTable empty, ItemTable item, MonsterTable monster, PhysicsTable physics)
        {
            AllTypes = allTypes;
            Empty = empty;
            Item = item;
            Monster = monster;
            Physics = physics;
        }

        public static FixtureTables Load(string manifestText, Func<string, byte[]> readFile)
        {
            TableManifest manifest = TableManifest.Parse(manifestText);
            manifest.ThrowIfSchemaDiffers(SCHEMA_HASH);
            AllTypesTable allTypes = new AllTypesTable(TableFile.ReadRows<AllTypesRow>(manifest, AllTypesTable.TABLE_NAME, readFile));
            EmptyTable empty = new EmptyTable(TableFile.ReadRows<EmptyRow>(manifest, EmptyTable.TABLE_NAME, readFile));
            ItemTable item = new ItemTable(TableFile.ReadRows<ItemRow>(manifest, ItemTable.TABLE_NAME, readFile));
            MonsterTable monster = new MonsterTable(TableFile.ReadRows<MonsterRow>(manifest, MonsterTable.TABLE_NAME, readFile));
            PhysicsTable physics = new PhysicsTable(TableFile.ReadRows<PhysicsRow>(manifest, PhysicsTable.TABLE_NAME, readFile));
            return new FixtureTables(allTypes, empty, item, monster, physics);
        }
    }
}
