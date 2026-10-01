using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        AboutCard.Description = $"Version {App.Version} · MIT License · © 2026 Luka Stojiljkovic";
    }

    /// <summary>A running timer's task moves with the warning.</summary>
    private async void OnWarningChanged(object sender, SelectionChangedEventArgs e)
    {
        var seconds = AppSettings.Warnings[WarningBox.SelectedIndex].Seconds;
        if (seconds == AppSettings.WarningSeconds)
            return;

        var previous = AppSettings.WarningSeconds;
        AppSettings.WarningSeconds = seconds;
        if (await _window.Timer.RescheduleAsync())
            return;

        AppSettings.WarningSeconds = previous;
        WarningBox.SelectedIndex = Array.FindIndex(AppSettings.Warnings, warning => warning.Seconds == previous);
        _window.ShowSchedulerError("Couldn't change the warning");
    }

    private void OnCloseAppsToggled(object sender, RoutedEventArgs e) => AppSettings.CloseApps = CloseAppsToggle.IsOn;

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        AppSettings.Theme = (ElementTheme)ThemeBox.SelectedIndex;
        _window.ApplyTheme(AppSettings.Theme);
    }

    private void OnWelcomeToggled(object sender, RoutedEventArgs e) => AppSettings.ShowWelcome = WelcomeToggle.IsOn;
}
