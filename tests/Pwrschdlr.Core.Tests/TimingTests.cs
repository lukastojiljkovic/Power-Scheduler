using System.Globalization;

namespace Pwrschdlr.Core.Tests;

public class TimingTests
{
    // Central European Time: clocks jump from 02:00 to 03:00 on 29 March 2026 and fall back from 03:00 to 02:00 on 25 October.
    private static readonly TimeZoneInfo Belgrade = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");

    private static DateTimeOffset At(string time) => DateTimeOffset.Parse(time, CultureInfo.InvariantCulture);

    [Fact]
    public void A_time_later_today_is_today() =>
        Assert.Equal(At("2026-10-01T23:30+02:00"), Timing.NextAt(new TimeOnly(23, 30), At("2026-10-01T20:00+02:00"), Belgrade));

    [Fact]
    public void A_time_that_has_passed_is_tomorrow() =>
        Assert.Equal(At("2026-10-02T07:00+02:00"), Timing.NextAt(new TimeOnly(7, 0), At("2026-10-01T20:00+02:00"), Belgrade));

    [Fact]
    public void The_current_minute_is_tomorrow()
    {
        var now = At("2026-10-01T20:00+02:00");

        Assert.Equal(now.AddDays(1), Timing.NextAt(new TimeOnly(20, 0), now, Belgrade));
    }

    [Fact]
    public void A_time_skipped_by_daylight_saving_moves_on_by_the_gap() =>
        Assert.Equal(At("2026-03-29T03:30+02:00"), Timing.NextAt(new TimeOnly(2, 30), At("2026-03-28T23:00+01:00"), Belgrade));

    [Fact]
    public void A_time_repeated_by_daylight_saving_is_its_first_occurrence() =>
        Assert.Equal(At("2026-10-25T02:30+02:00"), Timing.NextAt(new TimeOnly(2, 30), At("2026-10-24T23:00+02:00"), Belgrade));

    [Fact]
    public void A_day_with_a_daylight_saving_change_has_its_real_length()
    {
        // From 20:30 to 20:00 the next day is 24.5 hours when the clocks fall back in between.
        var now = At("2026-10-24T20:30+02:00");

        Assert.Equal(TimeSpan.FromHours(24.5), Timing.NextAt(new TimeOnly(20, 0), now, Belgrade) - now);
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(0.2, "0:01")]
    [InlineData(59.5, "1:00")]
    [InlineData(61, "1:01")]
    [InlineData(3599.4, "1:00:00")]
    [InlineData(3909, "1:05:09")]
    [InlineData(-3, "0:00")]
    public void The_countdown_rounds_up_and_shows_hours_only_when_there_are_some(double seconds, string expected) =>
        Assert.Equal(expected, Timing.Countdown(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData(0, "less than a minute")]
    [InlineData(20, "1 minute")]
    [InlineData(2700, "45 minutes")]
    [InlineData(3600, "1 hour")]
    [InlineData(3660, "1 hour 1 minute")]
    [InlineData(8970, "2 hours 30 minutes")]
    public void Words_round_up_to_whole_minutes(double seconds, string expected) =>
        Assert.Equal(expected, Timing.Words(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void When_names_the_day_and_uses_the_clock_format_of_the_region()
    {
        var now = At("2026-10-01T20:00+02:00");
        var serbian = CultureInfo.GetCultureInfo("sr-Latn-RS");

        Assert.Equal("today at 23:30", Timing.When(At("2026-10-01T23:30+02:00"), now, Belgrade, serbian));
        Assert.Equal("tomorrow at 7:00 AM", Timing.When(At("2026-10-02T07:00+02:00"), now, Belgrade, CultureInfo.GetCultureInfo("en-US")));
        Assert.Equal("on Saturday at 07:00", Timing.When(At("2026-10-03T07:00+02:00"), now, Belgrade, serbian));
    }

    [Fact]
    public void When_counts_days_on_the_local_clock()
    {
        // 00:30 in Belgrade is still the day before in UTC.
        var now = At("2026-10-01T23:00+02:00");

        Assert.StartsWith("tomorrow ", Timing.When(At("2026-10-02T00:30+02:00"), now, Belgrade, CultureInfo.InvariantCulture));
    }
}
