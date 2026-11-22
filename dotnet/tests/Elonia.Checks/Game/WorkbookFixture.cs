using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Cysharp.Text;
using Elonia.Data;
using Elonia.Game;
using HolySheet;
using HolySheet.Core;
using HolySheet.Core.Config;
using HolySheet.Core.Sources;

namespace Elonia.Game.Checks;

internal static class WorkbookFixture
{
    public static GameContent CreateContent(GameTables original, int healing)
    {
        return CreateContent(new Dictionary<(string, string, string), string>
        {
            [("Item", "potion", "Amount")] = healing.ToString(CultureInfo.InvariantCulture),
            [("Item", "branch", "Amount")] = "2",
            [("TownLegend", "potion", "Symbol")] = original.TownLegend.Get("gold").Symbol,
            [("TownLegend", "gold", "Symbol")] = original.TownLegend.Get("potion").Symbol,
            [("Actor", "hero", "MaxHp")] = (healing + 5).ToString(CultureInfo.InvariantCulture),
            [("Thing", "spike_trap", "StepDamage")] = (healing + 1).ToString(CultureInfo.InvariantCulture)
        });
    }

    public static GameContent CreateContent(Dictionary<(string Sheet, string Id, string Field), string> cells)
    {
        Workbook workbook = XlsxReader.Read(Path.Combine(AppContext.BaseDirectory, "Sheets", "Elonia.xlsx"));
        List<Sheet> editedSheets = new List<Sheet>();
        foreach (Sheet sheet in workbook.Sheets)
        {
            string[][] grid = sheet.ToGrid();
            for (int row = 0; row < grid.Length; row++)
            {
                string id = grid[row][0];
                for (int column = 0; column < grid[row].Length; column++)
                {
                    string field = grid[1][column];
                    if (cells.TryGetValue((sheet.Name, id, field), out string? value))
                    {
                        grid[row][column] = value;
                    }
                }
            }

            editedSheets.Add(Sheet.FromGrid(workbook.Name, sheet.Name, grid));
        }

        HolySheetConfig config = HolySheetConfig.Load(Path.Combine(AppContext.BaseDirectory, HolySheetConfig.DefaultFileName));
        TableBuildOptions options = new TableBuildOptions
        {
            Sources = new IWorkbookSource[] { new MemoryWorkbookSource(new Workbook(workbook.Name, editedSheets)) }
        };
        BuildResult result = TableBuild.Run(config, options);
        if (result.Output == null)
        {
            throw new InvalidDataException(ZString.Join('\n', result.Diagnostics.Items));
        }

        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>(result.Output.DataFiles, StringComparer.Ordinal);
        files.Add(result.Output.ManifestFileName, Encoding.UTF8.GetBytes(result.Output.ManifestText));
        return new GameContent(GameTables.Load(TableSource.FromMemory(files)));
    }
}
