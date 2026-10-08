using System.Globalization;

namespace Pwrschdlr.Core;

/// <summary>A repeating schedule: what happens, at what local time, on which days of the week.</summary>
/// <param name="Id">Ties the scheduled task's launch to this schedule, so a task left over from an older one does nothing.</param>
/// <param name="Action">What happens when an occurrence comes.</param>
/// <param name="Time">The local time of day of every occurrence.</param>
/// <param name="Days">The days it happens on, at least one.</param>
public sealed record Repeat(Guid Id, PowerAction Action, TimeOnly Time, IReadOnlySet<DayOfWeek> Days)
{
    /// <summary>Monday to Friday.</summary>
    public static readonly IReadOnlySet<DayOfWeek> Weekdays = new HashSet<DayOfWeek>
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
    };

    /// <summary>Saturday and Sunday.</summary>
    public static readonly IReadOnlySet<DayOfWeek> Weekend = new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };

    public IReadOnlySet<DayOfWeek> Days { get; init; } = Days.Count > 0
        ? Days
        : throw new ArgumentException("A repeating schedule needs at least one day.", nameof(Days));

    public static Repeat Create(PowerAction action, TimeOnly time, IEnumerable<DayOfWeek> days) =>
        new(Guid.NewGuid(), action, time, days.ToHashSet());

    /// <summary>
    /// The first occurrence strictly after <paramref name="now"/>. A time daylight saving skips moves on by the
    /// skipped hour, and a time it happens twice is the first one, exactly as <see cref="Timing"/> does for a timer.
    /// </summary>
    public DateTimeOffset NextOccurrence(DateTimeOffset now, TimeZoneInfo zone) => NextOccurrence(Time, Days, now, zone);

    /// <inheritdoc cref="NextOccurrence(DateTimeOffset, TimeZoneInfo)"/>
    public static DateTimeOffset NextOccurrence(TimeOnly time, IReadOnlySet<DayOfWeek> days, DateTimeOffset now, TimeZoneInfo zone)
    {
        var date = Timing.LocalDate(now, zone);
        for (var day = 0; day <= 7; day++)
        {
            var current = date.AddDays(day);
            if (!days.Contains(current.DayOfWeek))
                continue;
            var occurrence = Timing.Resolve(current.ToDateTime(time), zone);
            if (occurrence > now)
                return occurrence;
        }

        throw new InvalidOperationException("A repeating schedule needs at least one day.");
    }

    /// <summary>
    /// The occurrence closest to <paramref name="now"/>: the one being waited for, or the one just gone. It tells a
    /// launch that came too late for its occurrence from one that is early for the next.
    /// </summary>
    public DateTimeOffset Nearest(DateTimeOffset now, TimeZoneInfo zone)
    {
        var date = Timing.LocalDate(now, zone);
        DateTimeOffset? nearest = null;
        for (var day = -7; day <= 7; day++)
        {
            var current = date.AddDays(day);
            if (!Days.Contains(current.DayOfWeek))
                continue;
            var occurrence = Timing.Resolve(current.ToDateTime(Time), zone);
            if (nearest is null || Distance(occurrence, now) < Distance(nearest.Value, now))
                nearest = occurrence;
        }

        return nearest ?? NextOccurrence(now, zone);
    }

    /// <summary>The days in words: "every day", "on weekdays", "on weekends" or "on Mon, Wed and Fri".</summary>
    public string DaysInWords(CultureInfo culture) => DaysInWords(Days, culture);

    /// <inheritdoc cref="DaysInWords(CultureInfo)"/>
    public static string DaysInWords(IReadOnlySet<DayOfWeek> days, CultureInfo culture)
    {
        if (days.Count == 7)
            return "every day";
        if (days.SetEquals(Weekdays))
            return "on weekdays";
        if (days.SetEquals(Weekend))
            return "on weekends";

        var names = InWeekOrder(days, culture).Select(day => culture.DateTimeFormat.AbbreviatedDayNames[(int)day]).ToList();
        return "on " + string.Join(", ", names.Take(names.Count - 1)) + (names.Count > 1 ? " and " : string.Empty) + names[^1];
    }

    private static TimeSpan Distance(DateTimeOffset one, DateTimeOffset other) =>
        one > other ? one - other : other - one;

    private static IEnumerable<DayOfWeek> InWeekOrder(IReadOnlySet<DayOfWeek> days, CultureInfo culture)
    {
        for (var day = 0; day < 7; day++)
        {
            var current = (DayOfWeek)(((int)culture.DateTimeFormat.FirstDayOfWeek + day) % 7);
            if (days.Contains(current))
                yield return current;
        }
    }
}
