using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pwrschdlr.Core;
using Pwrschdlr.Dialogs;
using Pwrschdlr.Services;
using Pwrschdlr.Views;
using Windows.Graphics;

namespace Pwrschdlr;

public sealed partial class MainWindow : Window
{
    public static readonly TimeSpan Postponement = TimeSpan.FromMinutes(15);
    public const string PostponeLabel = "+15 minutes";

    private readonly bool _openedForWarning;
    private bool _warning;
    private bool _closing;

    /// <param name="openedForWarning">Started by the timer's task, rather than by the user.</param>
    public MainWindow(bool openedForWarning)
    {
        _openedForWarning = openedForWarning;
        InitializeComponent();
        Dialogs = new DialogService(Root);
        Timer = new TimerService(DispatcherQueue);
        Timer.Ticked += OnTicked;
        ConfigureWindow();
        ApplyTheme(AppSettings.Theme);
        NavView.SelectedItem = TimerItem;

        Root.Loaded += async (_, _) =>
        {
            if (_openedForWarning || !AppSettings.ShowWelcome)
                return;
            var welcome = new WelcomeDialog();
            await Dialogs.ShowAsync(welcome);
            if (welcome.DontShowAgain)
                AppSettings.ShowWelcome = false;
        };
    }

    internal DialogService Dialogs { get; }

    internal TimerService Timer { get; }

    private nint WindowHandle => Win32Interop.GetWindowFromWindowId(AppWindow.Id);

    internal static string When(DateTimeOffset target) => Timing.When(target, DateTimeOffset.Now, TimeZoneInfo.Local, CultureInfo.CurrentCulture);

    internal void ApplyTheme(ElementTheme theme)
    {
        Root.RequestedTheme = theme;
        AppWindow.TitleBar.PreferredTheme = theme switch
        {
            ElementTheme.Light => TitleBarTheme.Light,
            ElementTheme.Dark => TitleBarTheme.Dark,
            _ => TitleBarTheme.UseDefaultAppMode,
        };
    }

    /// <summary>For a second start of Pwrschdlr, and for the warning.</summary>
    internal void BringToFront()
    {
        if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
            presenter.Restore();
        Activate();
    }

    internal void ShowStatus(InfoBarSeverity severity, string title, string message)
    {
        StatusBar.Severity = severity;
        StatusBar.Title = title;
        StatusBar.Message = message;
        StatusBar.IsOpen = true;
    }

    internal void HideStatus() => StatusBar.IsOpen = false;

    private void ConfigureWindow()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Pwrschdlr.ico"));
        AppWindow.Closing += OnWindowClosing;

        var scale = GetDpiForWindow(WindowHandle) / 96.0;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = (int)(560 * scale);
            presenter.PreferredMinimumHeight = (int)(640 * scale);
        }

        var workArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var width = Math.Min((int)(940 * scale), workArea.Width);
        var height = Math.Min((int)(740 * scale), workArea.Height);
        AppWindow.MoveAndResize(new RectInt32(workArea.X + (workArea.Width - width) / 2, workArea.Y + (workArea.Height - height) / 2, width, height));
    }

    /// <summary>Shows the countdown in the taskbar, and starts the warning when it's time.</summary>
    private void OnTicked(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.Now;
        Title = Timer.Current is { } running ? $"{Timing.Countdown(running.Remaining(now))} · Pwrschdlr" : "Pwrschdlr";
        if (_warning || Timer.Current is not { } schedule)
            return;

        switch (schedule.PhaseAt(now, TimerService.Warning))
        {
            case TimerPhase.Warning:
                _ = WarnAsync(schedule);
                break;
            // The warning never started: the PC was off, asleep or signed out until after the end.
            case TimerPhase.Up or TimerPhase.Missed:
                _ = MissedAsync(schedule);
                break;
        }
    }

    private async Task WarnAsync(Schedule schedule)
    {
        _warning = true;
        var presenter = AppWindow.Presenter as OverlappedPresenter;
        try
        {
            BringToFront();
            // A window that the task started may not get to the front, so it stays on top until the warning ends.
            presenter?.IsAlwaysOnTop = true;
            var choice = await Dialogs.WarnAsync(schedule, WarningMessage(schedule), PostponeLabel);
            presenter?.IsAlwaysOnTop = false;
            switch (choice)
            {
                case WarningChoice.Cancel:
                    await Timer.CancelAsync();
                    break;
                case WarningChoice.Postpone:
                    if (!await Timer.PostponeAsync(Postponement))
                        ShowSchedulerError("Couldn't postpone the timer");
                    break;
                case WarningChoice.Missed:
                    await MissedAsync(schedule);
                    break;
                default:
                    await RunAsync(schedule);
                    break;
            }
        }
        finally
        {
            _warning = false;
        }
    }

    private async Task RunAsync(Schedule schedule)
    {
        var info = Actions.Get(schedule.Action);
        HideStatus();
        var error = await Timer.RunAsync(schedule.Action, WindowHandle);
        if (error == TimerService.DryRun)
            ShowStatus(InfoBarSeverity.Informational, "Debug build", $"A release build would {info.Name.ToLowerInvariant()} now.");
        else if (error != 0)
            ShowStatus(InfoBarSeverity.Error, $"{info.Name} didn't work", new Win32Exception(error).Message);
        // After sleep or hibernation, the user didn't open the window that greets them.
        else if (_openedForWarning && !info.ClosesApps)
            Close();
    }

    private async Task MissedAsync(Schedule schedule)
    {
        await Timer.CancelAsync();
        ShowStatus(
            InfoBarSeverity.Warning,
            "Missed timer",
            $"Your PC was off, asleep or signed out when the timer to {Actions.Get(schedule.Action).Name.ToLowerInvariant()} {When(schedule.Target)} ran out, so nothing happened.");
    }

    private static string WarningMessage(Schedule schedule)
    {
        var info = Actions.Get(schedule.Action);
        var at = $"{info.Future} at {Timing.Clock(schedule.Target, TimeZoneInfo.Local, CultureInfo.CurrentCulture)}.";
        if (!info.ClosesApps)
            return $"{at} Your apps stay open.";
        return AppSettings.CloseApps
            ? $"Save your work now. {at} Apps close without asking, so unsaved work is lost."
            : $"Save your work now. {at} Apps with unsaved work may ask you first.";
    }

    internal void ShowSchedulerError(string title) =>
        ShowStatus(InfoBarSeverity.Error, title, "Windows Task Scheduler didn't accept the timer. Try again, and restart your PC if it keeps happening.");

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        HideStatus();
        PageHost.Content = args.IsSettingsSelected ? new SettingsView(this) : new TimerView(this);
    }

    private void OnPaneToggleRequested(TitleBar sender, object args) => NavView.IsPaneOpen = !NavView.IsPaneOpen;

    /// <summary>Closing the window during the warning cancels the timer. At any other time, the timer keeps running.</summary>
    private async void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!_warning || _closing)
            return;

        args.Cancel = true;
        _closing = true;
        await Timer.CancelAsync();
        Close();
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hwnd);
}
