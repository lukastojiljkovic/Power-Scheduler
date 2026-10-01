using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Pwrschdlr.Core;

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
}
