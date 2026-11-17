using System;

namespace Elonia.Playtest;

internal sealed class DemoScript
{
    private const int SCREENSHOT_FRAMES_PER_COMMAND = 10;
    private const int FRAMES_BEFORE_SCREENSHOT = 14;

    private readonly string _commands;
    private readonly int _framesPerCommand;
    private readonly int _framesAfterCommands;
    private int _frame;

    public string? ScreenshotPathOrNull { get; }
    public bool IsFinished => _frame >= _commands.Length * _framesPerCommand + _framesAfterCommands;

    public DemoScript(string commands, string? screenshotPathOrNull)
    {
        for (int index = 0; index < commands.Length; index++)
        {
            ParseCommand(commands[index]);
        }

        _commands = commands;
        ScreenshotPathOrNull = screenshotPathOrNull;
        bool bScreenshot = screenshotPathOrNull != null;
        _framesPerCommand = bScreenshot ? SCREENSHOT_FRAMES_PER_COMMAND : 1;
        _framesAfterCommands = bScreenshot ? FRAMES_BEFORE_SCREENSHOT : 0;
    }

    public EPlayerCommand NextCommand()
    {
        int frame = _frame;
        _frame++;
        if (frame % _framesPerCommand != 0 || frame / _framesPerCommand >= _commands.Length)
        {
            return EPlayerCommand.None;
        }

        return ParseCommand(_commands[frame / _framesPerCommand]);
    }

    public static char? GetSymbolOrNull(EPlayerCommand command)
    {
        switch (command)
        {
            case EPlayerCommand.MoveUp:
                return 'w';
            case EPlayerCommand.MoveDown:
                return 's';
            case EPlayerCommand.MoveLeft:
                return 'a';
            case EPlayerCommand.MoveRight:
                return 'd';
            case EPlayerCommand.Wait:
                return '.';
            case EPlayerCommand.PickUp:
                return 'e';
            default:
                return null;
        }
    }

    private static EPlayerCommand ParseCommand(char symbol)
    {
        switch (symbol)
        {
            case 'w':
                return EPlayerCommand.MoveUp;
            case 's':
                return EPlayerCommand.MoveDown;
            case 'a':
                return EPlayerCommand.MoveLeft;
            case 'd':
                return EPlayerCommand.MoveRight;
            case '.':
                return EPlayerCommand.Wait;
            case 'e':
                return EPlayerCommand.PickUp;
            default:
                throw new ArgumentException($"Unknown demo command '{symbol}'.");
        }
    }
}
