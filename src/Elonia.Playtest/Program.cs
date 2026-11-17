using System;
using System.Numerics;
using Raylib_cs;

namespace Elonia.Playtest;

internal static class Program
{
    private const int SCREEN_WIDTH = 1280;
    private const int SCREEN_HEIGHT = 720;

    private const int DEMO_SEED = 1;

    private static readonly Color BACKGROUND = new Color(24, 20, 37, 255);

    public static void Main(string[] args)
    {
        LaunchOptions options = LaunchOptions.Parse(args);
        DemoScript? demoOrNull = options.HasDemo ? new DemoScript(options.DemoCommandsOrNull ?? "", options.ScreenshotPathOrNull) : null;
        int seed = options.SeedOrNull ?? (options.HasDemo ? DEMO_SEED : CreateRandomSeed());

        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.InitWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Elonia");
        Raylib.SetTargetFPS(60);

        SpriteAtlas atlas = new SpriteAtlas();
        UiFont font = new UiFont();
        ImGuiLayer imGui = new ImGuiLayer();
        try
        {
            Game game = new Game(atlas, font, seed, SCREEN_WIDTH, SCREEN_HEIGHT, options.ShouldOpenDebugger);
            PlayerInput input = new PlayerInput();

            while (!Raylib.WindowShouldClose())
            {
                float deltaTime = Raylib.GetFrameTime();
                EPlayerCommand command;
                if (demoOrNull != null && !demoOrNull.IsFinished)
                {
                    command = demoOrNull.NextCommand();
                }
                else
                {
                    command = input.Read(deltaTime);
                }

                if (command == EPlayerCommand.Restart)
                {
                    game = new Game(atlas, font, CreateRandomSeed(), SCREEN_WIDTH, SCREEN_HEIGHT, game.IsDebuggerOpen);
                }
                else if (command != EPlayerCommand.None)
                {
                    game.Execute(command);
                }
                else if ((demoOrNull == null || demoOrNull.IsFinished) && input.TryReadMapClick(out Vector2 clickPosition))
                {
                    game.ClickTile(clickPosition);
                }
                else if ((demoOrNull == null || demoOrNull.IsFinished) && input.TryReadMenuClick(out Vector2 menuPosition))
                {
                    game.OpenContextMenu(menuPosition);
                }

                game.Update(deltaTime);

                Raylib.BeginDrawing();
                Raylib.ClearBackground(BACKGROUND);
                imGui.Begin(deltaTime);
                game.Draw();
                imGui.End();
                Raylib.EndDrawing();

                if (demoOrNull != null && demoOrNull.IsFinished && demoOrNull.ScreenshotPathOrNull != null)
                {
                    SaveScreenshot(demoOrNull.ScreenshotPathOrNull);
                    break;
                }
            }
        }
        finally
        {
            imGui.Dispose();
            font.Dispose();
            atlas.Dispose();
            Raylib.CloseWindow();
        }
    }

    private static int CreateRandomSeed()
    {
        return Random.Shared.Next();
    }

    private static void SaveScreenshot(string path)
    {
        Image image = Raylib.LoadImageFromScreen();
        Raylib.ExportImage(image, path);
        Raylib.UnloadImage(image);
    }
}
