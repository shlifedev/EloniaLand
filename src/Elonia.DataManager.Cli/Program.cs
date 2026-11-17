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

    private const string DEFAULT_INPUT = "src/Elonia.Data/Sheets";
    private const string DEFAULT_CODE_OUTPUT = "src/Elonia.Data/Generated";
    private const string DEFAULT_DATA_OUTPUT = "src/Elonia.Data/Generated/Bytes";

    public static int Main(string[] args)
    {
        if (args.Length == 0 || (args[0] != BUILD_COMMAND && args[0] != CHECK_COMMAND))
        {
            PrintUsage();
            return EXIT_USAGE;
        }

        string input = DEFAULT_INPUT;
        string codeOutput = DEFAULT_CODE_OUTPUT;
        string dataOutput = DEFAULT_DATA_OUTPUT;
        TableBuildOptions options = new TableBuildOptions();
        for (int index = 1; index < args.Length; index++)
        {
            if (index + 1 >= args.Length)
            {
                Console.Error.WriteLine($"'{args[index]}' 뒤에 값이 없습니다.");
                return EXIT_USAGE;
            }

            string value = args[index + 1];
            switch (args[index])
            {
                case "--input":
                    input = value;
                    break;
                case "--code":
                    codeOutput = value;
                    break;
                case "--data":
                    dataOutput = value;
                    break;
                case "--namespace":
                    options.Namespace = value;
                    break;
                case "--class":
                    options.TablesClassName = value;
                    break;
                default:
                    Console.Error.WriteLine($"모르는 옵션입니다: {args[index]}");
                    PrintUsage();
                    return EXIT_USAGE;
            }

            index++;
        }

        string inputPath = Path.GetFullPath(input);
        if (inputPath == Path.GetFullPath(codeOutput) || inputPath == Path.GetFullPath(dataOutput))
        {
            Console.Error.WriteLine("엑셀을 둔 폴더와 결과를 쓰는 폴더는 달라야 합니다.");
            return EXIT_USAGE;
        }

        TableBuildResult result = TableBuild.Run(new XlsxWorkbookSource(input), options);
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
        if (args[0] == CHECK_COMMAND)
        {
            Console.WriteLine($"검증 통과: {summary}. (check는 아무것도 쓰지 않습니다)");
            return EXIT_SUCCESS;
        }

        try
        {
            CommitReport report = OutputCommitter.Commit(result.OutputOrNull!, codeOutput, dataOutput);
            for (int index = 0; index < report.Written.Count; index++)
            {
                Console.WriteLine($"  씀    {report.Written[index]}");
            }

            for (int index = 0; index < report.Deleted.Count; index++)
            {
                Console.WriteLine($"  지움  {report.Deleted[index]}");
            }

            string changes = report.HasChanges ? $"파일 {report.Written.Count}개를 쓰고 {report.Deleted.Count}개를 지웠습니다" : "바뀐 것이 없습니다";
            Console.WriteLine($"완료: {summary}. {changes}(그대로 {report.UnchangedCount}개).");
            return EXIT_SUCCESS;
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return EXIT_FAILED;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("사용법: elonia-data <build|check> [옵션]");
        Console.WriteLine("  build              엑셀을 읽어 생성 코드와 bytes를 만든다");
        Console.WriteLine("  check              읽고 검증만 한다(아무것도 쓰지 않는다)");
        Console.WriteLine($"  --input <폴더>     xlsx 파일이 있는 폴더 (기본: {DEFAULT_INPUT})");
        Console.WriteLine($"  --code <폴더>      생성 코드를 쓸 폴더 (기본: {DEFAULT_CODE_OUTPUT})");
        Console.WriteLine($"  --data <폴더>      bytes와 매니페스트를 쓸 폴더 (기본: {DEFAULT_DATA_OUTPUT})");
        Console.WriteLine("  --namespace <이름> 생성 코드의 네임스페이스 (기본: Elonia.Data)");
        Console.WriteLine("  --class <이름>     모든 표를 묶는 클래스 이름 (기본: GameTables)");
    }
}
