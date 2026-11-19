using System;
using System.Collections.Generic;
using System.IO;
using Elonia.Data;
using Elonia.DataManager;
using Elonia.Playtest;

namespace Elonia.Checks;

internal static class WorkbookFixture
{
    public static GameContent CreateContent(GameTables original, int healing)
    {
        string workbookName = "Elonia.xlsx";
        using FileStream source = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Sheets", workbookName));
        Workbook workbook = XlsxWorkbookSource.Read(source, workbookName);
        List<XlsxSheetContent> editedSheets = new List<XlsxSheetContent>();
        foreach (Sheet sheet in workbook.Sheets)
        {
            XlsxSheetContent edited = new XlsxSheetContent(sheet.Name);
            for (int row = 0; row < sheet.RowCount; row++)
            {
                string id = sheet.GetCell(row, 0).Text;
                object?[] cells = new object?[sheet.ColumnCount];
                for (int column = 0; column < cells.Length; column++)
                {
                    string field = sheet.GetCell(1, column).Text;
                    string value = sheet.GetCell(row, column).Text;
                    cells[column] = (sheet.Name, id, field) switch
                    {
                        ("Item", "potion", "Amount") => healing,
                        ("TownLegend", "potion", "Symbol") => original.TownLegend.Get("gold").Symbol,
                        ("TownLegend", "gold", "Symbol") => original.TownLegend.Get("potion").Symbol,
                        ("Actor", "hero", "MaxHp") => healing + 5,
                        ("Thing", "spike_trap", "StepDamage") => healing + 1,
                        _ => value
                    };
                }

                edited.AddRow(cells);
            }

            editedSheets.Add(edited);
        }

        DirectoryInfo temp = Directory.CreateTempSubdirectory("elonia-playtest-");
        try
        {
            using (FileStream file = File.Create(Path.Combine(temp.FullName, workbookName)))
            {
                SimpleXlsxWriter.Write(file, editedSheets, 3);
            }

            TableBuildResult result = TableBuild.Run(new XlsxWorkbookSource(temp.FullName), new TableBuildOptions());
            if (!result.IsSuccess)
            {
                throw new InvalidDataException(DiagnosticFormatter.Format(result.Diagnostics.Items));
            }

            foreach (KeyValuePair<string, byte[]> pair in result.OutputOrNull!.DataFiles)
            {
                File.WriteAllBytes(Path.Combine(temp.FullName, pair.Key), pair.Value);
            }

            return GameContent.Load(temp.FullName);
        }
        finally
        {
            temp.Delete(true);
        }
    }
}
