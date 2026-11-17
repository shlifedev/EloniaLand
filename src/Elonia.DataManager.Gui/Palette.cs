using Avalonia.Media;

namespace Elonia.DataManager.Gui;

internal static class Palette
{
    public static readonly IBrush ERROR_TEXT = new SolidColorBrush(Color.FromRgb(229, 72, 77));
    public static readonly IBrush WARNING_TEXT = new SolidColorBrush(Color.FromRgb(214, 158, 22));
    public static readonly IBrush GOOD_TEXT = new SolidColorBrush(Color.FromRgb(63, 164, 91));
    public static readonly IBrush MUTED_TEXT = new SolidColorBrush(Color.FromRgb(128, 132, 140));

    public static readonly IBrush ERROR_CELL = new SolidColorBrush(Color.FromArgb(150, 214, 55, 55));
    public static readonly IBrush WARNING_CELL = new SolidColorBrush(Color.FromArgb(150, 222, 170, 30));
    public static readonly IBrush HEADER_CELL = new SolidColorBrush(Color.FromArgb(48, 90, 140, 230));
    public static readonly IBrush NO_CELL = Brushes.Transparent;

    public const double MUTED_OPACITY = 0.42;
}
