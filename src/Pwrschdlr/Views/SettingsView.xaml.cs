using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pwrschdlr.Core.Updates;
using Pwrschdlr.Services;

namespace Pwrschdlr.Views;

public sealed partial class SettingsView : UserControl
{
    private readonly MainWindow _window;

    public SettingsView(MainWindow window)
    {
        _window = window;
        InitializeComponent();

        foreach (var (_, label) in AppSettings.Warnings)
            WarningBox.Items.Add(label);
        WarningBox.SelectedIndex = Array.FindIndex(AppSettings.Warnings, warning => warning.Seconds == AppSettings.WarningSeconds);
        CloseAppsToggle.IsOn = AppSettings.CloseApps;
        ThemeBox.SelectedIndex = (int)AppSettings.Theme;
        WelcomeToggle.IsOn = AppSettings.ShowWelcome;
        AutoUpdateToggle.IsOn = AppSettings.CheckForUpdatesAutomatically;
        UpdateStatusText.Text = $"Version {App.Version}";
        AboutCard.Description = $"Version {App.Version} · MIT License · © 2026 Luka Stojiljkovic";
    }

    /// <summary>A running timer's task, and a repeating schedule's, move with the warning.</summary>
    private async void OnWarningChanged(object sender, SelectionChangedEventArgs e)
    {
        var seconds = AppSettings.Warnings[WarningBox.SelectedIndex].Seconds;
        if (seconds == AppSettings.WarningSeconds)
            return;

        var previous = AppSettings.WarningSeconds;
        AppSettings.WarningSeconds = seconds;
        if (await _window.Timer.RescheduleAsync() & await _window.Repeat.RescheduleAsync())
            return;

        AppSettings.WarningSeconds = previous;
        WarningBox.SelectedIndex = Array.FindIndex(AppSettings.Warnings, warning => warning.Seconds == previous);
        await _window.Timer.RescheduleAsync();
        await _window.Repeat.RescheduleAsync();
        _window.ShowSchedulerError("Couldn't change the warning", "the timer");
    }

    private void OnCloseAppsToggled(object sender, RoutedEventArgs e) => AppSettings.CloseApps = CloseAppsToggle.IsOn;

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        AppSettings.Theme = (ElementTheme)ThemeBox.SelectedIndex;
        _window.ApplyTheme(AppSettings.Theme);
    }

    private void OnWelcomeToggled(object sender, RoutedEventArgs e) => AppSettings.ShowWelcome = WelcomeToggle.IsOn;

    private void OnAutoUpdateToggled(object sender, RoutedEventArgs e) =>
        AppSettings.CheckForUpdatesAutomatically = AutoUpdateToggle.IsOn;

    private async void OnCheckNowClick(object sender, RoutedEventArgs e)
    {
        CheckNowButton.IsEnabled = false;
        UpdateStatusText.Text = "Checking…";
        try
        {
            var result = await _window.Updates.Service.CheckAsync(manual: true);
            UpdateStatusText.Text = result.Status switch
            {
                UpdateCheckStatus.UpToDate => "Pwrschdlr is up to date.",
                UpdateCheckStatus.UpdateAvailable => $"Pwrschdlr {result.Release!.Version.ToString(3)} is available.",
                UpdateCheckStatus.NoReleases => "No releases have been published yet.",
                UpdateCheckStatus.RateLimited => "GitHub's rate limit was reached. Try again later.",
                _ => $"Could not check for updates: {result.Detail}",
            };
            if (result is { Status: UpdateCheckStatus.UpdateAvailable, Release: { } release })
                _window.ShowUpdateAvailable(release);
        }
        finally
        {
            CheckNowButton.IsEnabled = true;
        }
    }
}
