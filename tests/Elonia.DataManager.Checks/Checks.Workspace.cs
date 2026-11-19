using System;
using System.Collections.Generic;
using System.IO;
using Elonia.DataManager;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckWorkspace()
    {
        string root = Path.Combine(Path.GetTempPath(), "elonia-data-workspace-" + Guid.NewGuid().ToString("N"));
        string input = Path.Combine(root, "Sheets");
        string code = Path.Combine(root, "Generated");
        string data = Path.Combine(root, "Generated", "Bytes");
        Directory.CreateDirectory(input);
        try
        {
            CheckThrows<ArgumentException>(() => new TableWorkspace(input, input, data, Fixture.CreateOptions()), "The input folder cannot be an output folder.");

            string fixturePath = Path.Combine(input, Fixture.WORKBOOK_NAME);
            WriteXlsx(fixturePath, Fixture.CreateWorkbook());
            TableWorkspace workspace = new TableWorkspace(input, code, data, Fixture.CreateOptions());
            Check(workspace.ResultOrNull == null, "A new workspace has no result yet.");
            Check(workspace.HasInputChanged(), "A workspace that has not read its files yet reports a change.");

            TableBuildResult result = workspace.Refresh();
            Check(result.IsSuccess && workspace.ResultOrNull == result, "Refresh reads and validates the folder and keeps the result.");
            CheckEqual(Fixture.CreateWorkbook().Sheets.Count, result.Workbooks[0].Sheets.Count, "The result carries the sheets that were read, ignored ones included.");
            Check(!workspace.HasInputChanged(), "Nothing has changed right after a refresh.");
            Check(!Directory.Exists(code), "Refresh writes nothing.");

            File.WriteAllText(Path.Combine(input, "~$" + Fixture.WORKBOOK_NAME), "lock");
            Check(!workspace.HasInputChanged(), "An Excel lock file is not a change.");

            File.SetLastWriteTimeUtc(fixturePath, File.GetLastWriteTimeUtc(fixturePath).AddSeconds(5));
            Check(workspace.HasInputChanged(), "A file saved again is a change.");
            workspace.Refresh();
            Check(!workspace.HasInputChanged(), "A refresh takes in the change.");

            Sheet bad = new Sheet("Bad.xlsx", "Broken",
                new string?[][] { new string?[] { "Key<int>", "int" }, new string?[] { "Id", "Value" }, new string?[] { null, null }, new string?[] { "1", "x" }, new string?[] { null, "7" } });
            Sheet strange = new Sheet("Bad.xlsx", "A.B.C", new string?[][] { new string?[] { "int" }, new string?[] { "Value" } });
            string badPath = Path.Combine(input, "Bad.xlsx");
            WriteXlsx(badPath, new Workbook("Bad.xlsx", new Sheet[] { bad, strange }));
            Check(workspace.HasInputChanged(), "A new file is a change.");

            DiagnosticBag listDiagnostics = new DiagnosticBag();
            IReadOnlyList<WorkbookInfo> listed = new XlsxWorkbookSource(input).List(listDiagnostics);
            Check(listed.Count == 2 && !listDiagnostics.HasErrors, "목록은 잠금 파일을 건너뛰고 셀 값이나 시트 이름의 작성 규칙을 검사하지 않는다.");
            Check(listed[0].Name == "Bad.xlsx" && listed[0].SheetNames.Count == 2 && listed[0].SheetNames[1] == "A.B.C",
                "목록은 파일 이름순과 원본 탭 순서를 보존한다.");
            Check(!Directory.Exists(code), "목록 조회는 생성물을 쓰지 않는다.");

            Check(workspace.BuildOrNull() == null, "A build with errors returns nothing.");
            Check(!Directory.Exists(code), "A build with errors writes nothing.");
            TableBuildResult failed = workspace.ResultOrNull!;
            CheckEqual(2, failed.Diagnostics.ErrorCount, "The bad value and the bad sheet name are both errors.");
            CheckEqual(1, failed.Diagnostics.WarningCount, "The row without a key is a warning.");

            CellOrigin badCell = new CellOrigin("Bad.xlsx", "Broken", 4, 2);
            Diagnostic? found = workspace.FindDiagnosticOrNull(badCell);
            Check(found != null && found.Severity == ESeverity.Error && found.OriginOrNull!.Value.Equals(badCell), "A cell's problem is found by its position.");
            Diagnostic? warning = workspace.FindDiagnosticOrNull(new CellOrigin("Bad.xlsx", "Broken", 5, 1));
            Check(warning != null && warning.Severity == ESeverity.Warning, "A warning is found by its position too.");
            Check(workspace.FindDiagnosticOrNull(new CellOrigin("Bad.xlsx", "Broken", 4, 1)) == null, "A cell without a problem finds nothing.");
            Diagnostic sheetLevel = FindSheetLevel(failed.Diagnostics.Items);
            Check(sheetLevel.OriginOrNull == null && sheetLevel.Location == "Bad.xlsx / A.B.C", "A problem about a whole sheet has no cell position.");

            Workbook badWorkbook = FindWorkbook(failed, "Bad.xlsx");
            workspace.CountProblems(badWorkbook.Sheets[0], out int cellErrors, out int cellWarnings);
            CheckEqual("1,1", $"{cellErrors},{cellWarnings}", "A sheet counts the problems in its cells.");
            workspace.CountProblems(badWorkbook.Sheets[1], out int sheetErrors, out int sheetWarnings);
            CheckEqual("1,0", $"{sheetErrors},{sheetWarnings}", "A sheet counts the problems about the sheet itself.");
            workspace.CountProblems(FindWorkbook(failed, Fixture.WORKBOOK_NAME).Sheets[1], out int cleanErrors, out int cleanWarnings);
            CheckEqual("0,0", $"{cleanErrors},{cleanWarnings}", "A clean sheet has no problems.");

            File.Delete(badPath);
            Check(workspace.HasInputChanged(), "A deleted file is a change.");
            CommitReport? report = workspace.BuildOrNull();
            Check(report != null && report.Written.Count > 0 && File.Exists(Path.Combine(data, "Item.bytes")) && File.Exists(Path.Combine(code, "ItemRow.g.cs")),
                "A build without errors writes the code and the data.");
            CommitReport? again = workspace.BuildOrNull();
            Check(again != null && !again.HasChanges, "Building again changes nothing.");

            CheckWorkspaceSource(root);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void CheckWorkspaceSource(string root)
    {
        string code = Path.Combine(root, "MemoryOutput");
        string data = Path.Combine(code, "Bytes");
        Workbook workbook = Fixture.CreateWorkbook();
        MemoryWorkbookSource source = new MemoryWorkbookSource(workbook);
        TableWorkspace workspace = new TableWorkspace(source, code, data, Fixture.CreateOptions());

        Check(workspace.InputDirectory.Length == 0 && !workspace.HasInputChanged(), "주입한 원본은 입력 폴더 없이 사용할 수 있다.");
        TableBuildResult result = workspace.Refresh();
        Check(result.IsSuccess && result.Workbooks.Count == 1 && ReferenceEquals(result.Workbooks[0], workbook), "검증은 주입한 통합문서를 읽는다.");
        Check(!Directory.Exists(code), "메모리 원본의 검증은 출력 폴더를 만들지 않는다.");
        CommitReport? report = workspace.BuildOrNull();
        Check(report != null && report.HasChanges && File.Exists(Path.Combine(data, "Item.bytes"))
            && File.Exists(Path.Combine(code, "ItemRow.g.cs")), "주입한 원본으로 코드와 bytes를 함께 생성한다.");
        Check(!workspace.HasInputChanged(), "출력 파일이 바뀌어도 원본의 변경으로 처리하지 않는다.");
    }

    private static void WriteXlsx(string path, Workbook workbook)
    {
        using (FileStream file = File.Create(path))
        {
            SimpleXlsxWriter.Write(file, ToXlsxContents(workbook), 3);
        }
    }

    private static Workbook FindWorkbook(TableBuildResult result, string name)
    {
        for (int index = 0; index < result.Workbooks.Count; index++)
        {
            if (result.Workbooks[index].Name == name)
            {
                return result.Workbooks[index];
            }
        }

        throw new InvalidOperationException($"The result has no workbook '{name}'.");
    }

    private static Diagnostic FindSheetLevel(IReadOnlyList<Diagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            if (diagnostics[index].OriginOrNull == null)
            {
                return diagnostics[index];
            }
        }

        throw new InvalidOperationException("No sheet-level diagnostic was reported.");
    }
}
