using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Pwrschdlr.Services;

namespace Pwrschdlr;

public partial class App : Application
{
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();

        // An error in an event handler shouldn't take the window, or the timer it shows, down with it.
        UnhandledException += (_, e) =>
        {
            if (_window is null)
                return;
            e.Handled = true;
            _window.ShowStatus(InfoBarSeverity.Error, "Something went wrong", e.Message);
        };
    }

    public static string Version { get; } = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.1.1";

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // The scheduled task starts Pwrschdlr for the warning. A task left over from an older timer does nothing.
        var due = DueTimer(Environment.CommandLine);
        if (due is { } id && TimerService.Saved?.Id != id)
        {
            Exit();
            return;
        }

        var window = _window = new MainWindow(openedForWarning: due is not null);
        // A second start, including the task's start while the window is open, brings the window to the front. The
        // window's own countdown shows the warning.
        AppInstance.GetCurrent().Activated += (_, _) => window.DispatcherQueue.TryEnqueue(window.BringToFront);
        window.Activate();
    }

    private static Guid? DueTimer(string commandLine) =>
        Regex.Match(commandLine, @"--due\s+(?<id>[0-9a-fA-F-]{36})") is { Success: true } match && Guid.TryParse(match.Groups["id"].Value, out var id) ? id : null;
}
