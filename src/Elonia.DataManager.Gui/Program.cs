using System;
using System.Globalization;
using Avalonia;
using Elonia.DataManager.Cli;

namespace Elonia.DataManager.Gui;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        StartOptions start = new StartOptions();
        Func<string, string, bool> tryReadExtra = (string name, string value) =>
        {
            switch (name)
            {
                case "--screenshot":
                    start.ScreenshotPathOrNull = value;
                    return true;
                case "--sheet":
                    start.SheetNameOrNull = value;
                    return true;
                case "--problem":
                    bool bParsed = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int problemNumber);
                    start.ProblemNumber = problemNumber;
                    return bParsed;
                default:
                    return false;
            }
        };

        if (!ToolOptions.TryParse(args, 0, tryReadExtra, out ToolOptions options, out string error)
            || !options.TryCreateWorkspace(out TableWorkspace? workspaceOrNull, out error))
        {
            Console.Error.WriteLine(error);
            Console.WriteLine("사용법: elonia-data-gui [옵션]");
            for (int index = 0; index < ToolOptions.USAGE_LINES.Length; index++)
            {
                Console.WriteLine(ToolOptions.USAGE_LINES[index]);
            }

            return 2;
        }

        TableWorkspace workspace = workspaceOrNull!;
        AppBuilder.Configure(() => new App(workspace, start))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(Array.Empty<string>());
        return 0;
    }
}

internal sealed class StartOptions
{
    public string? ScreenshotPathOrNull { get; set; }
    public string? SheetNameOrNull { get; set; }

    public int ProblemNumber { get; set; }
}
