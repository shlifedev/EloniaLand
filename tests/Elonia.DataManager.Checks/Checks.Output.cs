using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Elonia.DataManager;
using Elonia.DataManager.Runtime;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckManifest()
    {
        TableManifest manifest = new TableManifest("abc123", new TableManifestEntry[]
        {
            new TableManifestEntry("Monster", 3, "ffff"),
            new TableManifestEntry("Item", 12, "0000"),
        });
        string text = manifest.ToText();
        CheckEqual("elonia-tables 1\nschema abc123\ntable Item 12 0000\ntable Monster 3 ffff\n", text, "A manifest is plain text with tables in name order.");

        TableManifest parsed = TableManifest.Parse(text.Replace("\n", "\r\n"));
        CheckEqual("abc123", parsed.SchemaHash, "A parsed manifest keeps the schema hash; Windows line endings are accepted.");
        CheckEqual(12, parsed.GetTable("Item").RowCount, "A parsed manifest keeps the row counts.");
        CheckEqual("ffff", parsed.GetTable("Monster").FileHash, "A parsed manifest keeps the file hashes.");
        CheckThrows<TableLoadException>(() => parsed.GetTable("Ghost"), "A table the manifest does not list is rejected.");
        CheckThrows<TableLoadException>(() => parsed.ThrowIfSchemaDiffers("other"), "A different schema hash is rejected.");
        parsed.ThrowIfSchemaDiffers("abc123");
        CheckThrows<TableLoadException>(() => TableManifest.Parse(""), "An empty manifest is rejected.");
        CheckThrows<TableLoadException>(() => TableManifest.Parse("something else\n"), "A file that is not a manifest is rejected.");
        CheckThrows<TableLoadException>(() => TableManifest.Parse("elonia-tables 99\nschema x\n"), "A manifest of another format version is rejected.");
        CheckThrows<TableLoadException>(() => TableManifest.Parse("elonia-tables 1\nschema x\ntable Item many 00\n"), "A broken table line is rejected.");

        CheckEqual("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", TableHash.Compute("abc"), "The hash is SHA-256 in lowercase hex.");
    }

    private static void CheckCommitter()
    {
        string root = Path.Combine(Path.GetTempPath(), "elonia-data-checks-" + Guid.NewGuid().ToString("N"));
        string codeDirectory = Path.Combine(root, "Code");
        string dataDirectory = Path.Combine(root, "Code", "Bytes");
        try
        {
            GeneratedOutput output = BuildFixture().OutputOrNull!;
            int fileCount = output.CodeFiles.Count + output.DataFiles.Count;

            CommitReport first = OutputCommitter.Commit(output, codeDirectory, dataDirectory);
            CheckEqual(fileCount, first.Written.Count, "The first commit writes every file.");
            Check(File.Exists(Path.Combine(codeDirectory, "ItemRow.g.cs")) && File.Exists(Path.Combine(dataDirectory, "Item.bytes"))
                && File.Exists(Path.Combine(dataDirectory, TableManifest.FILE_NAME)), "Code, data and the manifest land in their folders.");
            byte[] codeBytes = File.ReadAllBytes(Path.Combine(codeDirectory, "ItemRow.g.cs"));
            Check(codeBytes[0] == (byte)'/' && Array.IndexOf(codeBytes, (byte)'\r') < 0, "Code is written without a byte order mark and with \\n line endings.");

            CommitReport second = OutputCommitter.Commit(output, codeDirectory, dataDirectory);
            Check(!second.HasChanges, "Committing the same output again changes nothing.");
            CheckEqual(fileCount, second.UnchangedCount, "Every file is recognized as unchanged.");

            File.WriteAllText(Path.Combine(codeDirectory, "OldRow.g.cs"), CodeWriter.GENERATED_MARKER + "\n// stale\n");
            File.WriteAllBytes(Path.Combine(dataDirectory, "Old.bytes"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(codeDirectory, "Hand.g.cs"), "// written by a person\n");
            File.WriteAllText(Path.Combine(codeDirectory, "ItemRow.Extra.cs"), "// a partial class written by a person\n");
            File.WriteAllText(Path.Combine(dataDirectory, "notes.txt"), "keep me");
            File.WriteAllText(Path.Combine(codeDirectory, "ItemRow.g.cs"), CodeWriter.GENERATED_MARKER + "\n// edited by hand\n");
            CommitReport third = OutputCommitter.Commit(output, codeDirectory, dataDirectory);
            CheckEqual("Old.bytes,OldRow.g.cs", JoinSorted(third.Deleted), "Stale generated files are deleted.");
            CheckEqual("ItemRow.g.cs", string.Join(",", third.Written), "A generated file that was edited by hand is written again.");
            Check(File.Exists(Path.Combine(codeDirectory, "Hand.g.cs")) && File.Exists(Path.Combine(codeDirectory, "ItemRow.Extra.cs"))
                && File.Exists(Path.Combine(dataDirectory, "notes.txt")), "Files the tool did not make are left alone.");

            GeneratedOutput changed = new GeneratedOutput();
            foreach (KeyValuePair<string, string> file in output.CodeFiles)
            {
                changed.CodeFiles.Add(file.Key, file.Value + "// changed\n");
            }

            foreach (KeyValuePair<string, byte[]> file in output.DataFiles)
            {
                changed.DataFiles.Add(file.Key, file.Value);
            }

            File.Delete(Path.Combine(dataDirectory, TableManifest.FILE_NAME));
            Directory.CreateDirectory(Path.Combine(dataDirectory, TableManifest.FILE_NAME));
            Dictionary<string, string> before = SnapshotFiles(root);
            CheckThrows<IOException>(() => OutputCommitter.Commit(changed, codeDirectory, dataDirectory), "A write that fails is reported.");
            Dictionary<string, string> after = SnapshotFiles(root);
            Check(AreSame(before, after), "After a failed commit every file is back to what it was.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static Dictionary<string, string> SnapshotFiles(string root)
    {
        Dictionary<string, string> snapshot = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] paths = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
        for (int index = 0; index < paths.Length; index++)
        {
            snapshot.Add(paths[index], Convert.ToBase64String(File.ReadAllBytes(paths[index])));
        }

        return snapshot;
    }

    private static bool AreSame(Dictionary<string, string> left, Dictionary<string, string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, string> pair in left)
        {
            if (!right.TryGetValue(pair.Key, out string? other) || other != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static string JoinSorted(List<string> names)
    {
        List<string> sorted = new List<string>(names);
        sorted.Sort(StringComparer.Ordinal);
        return string.Join(",", sorted);
    }

    private static void CheckXlsxFiles()
    {
        XlsxSheetContent content = new XlsxSheetContent("Cells");
        content.AddRow("머리글", null, "C1");
        content.AddRow(5, 1.5, true, false, new DateTime(2026, 1, 31), new XlsxFormula("A2*2", 10.0), " <글자 & 공백> ", -7L, 0.1);
        content.AddRow();
        content.AddRow(null, "B4");
        content.MergedRanges.Add("A1:B1");
        XlsxSheetContent second = new XlsxSheetContent("Monster.Col.Stats");
        second.AddRow("x");
        MemoryStream stream = new MemoryStream();
        SimpleXlsxWriter.Write(stream, new XlsxSheetContent[] { content, second }, 1);
        stream.Position = 0;
        Workbook workbook = XlsxWorkbookSource.Read(stream, "Cells.xlsx");
        CheckEqual("Cells,Monster.Col.Stats", workbook.Sheets[0].Name + "," + workbook.Sheets[1].Name, "Sheet names and their order are kept.");
        Sheet sheet = workbook.Sheets[0];
        CheckEqual("머리글", sheet.GetCell(0, 0).Text, "Korean text is read.");
        Check(sheet.GetCell(0, 1).IsBlank && sheet.GetCell(0, 2).Text == "C1", "A merged range holds its value in the first cell only; the cells after it keep their positions.");
        CheckEqual("5|1.5|true|false|2026-01-31|10| <글자 & 공백> |-7|0.1", JoinRow(sheet, 1, 9), "Numbers, booleans, dates, formula results and text are read as text.");
        Check(sheet.GetCell(2, 0).IsBlank && sheet.GetCell(3, 0).IsBlank, "A blank row and a blank cell stay blank.");
        CheckEqual("B4", sheet.GetCell(3, 1).Text, "A value after a blank row keeps its position.");
        CheckEqual("Cells.xlsx / Cells!B4", sheet.GetCell(3, 1).Origin.ToString(), "A cell from a file knows its address.");

        string directory = Path.Combine(Path.GetTempPath(), "elonia-data-xlsx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using (FileStream file = File.Create(Path.Combine(directory, Fixture.WORKBOOK_NAME)))
            {
                SimpleXlsxWriter.Write(file, ToXlsxContents(Fixture.CreateWorkbook()), 3);
            }

            File.WriteAllText(Path.Combine(directory, "~$" + Fixture.WORKBOOK_NAME), "lock");

            TableBuildResult fromFile = TableBuild.Run(new XlsxWorkbookSource(directory), Fixture.CreateOptions());
            Check(fromFile.IsSuccess, "The fixture written as an xlsx file builds." + Environment.NewLine + DiagnosticFormatter.Format(fromFile.Diagnostics.Items));
            if (fromFile.IsSuccess)
            {
                GeneratedOutput memory = BuildFixture().OutputOrNull!;
                bool bSame = true;
                foreach (KeyValuePair<string, byte[]> data in memory.DataFiles)
                {
                    bSame &= fromFile.OutputOrNull!.DataFiles[data.Key].AsSpan().SequenceEqual(data.Value);
                }

                foreach (KeyValuePair<string, string> code in memory.CodeFiles)
                {
                    bSame &= fromFile.OutputOrNull!.CodeFiles[code.Key] == code.Value;
                }

                Check(bSame, "Reading from an xlsx file gives byte-identical output to the in-memory fixture.");
            }

            File.WriteAllText(Path.Combine(directory, "Broken.xlsx"), "this is not a zip file");
            TableBuildResult broken = TableBuild.Run(new XlsxWorkbookSource(directory), Fixture.CreateOptions());
            Check(!broken.IsSuccess && broken.Diagnostics.Items[0].Location == "Broken.xlsx", "A broken xlsx file is reported by its name.");
        }
        finally
        {
            Directory.Delete(directory, true);
        }

        TableBuildResult missing = TableBuild.Run(new XlsxWorkbookSource(Path.Combine(directory, "nowhere")), Fixture.CreateOptions());
        Check(!missing.IsSuccess && missing.Diagnostics.Items[0].Reason.Contains("폴더가 없습니다"), "A missing input folder is an error.");
    }

    private static string JoinRow(Sheet sheet, int rowIndex, int columnCount)
    {
        StringBuilder builder = new StringBuilder();
        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            builder.Append(columnIndex == 0 ? "" : "|").Append(sheet.GetCell(rowIndex, columnIndex).Text);
        }

        return builder.ToString();
    }

    private static List<XlsxSheetContent> ToXlsxContents(Workbook workbook)
    {
        List<XlsxSheetContent> contents = new List<XlsxSheetContent>();
        for (int sheetIndex = 0; sheetIndex < workbook.Sheets.Count; sheetIndex++)
        {
            Sheet sheet = workbook.Sheets[sheetIndex];
            XlsxSheetContent content = new XlsxSheetContent(sheet.Name);
            for (int rowIndex = 0; rowIndex < sheet.RowCount; rowIndex++)
            {
                object?[] cells = new object?[sheet.ColumnCount];
                for (int columnIndex = 0; columnIndex < cells.Length; columnIndex++)
                {
                    cells[columnIndex] = ToXlsxValue(sheet.GetCell(rowIndex, columnIndex).Text);
                }

                content.Rows.Add(cells);
            }

            contents.Add(content);
        }

        return contents;
    }

    private static object? ToXlsxValue(string text)
    {
        if (text.Length == 0)
        {
            return null;
        }

        bool bNumber = double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double number);
        if (bNumber && CellText.FromValue(number) == text)
        {
            return number;
        }

        return text;
    }
}
