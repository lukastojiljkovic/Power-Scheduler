using System.Globalization;

namespace Pwrschdlr.Core;

/// <summary>Turns what the user picks into an instant, and instants into words.</summary>
public static class Timing
{
    /// <summary>The longest delay the hour and minute fields allow. A time of day is at most a day ahead too.</summary>
    public static readonly TimeSpan LongestDelay = new(23, 59, 0);

    /// <summary>The next time the clock shows <paramref name="time"/>: today, or tomorrow once today's has passed.</summary>
    public static DateTimeOffset NextAt(TimeOnly time, DateTimeOffset now, TimeZoneInfo zone)
    {
        var today = LocalDate(now, zone);
        var candidate = Resolve(today.ToDateTime(time), zone);
        return candidate > now ? candidate : Resolve(today.AddDays(1).ToDateTime(time), zone);
    }

    /// <summary>The countdown: 1:05:09 with hours, 5:09 under an hour. It shows 0:00 only once the time is up.</summary>
    public static string Countdown(TimeSpan remaining)
    {
        var seconds = (long)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
        var (hours, minutes, rest) = (seconds / 3600, seconds / 60 % 60, seconds % 60);
        return hours > 0 ? $"{hours}:{minutes:00}:{rest:00}" : $"{minutes}:{rest:00}";
    }

    /// <summary>A duration in words, rounded up to whole minutes: "1 hour 30 minutes".</summary>
    public static string Words(TimeSpan span)
    {
        var minutes = (long)Math.Ceiling(Math.Max(0, span.TotalMinutes));
        return (minutes / 60, minutes % 60) switch
        {
            (0, 0) => "less than a minute",
            (0, var m) => Unit(m, "minute"),
            (var h, 0) => Unit(h, "hour"),
            (var h, var m) => $"{Unit(h, "hour")} {Unit(m, "minute")}",
        };
    }

    /// <summary>"today at 23:30", "tomorrow at 7:00 AM": the day in English, the time in the user's clock format.</summary>
    public static string When(DateTimeOffset target, DateTimeOffset now, TimeZoneInfo zone, CultureInfo culture)
    {
        var day = (LocalDate(target, zone).DayNumber - LocalDate(now, zone).DayNumber) switch
        {
            0 => "today",
            1 => "tomorrow",
            _ => "on " + TimeZoneInfo.ConvertTime(target, zone).ToString("dddd", CultureInfo.InvariantCulture),
        };
        return $"{day} at {Clock(target, zone, culture)}";
    }

    /// <summary>The time of day in the user's clock format, such as 23:30 or 11:30 PM.</summary>
    public static string Clock(DateTimeOffset time, TimeZoneInfo zone, CultureInfo culture) =>
        TimeZoneInfo.ConvertTime(time, zone).ToString("t", culture);

    private static DateOnly LocalDate(DateTimeOffset time, TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time, zone).DateTime);

    /// <summary>
    /// The instant a wall-clock time stands for. A time that a daylight saving change skips moves on by the gap, as a
    /// clock that wasn't changed would show it. A time that the change repeats is its first occurrence.
    /// </summary>
    private static DateTimeOffset Resolve(DateTime wall, TimeZoneInfo zone)
    {
        if (zone.IsAmbiguousTime(wall))
            return new DateTimeOffset(wall, zone.GetAmbiguousTimeOffsets(wall).Max());

        var before = wall;
        while (zone.IsInvalidTime(before))
            before = before.AddMinutes(-15);
        return new DateTimeOffset(wall, zone.GetUtcOffset(before));
    }

    private static string Unit(long count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";
}
