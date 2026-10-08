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

    public static string Version { get; } = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "2.0.0";

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // The scheduled tasks start Pwrschdlr for a warning. A task left over from an older timer or schedule does
        // nothing.
        var due = IdArgument(Environment.CommandLine, "--due");
        var repeat = IdArgument(Environment.CommandLine, "--repeat");
        if (due is { } id && TimerService.Saved?.Id != id)
        {
            Exit();
            return;
        }
        if (repeat is { } schedule && RepeatService.Saved?.Id != schedule)
        {
            Exit();
            return;
        }

        var window = _window = new MainWindow(openedForWarning: due is not null, openedForRepeat: repeat is not null);
        // A second start, including the task's start while the window is open, brings the window to the front. The
        // window's own countdown shows the warning.
        AppInstance.GetCurrent().Activated += (_, _) => window.DispatcherQueue.TryEnqueue(window.BringToFront);
        window.Activate();
    }

    /// <summary>The id of a <c>--due</c> or <c>--repeat</c> argument, which one of the tasks put on the command line.</summary>
    private static Guid? IdArgument(string commandLine, string option) =>
        Regex.Match(commandLine, $@"{option}\s+(?<id>[0-9a-fA-F-]{{36}})") is { Success: true } match && Guid.TryParse(match.Groups["id"].Value, out var id) ? id : null;
}
