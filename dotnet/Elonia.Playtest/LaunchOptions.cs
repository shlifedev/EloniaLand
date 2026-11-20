using System;
using System.Globalization;

namespace Elonia.Playtest;

internal sealed class LaunchOptions
{
    public int? SeedOrNull { get; }
    public string? DemoCommandsOrNull { get; }
    public string? ScreenshotPathOrNull { get; }
    public bool ShouldOpenDebugger { get; }

    public bool HasDemo => DemoCommandsOrNull != null || ScreenshotPathOrNull != null;

    private LaunchOptions(int? seedOrNull, string? demoCommandsOrNull, string? screenshotPathOrNull, bool bOpenDebugger)
    {
        SeedOrNull = seedOrNull;
        DemoCommandsOrNull = demoCommandsOrNull;
        ScreenshotPathOrNull = screenshotPathOrNull;
        ShouldOpenDebugger = bOpenDebugger;
    }

    public static LaunchOptions Parse(string[] args)
    {
        int? seedOrNull = null;
        string? demoCommandsOrNull = null;
        string? screenshotPathOrNull = null;
        bool bOpenDebugger = false;
        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--seed":
                    seedOrNull = int.Parse(ReadValue(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--demo":
                    demoCommandsOrNull = ReadValue(args, ref index);
                    break;
                case "--screenshot":
                    screenshotPathOrNull = ReadValue(args, ref index);
                    break;
                case "--debug":
                    bOpenDebugger = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[index]}'.");
            }
        }

        return new LaunchOptions(seedOrNull, demoCommandsOrNull, screenshotPathOrNull, bOpenDebugger);
    }

    private static string ReadValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Option '{args[index]}' needs a value.");
        }

        index++;
        return args[index];
    }
}
