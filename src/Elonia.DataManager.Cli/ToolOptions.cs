using System;
using System.Collections.Generic;

namespace Elonia.DataManager.Cli;

internal sealed class ToolOptions
{
    public const string DEFAULT_INPUT = "src/Elonia.Data/Sheets";
    public const string DEFAULT_CODE_OUTPUT = "src/Elonia.Data/Generated";
    public const string DEFAULT_DATA_OUTPUT = "src/Elonia.Data/Generated/Bytes";

    public static readonly string[] USAGE_LINES = new string[]
    {
        $"  --input <폴더>     xlsx 파일이 있는 폴더 (기본: {DEFAULT_INPUT})",
        $"  --code <폴더>      생성 코드를 쓸 폴더 (기본: {DEFAULT_CODE_OUTPUT})",
        $"  --data <폴더>      bytes와 매니페스트를 쓸 폴더 (기본: {DEFAULT_DATA_OUTPUT})",
        "  --namespace <이름> 생성 코드의 네임스페이스 (기본: Elonia.Data)",
        "  --class <이름>     모든 표를 묶는 클래스 이름 (기본: GameTables)",
    };

    public string Input { get; private set; } = DEFAULT_INPUT;
    public string CodeOutput { get; private set; } = DEFAULT_CODE_OUTPUT;
    public string DataOutput { get; private set; } = DEFAULT_DATA_OUTPUT;
    public TableBuildOptions Build { get; } = new TableBuildOptions();

    public static bool TryParse(IReadOnlyList<string> args, int startIndex, Func<string, string, bool>? tryReadExtraOrNull, out ToolOptions options, out string error)
    {
        options = new ToolOptions();
        error = "";
        for (int index = startIndex; index < args.Count; index += 2)
        {
            if (index + 1 >= args.Count)
            {
                error = $"'{args[index]}' 뒤에 값이 없습니다.";
                return false;
            }

            string value = args[index + 1];
            switch (args[index])
            {
                case "--input":
                    options.Input = value;
                    break;
                case "--code":
                    options.CodeOutput = value;
                    break;
                case "--data":
                    options.DataOutput = value;
                    break;
                case "--namespace":
                    options.Build.Namespace = value;
                    break;
                case "--class":
                    options.Build.TablesClassName = value;
                    break;
                default:
                    if (tryReadExtraOrNull == null || !tryReadExtraOrNull(args[index], value))
                    {
                        error = $"모르는 옵션입니다: {args[index]}";
                        return false;
                    }

                    break;
            }
        }

        return true;
    }

    public bool TryCreateWorkspace(out TableWorkspace? workspaceOrNull, out string error)
    {
        try
        {
            workspaceOrNull = new TableWorkspace(Input, CodeOutput, DataOutput, Build);
            error = "";
            return true;
        }
        catch (ArgumentException)
        {
            workspaceOrNull = null;
            error = "엑셀을 둔 폴더와 결과를 쓰는 폴더는 달라야 합니다.";
            return false;
        }
    }
}
