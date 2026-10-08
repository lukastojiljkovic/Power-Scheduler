namespace Pwrschdlr.Core.Tests;

public class RepeatLaunchTests
{
    private static readonly DateTimeOffset Occurrence = new(2026, 10, 5, 23, 30, 0, TimeSpan.FromHours(2));
    private static readonly TimeSpan Warning = TimeSpan.FromMinutes(1);

    [Theory]
    [InlineData(-60, RepeatDecision.Start)]
    [InlineData(-1, RepeatDecision.Start)]
    [InlineData(0, RepeatDecision.Start)]
    [InlineData(10, RepeatDecision.Start)]
    [InlineData(11, RepeatDecision.Missed)]
    [InlineData(3600, RepeatDecision.Missed)]
    public void An_occurrence_that_came_is_started_unless_it_came_too_long_ago(int seconds, RepeatDecision expected) =>
        Assert.Equal(expected, RepeatLaunch.Decide(Occurrence, Occurrence.AddSeconds(seconds), Warning, somethingRunning: false));

    [Fact]
    public void An_occurrence_before_its_warning_is_not_yet() =>
        Assert.Equal(RepeatDecision.NotYet, RepeatLaunch.Decide(Occurrence, Occurrence.AddSeconds(-61), Warning, somethingRunning: false));

    [Fact]
    public void An_occurrence_is_skipped_while_a_timer_or_a_condition_runs() =>
        Assert.Equal(RepeatDecision.SkipBusy, RepeatLaunch.Decide(Occurrence, Occurrence.AddSeconds(-30), Warning, somethingRunning: true));

    [Fact]
    public void A_schedule_just_after_midnight_starts_when_its_task_fires_the_day_before()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
        var warning = TimeSpan.FromMinutes(10);
        var repeat = Repeat.Create(PowerAction.ShutDown, new TimeOnly(0, 5), [DayOfWeek.Monday]);
        // The task fires Sunday 23:55, ten minutes before the Monday 00:05 occurrence.
        var now = new DateTimeOffset(2026, 10, 4, 23, 55, 0, TimeSpan.FromHours(2));

        var occurrence = repeat.Nearest(now, zone);

        Assert.Equal(new DateTimeOffset(2026, 10, 5, 0, 5, 0, TimeSpan.FromHours(2)), occurrence);
        Assert.Equal(RepeatDecision.Start, RepeatLaunch.Decide(occurrence, now, warning, somethingRunning: false));
    }
}
