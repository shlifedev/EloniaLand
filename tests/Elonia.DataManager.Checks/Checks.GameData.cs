using System;
using System.Collections.Generic;
using System.IO;
using Elonia.Data;
using Elonia.DataManager;
using Elonia.DataManager.Runtime;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckGameData()
    {
        string dataProject = Path.GetFullPath(Path.Combine(GetProjectDirectory(), "..", "..", "src", "Elonia.Data"));
        string codeDirectory = Path.Combine(dataProject, "Generated");
        string dataDirectory = Path.Combine(codeDirectory, "Bytes");

        TableBuildResult result = TableBuild.Run(new XlsxWorkbookSource(Path.Combine(dataProject, "Sheets")), new TableBuildOptions());
        Check(result.IsSuccess, "The game sheets build without errors." + Environment.NewLine + DiagnosticFormatter.Format(result.Diagnostics.Items));
        if (!result.IsSuccess)
        {
            return;
        }

        const string HINT = " Run the data manager (build) after editing the sheets.";
        GeneratedOutput output = result.OutputOrNull!;
        foreach (KeyValuePair<string, string> file in output.CodeFiles)
        {
            string path = Path.Combine(codeDirectory, file.Key);
            Check(File.Exists(path) && IsSameCode(File.ReadAllText(path), file.Value), $"The generated game code {file.Key} matches the sheets." + HINT);
        }

        foreach (KeyValuePair<string, byte[]> file in output.DataFiles)
        {
            string path = Path.Combine(dataDirectory, file.Key);
            Check(File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(file.Value), $"The game data file {file.Key} matches the sheets." + HINT);
        }

        CheckEqual(output.CodeFiles.Count, Directory.GetFiles(codeDirectory, "*.g.cs").Length, "The game code folder holds no stale generated files.");
        CheckEqual(output.DataFiles.Count - 1, Directory.GetFiles(dataDirectory, "*" + TableManifest.DATA_EXTENSION).Length, "The game data folder holds no stale data files.");

        string tablesDirectory = Path.Combine(AppContext.BaseDirectory, "Tables");
        GameTables tables = GameTables.Load(File.ReadAllText(Path.Combine(tablesDirectory, TableManifest.FILE_NAME)),
            (string fileName) => File.ReadAllBytes(Path.Combine(tablesDirectory, fileName)));
        CheckEqual(GameTables.SCHEMA_HASH, TableManifest.Parse(File.ReadAllText(Path.Combine(tablesDirectory, TableManifest.FILE_NAME))).SchemaHash,
            "The copied data and the compiled code are from the same build.");

        ItemRow potion = tables.Item.Get("potion");
        Check(potion.Name == "Potion" && potion.Effect == EItemEffect.Heal && potion.Amount == 5, "The potion heals 5.");
        ActorRow bat = tables.Actor.Get("bat");
        Check(bat.MaxHp == 3 && bat.Attack == 1 && bat.Speed == 140 && bat.IsHostile && bat.SenseRange == 7, "The bat's numbers are read.");
        Check(!tables.Actor.Get("villager").IsHostile, "The villager is not hostile.");
        TileRow tree = tables.Tile.Get("tree");
        Check(!tree.IsWalkable && tree.DropAmount == 1, "A tree blocks movement and drops one item.");
        CheckEqual("Grass", tables.Tile.Get(tree.HarvestResult).Name, "A harvested tree turns into grass (a reference to its own table).");
        CheckEqual("Branch", tables.Item.Get(tree.DropItem).Name, "A harvested tree drops a branch (a reference to another table).");
        Check(!tables.Tile.Get("grass").HarvestResult.HasValue, "Grass cannot be harvested.");
        CheckEqual(10, tables.Tile.Count, "All tiles are loaded.");
    }
}
