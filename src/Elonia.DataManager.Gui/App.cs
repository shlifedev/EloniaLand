using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Elonia.DataManager.Gui;

internal sealed class App : Application
{
    private readonly TableWorkspace _workspace;
    private readonly StartOptions _start;

    public App(TableWorkspace workspace, StartOptions start)
    {
        _workspace = workspace;
        _start = start;
    }

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Default;
        Styles.Add(new FluentTheme());

        Styles.Add(new StyleInclude(new Uri("avares://Elonia.DataManager.Gui"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(_workspace, _start);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
