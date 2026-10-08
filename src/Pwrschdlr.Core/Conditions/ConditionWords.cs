using System.Globalization;

namespace Pwrschdlr.Core.Conditions;

/// <summary>The words conditions use, in one place, so the panel and the waiting screen agree.</summary>
public static class ConditionWords
{
    /// <summary>"your downloads finish", as it reads in "Your PC shuts down when ...".</summary>
    public const string DownloadsClause = "your downloads finish";

    /// <summary>"nobody uses the PC", as it reads in "Your PC shuts down when ...".</summary>
    public const string IdleClause = "nobody uses the PC";

    /// <summary>"Steam closes", as it reads in "Your PC shuts down when ...".</summary>
    public static string AppClause(string app) => $"{app} closes";

    /// <summary>The live download reading: "Downloading now: 12.4 MB/s", or "Downloading now: nothing".</summary>
    public static string DownloadReading(double? bytesPerSecond) => $"Downloading now: {Rate(bytesPerSecond)}";

    /// <summary>A rate in bytes per second: "12.4 MB/s", or "nothing" when no bytes are coming in.</summary>
    public static string Rate(double? bytesPerSecond) => bytesPerSecond switch
    {
        null or < 1 => "nothing",
        < 1024 => Format(bytesPerSecond.Value, 0, "B/s"),
        < 1024 * 1024 => Format(bytesPerSecond.Value / 1024, 1, "KB/s"),
        < 1024L * 1024 * 1024 => Format(bytesPerSecond.Value / (1024.0 * 1024), 1, "MB/s"),
        _ => Format(bytesPerSecond.Value / (1024.0 * 1024 * 1024), 1, "GB/s"),
    };

    /// <summary>How far the quiet period is: "Quiet for 2 of 5 minutes".</summary>
    public static string Quiet(TimeSpan elapsed, TimeSpan required) =>
        $"Quiet for {(long)Math.Max(0, elapsed.TotalMinutes)} of {(long)required.TotalMinutes} minutes";

    /// <summary>Sizes are the same wherever the PC runs: the app is in English.</summary>
    private static string Format(double size, int decimals, string unit) =>
        $"{size.ToString($"F{decimals}", CultureInfo.InvariantCulture)} {unit}";
}
