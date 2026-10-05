using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Pwrschdlr.Core;
using Pwrschdlr.Core.Updates;

namespace Pwrschdlr.Services;

/// <summary>
/// User preferences under HKCU\Software\Pwrschdlr, where the uninstaller finds them too. The <c>Timer</c> subkey holds
/// the running timer.
/// </summary>
internal static class AppSettings
{
    public const string KeyPath = @"Software\Pwrschdlr";

    /// <summary>The warnings Settings offers, in seconds, with their labels.</summary>
    public static readonly (int Seconds, string Label)[] Warnings = [(30, "30 seconds"), (60, "1 minute"), (120, "2 minutes"), (300, "5 minutes")];

    /// <summary>Deletes every preference and the timer, for the uninstaller.</summary>
    public static void Clear() => Registry.CurrentUser.DeleteSubKeyTree(KeyPath, throwOnMissingSubKey: false);

    public static ElementTheme Theme
    {
        get => (ElementTheme)Read(nameof(Theme), (int)ElementTheme.Default) is var theme && Enum.IsDefined(theme) ? theme : ElementTheme.Default;
        set => Write(nameof(Theme), (int)value);
    }

    public static bool ShowWelcome
    {
        get => Read(nameof(ShowWelcome), 1) != 0;
        set => Write(nameof(ShowWelcome), value ? 1 : 0);
    }

    public static int WarningSeconds
    {
        get => Read(nameof(WarningSeconds), 60) is var seconds && Warnings.Any(warning => warning.Seconds == seconds) ? seconds : 60;
        set => Write(nameof(WarningSeconds), value);
    }

    public static string WarningLabel => Warnings.First(warning => warning.Seconds == WarningSeconds).Label;

    /// <summary>Shut down, restart and sign out without letting apps with unsaved work hold them up.</summary>
    public static bool CloseApps
    {
        get => Read(nameof(CloseApps), 0) != 0;
        set => Write(nameof(CloseApps), value ? 1 : 0);
    }

    // The last timer's choices, which the timer page starts with.

    public static PowerAction LastAction
    {
        get => (PowerAction)Read(nameof(LastAction), 0) is var action && Enum.IsDefined(action) ? action : PowerAction.ShutDown;
        set => Write(nameof(LastAction), (int)value);
    }

    /// <summary>Whether the last timer was set for a time of day rather than a delay.</summary>
    public static bool LastWasAtTime
    {
        get => Read(nameof(LastWasAtTime), 0) != 0;
        set => Write(nameof(LastWasAtTime), value ? 1 : 0);
    }

    public static TimeSpan LastDelay
    {
        get => TimeSpan.FromMinutes(Math.Clamp(Read(nameof(LastDelay), 60), 1, (int)Timing.LongestDelay.TotalMinutes));
        set => Write(nameof(LastDelay), (int)value.TotalMinutes);
    }

    public static TimeOnly LastTime
    {
        get => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(Math.Clamp(Read(nameof(LastTime), 23 * 60), 0, 24 * 60 - 1)));
        set => Write(nameof(LastTime), value.Hour * 60 + value.Minute);
    }

    /// <summary>Whether the background update check runs at startup. On by default.</summary>
    public static bool CheckForUpdatesAutomatically
    {
        get => Read(nameof(CheckForUpdatesAutomatically), 1) != 0;
        set => Write(nameof(CheckForUpdatesAutomatically), value ? 1 : 0);
    }

    /// <summary>When the update check last reached GitHub; written by the update service, never by the UI.</summary>
    public static DateTimeOffset? LastUpdateCheckUtc
    {
        get => ReadTime(nameof(LastUpdateCheckUtc));
        set => WriteTime(nameof(LastUpdateCheckUtc), value);
    }

    /// <summary>The same store, as the update service sees it.</summary>
    public static IUpdatePreferences UpdatePreferences { get; } = new UpdatePreferencesAdapter();

    private sealed class UpdatePreferencesAdapter : IUpdatePreferences
    {
        public bool CheckForUpdatesAutomatically
        {
            get => AppSettings.CheckForUpdatesAutomatically;
            set => AppSettings.CheckForUpdatesAutomatically = value;
        }

        public DateTimeOffset? LastUpdateCheckUtc
        {
            get => AppSettings.LastUpdateCheckUtc;
            set => AppSettings.LastUpdateCheckUtc = value;
        }
    }

    private static int Read(string name, int fallback)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(name) is int value ? value : fallback;
    }

    private static void Write(string name, int value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        key.SetValue(name, value, RegistryValueKind.DWord);
    }

    /// <summary>Times are QWORDs of Unix seconds.</summary>
    private static DateTimeOffset? ReadTime(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(name) is long seconds ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
    }

    private static void WriteTime(string name, DateTimeOffset? value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        if (value is { } time)
            key.SetValue(name, time.ToUnixTimeSeconds(), RegistryValueKind.QWord);
        else
            key.DeleteValue(name, throwOnMissingValue: false);
    }
}
