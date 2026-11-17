using System;
using System.Globalization;
using Elonia.DataManager.Cli;
using Raylib_cs;

namespace Elonia.DataManager.Gui;

public static class Program
{
    private const int WINDOW_WIDTH = 1280;
    private const int WINDOW_HEIGHT = 800;
    private const int MIN_WINDOW_WIDTH = 800;
    private const int MIN_WINDOW_HEIGHT = 500;

    private const int FRAMES_BEFORE_SCREENSHOT = 12;

    public static int Main(string[] args)
    {
        string? screenshotPathOrNull = null;
        string? sheetNameOrNull = null;
        int problemIndex = -1;
        Func<string, string, bool> tryReadExtra = (string name, string value) =>
        {
            switch (name)
            {
                case "--screenshot":
                    screenshotPathOrNull = value;
                    return true;
                case "--sheet":
                    sheetNameOrNull = value;
                    return true;
                case "--problem":
                    return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out problemIndex);
                default:
                    return false;
            }
        };

        if (!ToolOptions.TryParse(args, 0, tryReadExtra, out ToolOptions options, out string error)
            || !options.TryCreateWorkspace(out TableWorkspace? workspaceOrNull, out error))
        {
            Console.Error.WriteLine(error);
            PrintUsage();
            return 2;
        }

        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "Elonia Data");
        Raylib.SetWindowMinSize(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        Raylib.SetTargetFPS(60);

        GuiLayer gui = new GuiLayer();
        try
        {
            MainWindow window = new MainWindow(workspaceOrNull!, sheetNameOrNull);
            if (problemIndex > 0)
            {
                window.SelectProblem(problemIndex - 1);
            }

            int frame = 0;
            while (!Raylib.WindowShouldClose())
            {
                float deltaTime = Raylib.GetFrameTime();
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Palette.BACKGROUND);
                gui.Begin(deltaTime);
                window.Update(deltaTime);
                window.Draw(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
                gui.End();
                Raylib.EndDrawing();

                frame++;
                if (screenshotPathOrNull != null && frame >= FRAMES_BEFORE_SCREENSHOT)
                {
                    Image image = Raylib.LoadImageFromScreen();
                    Raylib.ExportImage(image, screenshotPathOrNull);
                    Raylib.UnloadImage(image);
                    break;
                }
            }
        }
        finally
        {
            gui.Dispose();
            Raylib.CloseWindow();
        }

        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("사용법: elonia-data-gui [옵션]");
        for (int index = 0; index < ToolOptions.USAGE_LINES.Length; index++)
        {
            Console.WriteLine(ToolOptions.USAGE_LINES[index]);
        }
    }
}
