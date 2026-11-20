using Elonia.Game;

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
            PlayerCommandSymbols.GetCommand(commands[index]);
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

        return PlayerCommandSymbols.GetCommand(_commands[frame / _framesPerCommand]);
    }
}
