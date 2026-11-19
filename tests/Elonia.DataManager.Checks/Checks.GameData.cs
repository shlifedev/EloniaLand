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
        string manifestText = File.ReadAllText(Path.Combine(tablesDirectory, TableManifest.FILE_NAME));
        CheckEqual(File.ReadAllText(Path.Combine(dataDirectory, TableManifest.FILE_NAME)), manifestText,
            "실행 파일 옆의 매니페스트가 현재 게임 표의 파일 해시와 일치한다.");
        GameTables tables = GameTables.Load(manifestText,
            (string fileName) => File.ReadAllBytes(Path.Combine(tablesDirectory, fileName)));

        CheckEqual(FindTable(result, TownLegendTable.TABLE_NAME).Rows.Count, tables.TownLegend.Count,
            "현재 엑셀에 있는 마을 범례를 모두 읽는다.");
        foreach (TownLegendRow row in tables.TownLegend.Rows)
        {
            Check(row.Symbol.Length == 1 && tables.TownLegend.GetBySymbol(row.Symbol) == row,
                $"마을 범례 {row.Id}를 한 글자 기호로 찾을 수 있다.");
            Check(tables.Floor.TryGet(row.Floor, out _), $"마을 범례 {row.Id}에 바닥이 있다.");
        }
    }
}
