using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Text;
using ZLinq;

namespace Elonia.DataManager.Cli;

public static class Program
{
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_FAILED = 1;
    private const int EXIT_USAGE = 2;

    private const string LIST_COMMAND = "list";
    private const string BUILD_COMMAND = "build";
    private const string CHECK_COMMAND = "check";

    public static int Main(string[] args)
    {
        if (args.Length == 0 || (args[0] != BUILD_COMMAND && args[0] != CHECK_COMMAND && args[0] != LIST_COMMAND))
        {
            PrintUsage();
            return EXIT_USAGE;
        }

        if (!ToolOptions.TryParse(args, out ToolOptions options, out string error))
        {
            Console.Error.WriteLine(error);
            PrintUsage();
            return EXIT_USAGE;
        }

        TableBuildResult result;
        CommitReport? reportOrNull = null;
        try
        {
            if (args[0] == LIST_COMMAND)
            {
                return ListWorkbooks(options);
            }
            if (!options.TryCreateWorkspace(out TableWorkspace? workspaceOrNull, out error))
            {
                Console.Error.WriteLine(error);
                PrintUsage();
                return EXIT_USAGE;
            }

            TableWorkspace workspace = workspaceOrNull!;
            if (args[0] == BUILD_COMMAND)
            {
                reportOrNull = workspace.BuildOrNull();
            }
            else
            {
                workspace.Refresh();
            }
            result = workspace.ResultOrNull!;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Console.Error.WriteLine(exception.Message);
            return EXIT_FAILED;
        }

        if (options.IsJson)
        {
            WorkbookInfo[] workbooks = result.Workbooks.AsValueEnumerable().Select(static book => WorkbookInfo.FromWorkbook(book)).ToArray();
            CliResult.WriteJson(workbooks, result.Diagnostics, reportOrNull, options.Input);
            return result.IsSuccess ? EXIT_SUCCESS : EXIT_FAILED;
        }
        if (result.Diagnostics.Items.Count > 0)
        {
            Console.Write(DiagnosticFormatter.Format(result.Diagnostics.Items));
        }

        if (!result.IsSuccess)
        {
            Console.WriteLine(ZString.Format("실패: 오류 {0}개, 경고 {1}개. 아무것도 쓰지 않았습니다.", result.Diagnostics.ErrorCount, result.Diagnostics.WarningCount));
            return EXIT_FAILED;
        }

        int rowCount = result.Tables.AsValueEnumerable().Sum(static table => table.Rows.Count);
        string summary = ZString.Format("표 {0}개, 행 {1}개, enum {2}개, 경고 {3}개", result.Tables.Count, rowCount, result.Enums.Count, result.Diagnostics.WarningCount);
        if (reportOrNull == null)
        {
            Console.WriteLine(ZString.Concat("검증 통과: ", summary, ". (check는 아무것도 쓰지 않습니다)"));
            return EXIT_SUCCESS;
        }

        for (int index = 0; index < reportOrNull.Written.Count; index++)
        {
            Console.WriteLine(ZString.Concat("  씀    ", reportOrNull.Written[index]));
        }

        for (int index = 0; index < reportOrNull.Deleted.Count; index++)
        {
            Console.WriteLine(ZString.Concat("  지움  ", reportOrNull.Deleted[index]));
        }

        string changes = reportOrNull.HasChanges ? ZString.Format("파일 {0}개를 쓰고 {1}개를 지웠습니다", reportOrNull.Written.Count, reportOrNull.Deleted.Count) : "바뀐 것이 없습니다";
        Console.WriteLine(ZString.Format("완료: {0}. {1}(그대로 {2}개).", summary, changes, reportOrNull.UnchangedCount));
        return EXIT_SUCCESS;
    }

    private static int ListWorkbooks(ToolOptions options)
    {
        DiagnosticBag diagnostics = new DiagnosticBag();
        IReadOnlyList<WorkbookInfo> workbooks = new XlsxWorkbookSource(options.Input).List(diagnostics);
        if (options.IsJson)
        {
            CliResult.WriteJson(workbooks, diagnostics, null, options.Input);
        }
        else
        {
            foreach (WorkbookInfo book in workbooks)
            {
                Console.WriteLine(book.Name);
                foreach (string sheet in book.SheetNames)
                {
                    Console.WriteLine(ZString.Format("  {0} ({1})", sheet, SheetRole.FromName(sheet).Kind));
                }
            }
            Console.Write(DiagnosticFormatter.Format(diagnostics.Items));
        }
        return diagnostics.HasErrors ? EXIT_FAILED : EXIT_SUCCESS;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("사용법: elonia-data <list|check|build> [옵션]");
        Console.WriteLine("  list               스프레드시트와 시트 이름·종류를 나열한다");
        Console.WriteLine("  build              엑셀을 읽어 생성 코드와 bytes를 만든다");
        Console.WriteLine("  check              읽고 검증만 한다(아무것도 쓰지 않는다)");
        for (int index = 0; index < ToolOptions.USAGE_LINES.Length; index++)
        {
            Console.WriteLine(ToolOptions.USAGE_LINES[index]);
        }
    }
}
