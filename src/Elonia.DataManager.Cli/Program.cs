using System;
using System.IO;

namespace Elonia.DataManager.Cli;

public static class Program
{
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_FAILED = 1;
    private const int EXIT_USAGE = 2;

    private const string BUILD_COMMAND = "build";
    private const string CHECK_COMMAND = "check";

    public static int Main(string[] args)
    {
        if (args.Length == 0 || (args[0] != BUILD_COMMAND && args[0] != CHECK_COMMAND))
        {
            PrintUsage();
            return EXIT_USAGE;
        }

        if (!ToolOptions.TryParse(args, 1, null, out ToolOptions options, out string error)
            || !options.TryCreateWorkspace(out TableWorkspace? workspaceOrNull, out error))
        {
            Console.Error.WriteLine(error);
            PrintUsage();
            return EXIT_USAGE;
        }

        TableWorkspace workspace = workspaceOrNull!;
        bool bBuild = args[0] == BUILD_COMMAND;
        CommitReport? reportOrNull = null;
        try
        {
            if (bBuild)
            {
                reportOrNull = workspace.BuildOrNull();
            }
            else
            {
                workspace.Refresh();
            }
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return EXIT_FAILED;
        }

        TableBuildResult result = workspace.ResultOrNull!;
        if (result.Diagnostics.Items.Count > 0)
        {
            Console.Write(DiagnosticFormatter.Format(result.Diagnostics.Items));
        }

        if (!result.IsSuccess)
        {
            Console.WriteLine($"실패: 오류 {result.Diagnostics.ErrorCount}개, 경고 {result.Diagnostics.WarningCount}개. 아무것도 쓰지 않았습니다.");
            return EXIT_FAILED;
        }

        int rowCount = 0;
        for (int index = 0; index < result.Tables.Count; index++)
        {
            rowCount += result.Tables[index].Rows.Count;
        }

        string summary = $"표 {result.Tables.Count}개, 행 {rowCount}개, enum {result.Enums.Count}개, 경고 {result.Diagnostics.WarningCount}개";
        if (reportOrNull == null)
        {
            Console.WriteLine($"검증 통과: {summary}. (check는 아무것도 쓰지 않습니다)");
            return EXIT_SUCCESS;
        }

        for (int index = 0; index < reportOrNull.Written.Count; index++)
        {
            Console.WriteLine($"  씀    {reportOrNull.Written[index]}");
        }

        for (int index = 0; index < reportOrNull.Deleted.Count; index++)
        {
            Console.WriteLine($"  지움  {reportOrNull.Deleted[index]}");
        }

        string changes = reportOrNull.HasChanges ? $"파일 {reportOrNull.Written.Count}개를 쓰고 {reportOrNull.Deleted.Count}개를 지웠습니다" : "바뀐 것이 없습니다";
        Console.WriteLine($"완료: {summary}. {changes}(그대로 {reportOrNull.UnchangedCount}개).");
        return EXIT_SUCCESS;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("사용법: elonia-data <build|check> [옵션]");
        Console.WriteLine("  build              엑셀을 읽어 생성 코드와 bytes를 만든다");
        Console.WriteLine("  check              읽고 검증만 한다(아무것도 쓰지 않는다)");
        for (int index = 0; index < ToolOptions.USAGE_LINES.Length; index++)
        {
            Console.WriteLine(ToolOptions.USAGE_LINES[index]);
        }
    }
}
