#if HAS_FIXTURE
using System;
using System.Collections.Generic;
using Elonia.Checks.Generated;
using Elonia.DataManager;
using Elonia.DataManager.Runtime;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckRoundTrip()
    {
        GeneratedOutput output = BuildFixture().OutputOrNull!;
        FixtureTables tables = LoadFixture(output);

        AllTypesRow full = tables.AllTypes.Get(1);
        CheckEqual(4000000000u, full.U, "uint");
        CheckEqual(-9000000000000000000L, full.L, "long");
        CheckEqual(18000000000000000000ul, full.UL, "ulong");
        CheckEqual((short)-32768, full.S, "short");
        CheckEqual((ushort)65535, full.US, "ushort");
        CheckEqual((byte)255, full.B, "byte");
        CheckEqual(1.5f, full.F, "float");
        CheckEqual(0.1, full.D, "double");
        CheckEqual(decimal.MaxValue, full.M, "decimal");
        CheckEqual(true, full.Flag, "bool");
        CheckEqual(" 앞뒤 공백 ", full.Text, "string keeps its spaces and Korean text");
        CheckEqual(new DateTime(2026, 1, 31, 13, 30, 0, DateTimeKind.Utc), full.When, "DateTime");
        CheckEqual(DateTimeKind.Utc, full.When.Kind, "DateTime is UTC");
        CheckEqual(new Guid(Fixture.GUID_TEXT), full.G, "Guid");
        CheckEqual("1,2,3", string.Join(",", full.Numbers), "int[]");
        CheckEqual("a,b,c", string.Join(",", full.Tags), "List<string> with trimmed elements");
        CheckEqual("0.5,1.5", string.Join(",", full.Rates), "float[]");
        CheckEqual("7", string.Join(",", full.Counts), "List<int>");
        CheckEqual("Potion,Gold", string.Join(",", full.Kinds), "enum array");
        CheckEqual(EAccess.Read | EAccess.Write, full.Access, "flags enum from a comma list");
        CheckEqual(EAccess.All, full.Access, "flags enum equals the combined member");
        CheckEqual(ESmall.Low, full.Small, "enum with a negative value");
        CheckEqual(EPlain.A, full.Plain, "enum key");

        AllTypesRow blank = tables.AllTypes.Get(2);
        Check(blank.U == 0 && blank.L == 0 && blank.UL == 0 && blank.S == 0 && blank.US == 0 && blank.B == 0, "blank integers are 0");
        Check(blank.F == 0.0f && blank.D == 0.0 && blank.M == 0m && !blank.Flag, "blank numbers and bools are zero and false");
        CheckEqual("", blank.Text, "blank string is empty, not null");
        CheckEqual(default(DateTime), blank.When, "blank DateTime is the default");
        CheckEqual(Guid.Empty, blank.G, "blank Guid is empty");
        Check(blank.Numbers.Length == 0 && blank.Tags.Count == 0 && blank.Rates.Length == 0 && blank.Counts.Count == 0 && blank.Kinds.Length == 0,
            "blank collections are empty, not null");
        CheckEqual(EAccess.None, blank.Access, "blank flags enum is 0");
        CheckEqual(ESmall.High, blank.Small, "blank enum is the member whose value is 0");

        CheckEqual(2, tables.AllTypes.Count, "row count");
        CheckEqual(2, tables.AllTypes.GetByPlain(EPlain.B).Id, "a second key gets its own lookup");
        Check(tables.AllTypes.Contains(1) && !tables.AllTypes.Contains(3), "Contains");
        Check(tables.AllTypes.TryGet(2, out AllTypesRow? found) && found.Id == 2, "TryGet finds a row");
        Check(!tables.AllTypes.TryGet(9, out AllTypesRow? _), "TryGet reports a missing row");
        CheckThrows<KeyNotFoundException>(() => tables.AllTypes.Get(9), "Get throws for a missing key");

        CheckEqual("potion,gold,branch", JoinItemIds(tables.Item.Rows), "rows keep the sheet order");
        ItemRow potion = tables.Item.Get("potion");
        Check(potion.Name == "Potion" && potion.Price == 5 && potion.Kind == EKind.Potion, "an Item row");
        CheckEqual(EKind.None, tables.Item.Get("branch").Kind, "a blank enum cell");
        CheckEqual(0, tables.Item.Get("gold").Price, "a blank int cell");

        MonsterRow slime = tables.Monster.Get(1001);
        Check(slime.Name == "슬라임" && slime.Slug == "slime" && slime.Hp == 6 && slime.Speed == 0.5f, "a Monster row with extension columns");
        CheckEqual("1,2,3", string.Join(",", slime.Level), "columns merged into an array");
        Check(slime.HasDrop && slime.DropKey == "potion" && slime.Drop.HasValue && slime.Drop.Key == "potion", "a reference with a target");
        CheckEqual("Potion", tables.Item.Get(slime.Drop).Name, "a reference resolves to its row");
        MonsterRow bat = tables.Monster.GetBySlug("bat");
        Check(bat.Id == 1002 && bat.Hp == 0 && !bat.HasDrop && !bat.Drop.HasValue, "a row found by its second key, with no extension values and no reference");
        Check(!tables.Item.TryGet(bat.Drop, out ItemRow? _), "a reference to nothing finds no row");
        CheckThrows<InvalidOperationException>(() => tables.Item.Get(bat.Drop), "Get throws for a reference to nothing");
        MonsterRow dragon = tables.Monster.Get(2001);
        Check(dragon.Name == "드래곤" && dragon.Hp == 99 && dragon.Drop.Key == "gold", "a row from the child sheet");
        CheckEqual(3, tables.Monster.Count, "parent rows and child rows are one table");
        CheckEqual(EMonsterId.Slime, (EMonsterId)slime.Id, "a Mapping enum's values are the keys");
        CheckEqual(2001, (int)EMonsterId.Dragon, "a Mapping enum covers child rows");

        CheckEqual(1, tables.Physics.Count, "a Config table has one row per value column");
        Check(tables.Physics.Rows[0].Gravity == 9.8f && tables.Physics.Rows[0].MaxSpeed == 30 && tables.Physics.Rows[0].IsEnabled, "a Config row");
        CheckEqual(0, tables.Empty.Count, "a table without rows loads as empty");
        CheckEqual((byte)10, (byte)EKind.Gold, "an enum keeps its underlying type and value");
    }

    private static void CheckLoadRejectsMismatch()
    {
        GeneratedOutput output = BuildFixture().OutputOrNull!;
        string manifestText = GetManifestText(output);
        CheckEqual(FixtureTables.SCHEMA_HASH, TableManifest.Parse(manifestText).SchemaHash, "The generated code and the manifest carry the same schema hash.");

        string otherSchema = manifestText.Replace(FixtureTables.SCHEMA_HASH, new string('0', FixtureTables.SCHEMA_HASH.Length));
        CheckThrows<TableLoadException>(() => FixtureTables.Load(otherSchema, (string name) => output.DataFiles[name]), "Data built for another schema is rejected.");

        CheckThrows<TableLoadException>(() => FixtureTables.Load(manifestText, (string name) =>
        {
            byte[] bytes = (byte[])output.DataFiles[name].Clone();
            bytes[bytes.Length - 1] ^= 0xFF;
            return bytes;
        }), "A data file that differs from the manifest is rejected.");

        CheckThrows<TableLoadException>(() => FixtureTables.Load(manifestText, (string name) => null!), "A data file that cannot be read is rejected.");

        string withoutItem = RemoveLine(manifestText, "table Item ");
        CheckThrows<TableLoadException>(() => FixtureTables.Load(withoutItem, (string name) => output.DataFiles[name]), "A manifest that lacks a table is rejected.");

        byte[] monsterBytes = output.DataFiles["Monster.bytes"];
        TableManifest original = TableManifest.Parse(manifestText);
        List<TableManifestEntry> entries = new List<TableManifestEntry>();
        for (int index = 0; index < original.Tables.Count; index++)
        {
            TableManifestEntry entry = original.Tables[index];
            entries.Add(entry.Name == "Item" ? new TableManifestEntry("Item", entry.RowCount, TableHash.Compute(monsterBytes)) : entry);
        }

        string swapped = new TableManifest(original.SchemaHash, entries).ToText();
        CheckThrows<TableLoadException>(() => FixtureTables.Load(swapped, (string name) => name == "Item.bytes" ? monsterBytes : output.DataFiles[name]),
            "Another table's bytes in place of a table are rejected.");
    }

    private static FixtureTables LoadFixture(GeneratedOutput output)
    {
        return FixtureTables.Load(GetManifestText(output), (string name) => output.DataFiles[name]);
    }

    private static string GetManifestText(GeneratedOutput output)
    {
        return System.Text.Encoding.UTF8.GetString(output.DataFiles[TableManifest.FILE_NAME]);
    }

    private static string RemoveLine(string text, string linePrefix)
    {
        string[] lines = text.Split('\n');
        List<string> kept = new List<string>();
        for (int index = 0; index < lines.Length; index++)
        {
            if (!lines[index].StartsWith(linePrefix, StringComparison.Ordinal))
            {
                kept.Add(lines[index]);
            }
        }

        return string.Join("\n", kept);
    }

    private static string JoinItemIds(IReadOnlyList<ItemRow> rows)
    {
        List<string> ids = new List<string>();
        for (int index = 0; index < rows.Count; index++)
        {
            ids.Add(rows[index].Id);
        }

        return string.Join(",", ids);
    }
}
#endif
