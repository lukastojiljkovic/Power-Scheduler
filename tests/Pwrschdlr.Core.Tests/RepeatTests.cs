using System.Globalization;

namespace Pwrschdlr.Core.Tests;

public class RepeatTests
{
    // Central European Time: clocks jump from 02:00 to 03:00 on 29 March 2026 and fall back from 03:00 to 02:00 on 25 October.
    private static readonly TimeZoneInfo Belgrade = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private static DateTimeOffset At(string time) => DateTimeOffset.Parse(time, CultureInfo.InvariantCulture);

    private static Repeat On(TimeOnly time, params DayOfWeek[] days) => Repeat.Create(PowerAction.ShutDown, time, days);

    private static Repeat Daily(TimeOnly time) => On(time, Enum.GetValues<DayOfWeek>());

    [Fact]
    public void An_occurrence_later_today_is_today() =>
        Assert.Equal(At("2026-10-01T23:30+02:00"), Daily(new TimeOnly(23, 30)).NextOccurrence(At("2026-10-01T20:00+02:00"), Belgrade));

    [Fact]
    public void An_occurrence_that_has_passed_is_the_next_day() =>
        Assert.Equal(At("2026-10-02T07:00+02:00"), Daily(new TimeOnly(7, 0)).NextOccurrence(At("2026-10-01T20:00+02:00"), Belgrade));

    [Fact]
    public void The_current_minute_is_the_next_day()
    {
        var now = At("2026-10-01T20:00+02:00");

        Assert.Equal(now.AddDays(1), Daily(new TimeOnly(20, 0)).NextOccurrence(now, Belgrade));
    }

    [Fact]
    public void The_week_wraps_to_the_chosen_day() =>
        Assert.Equal(At("2026-10-05T23:30+02:00"), On(new TimeOnly(23, 30), DayOfWeek.Monday).NextOccurrence(At("2026-10-03T09:00+02:00"), Belgrade));

    [Fact]
    public void Every_day_is_every_day() =>
        Assert.Equal(At("2026-10-03T23:30+02:00"), Daily(new TimeOnly(23, 30)).NextOccurrence(At("2026-10-03T09:00+02:00"), Belgrade));

    [Fact]
    public void A_time_skipped_by_daylight_saving_moves_on_by_the_gap() =>
        Assert.Equal(At("2026-03-29T03:30+02:00"), On(new TimeOnly(2, 30), DayOfWeek.Sunday).NextOccurrence(At("2026-03-28T23:00+01:00"), Belgrade));

    [Fact]
    public void A_time_repeated_by_daylight_saving_is_its_first_occurrence() =>
        Assert.Equal(At("2026-10-25T02:30+02:00"), On(new TimeOnly(2, 30), DayOfWeek.Sunday).NextOccurrence(At("2026-10-24T23:00+02:00"), Belgrade));

    [Fact]
    public void The_nearest_occurrence_is_the_one_just_gone()
    {
        var repeat = On(new TimeOnly(23, 30), DayOfWeek.Monday);

        Assert.Equal(At("2026-10-05T23:30+02:00"), repeat.Nearest(At("2026-10-06T00:10+02:00"), Belgrade));
        Assert.Equal(At("2026-10-05T23:30+02:00"), repeat.Nearest(At("2026-10-05T23:00+02:00"), Belgrade));
    }

    [Fact]
    public void The_trigger_days_stay_when_the_occurrence_is_later_than_the_warning() =>
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Thursday], On(new TimeOnly(23, 30), DayOfWeek.Monday, DayOfWeek.Thursday).TriggerDays(TimeSpan.FromMinutes(10)).Order());

    [Fact]
    public void The_trigger_days_move_back_when_the_occurrence_is_earlier_than_the_warning() =>
        Assert.Equal([DayOfWeek.Sunday], On(new TimeOnly(0, 5), DayOfWeek.Monday).TriggerDays(TimeSpan.FromMinutes(10)).Order());

    [Fact]
    public void A_trigger_day_on_sunday_becomes_saturday() =>
        Assert.Equal([DayOfWeek.Sunday, DayOfWeek.Saturday], On(new TimeOnly(0, 5), DayOfWeek.Sunday, DayOfWeek.Monday).TriggerDays(TimeSpan.FromMinutes(10)).Order());

    [Fact]
    public void Every_day_stays_every_day_when_the_trigger_moves_back() =>
        Assert.Equal(Enum.GetValues<DayOfWeek>().Order(), Daily(new TimeOnly(0, 0)).TriggerDays(TimeSpan.FromMinutes(10)).Order());

    [Fact]
    public void A_time_exactly_at_the_warning_doesnt_move() =>
        Assert.Equal([DayOfWeek.Monday], On(new TimeOnly(0, 10), DayOfWeek.Monday).TriggerDays(TimeSpan.FromMinutes(10)).Order());

    [Fact]
    public void The_days_read_as_words()
    {
        Assert.Equal("every day", On(new TimeOnly(23, 30), Enum.GetValues<DayOfWeek>()).DaysInWords(English));
        Assert.Equal("on weekdays", On(new TimeOnly(23, 30), [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]).DaysInWords(English));
        Assert.Equal("on weekends", On(new TimeOnly(23, 30), [DayOfWeek.Saturday, DayOfWeek.Sunday]).DaysInWords(English));
        Assert.Equal("on Mon, Wed and Fri", On(new TimeOnly(23, 30), [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]).DaysInWords(English));
        Assert.Equal("on Mon and Fri", On(new TimeOnly(23, 30), [DayOfWeek.Monday, DayOfWeek.Friday]).DaysInWords(English));
        Assert.Equal("on Wed", On(new TimeOnly(23, 30), [DayOfWeek.Wednesday]).DaysInWords(English));
    }
}
