using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pwrschdlr.Core;

/// <summary>What this PC can do, and doing it.</summary>
public static partial class Power
{
    private const uint WmSysCommand = 0x0112;
    private const nint ScMonitorPower = 0xF170;
    private const nint MonitorOff = 2;
    private const int SystemPowerCapabilitiesSize = 76;

    private static readonly Lazy<Capabilities> Current = new(Query);

    /// <summary>Sleep in S1 to S3, or Modern Standby. Hibernation must also be turned on.</summary>
    public static bool IsAvailable(PowerAction action) => action switch
    {
        PowerAction.Sleep => Current.Value.ClassicSleep || Current.Value.ModernStandby,
        PowerAction.Hibernate => Current.Value.Hibernate,
        _ => true,
    };

    /// <summary>Why an action isn't available, for the timer page.</summary>
    public static string? WhyUnavailable(PowerAction action) => IsAvailable(action) ? null : action switch
    {
        PowerAction.Sleep => "This PC can't sleep.",
        PowerAction.Hibernate => "Hibernation is turned off on this PC.",
        _ => null,
    };

    /// <summary>shutdown.exe's arguments for an action, or <see langword="null"/> for sleep, which it can't do.</summary>
    /// <param name="closeApps">Close apps without letting unsaved work hold up the shutdown, restart or sign-out.</param>
    public static string? ShutdownArguments(PowerAction action, bool closeApps) => action switch
    {
        PowerAction.ShutDown => closeApps ? "/s /f /t 0" : "/s /t 0",
        PowerAction.Restart => closeApps ? "/r /f /t 0" : "/r /t 0",
        PowerAction.SignOut => closeApps ? "/l /f" : "/l",
        PowerAction.Hibernate => "/h",
        PowerAction.Sleep => null,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    /// <returns>0, or the Win32 error code of the failure.</returns>
    /// <param name="window">A window of the calling app, which turns off the display on Modern Standby PCs.</param>
    public static async Task<int> RunAsync(PowerAction action, bool closeApps, nint window)
    {
        if (ShutdownArguments(action, closeApps) is not { } arguments)
            return Sleep(window);

        using var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        })!;
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    /// <summary>
    /// SetSuspendState only works on PCs with classic sleep. Modern Standby PCs have no API to enter standby: they enter
    /// it when the display turns off, so that's what Pwrschdlr does there, as Microsoft suggests.
    /// </summary>
    private static int Sleep(nint window)
    {
        if (!Current.Value.ClassicSleep)
        {
            SendMessageW(window, WmSysCommand, ScMonitorPower, MonitorOff);
            return 0;
        }

        return SetSuspendState(hibernate: false, force: false, wakeupEventsDisabled: false) ? 0 : Marshal.GetLastPInvokeError();
    }

    private static Capabilities Query()
    {
        var caps = new byte[SystemPowerCapabilitiesSize];
        if (!GetPwrCapabilities(caps))
            return new(ClassicSleep: false, ModernStandby: false, Hibernate: IsPwrHibernateAllowed());

        // SYSTEM_POWER_CAPABILITIES: one byte each for SystemS1 (3), SystemS2 (4), SystemS3 (5) and AoAc (20).
        return new(ClassicSleep: caps[3] != 0 || caps[4] != 0 || caps[5] != 0, ModernStandby: caps[20] != 0, Hibernate: IsPwrHibernateAllowed());
    }

    private sealed record Capabilities(bool ClassicSleep, bool ModernStandby, bool Hibernate);

    [LibraryImport("powrprof.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool GetPwrCapabilities([Out] byte[] capabilities);

    [LibraryImport("powrprof.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool IsPwrHibernateAllowed();

    [LibraryImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool SetSuspendState(
        [MarshalAs(UnmanagedType.U1)] bool hibernate,
        [MarshalAs(UnmanagedType.U1)] bool force,
        [MarshalAs(UnmanagedType.U1)] bool wakeupEventsDisabled);

    [LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
}
