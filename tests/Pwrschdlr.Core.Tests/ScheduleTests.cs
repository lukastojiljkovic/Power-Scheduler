namespace Pwrschdlr.Core.Tests;

public class ScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 20, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void The_ring_empties_as_the_time_passes()
    {
        var schedule = Schedule.Create(PowerAction.ShutDown, Now, Now.AddHours(1));

        Assert.Equal(1, schedule.Left(Now));
        Assert.Equal(0.25, schedule.Left(Now.AddMinutes(45)), 6);
        Assert.Equal(0, schedule.Left(Now.AddHours(2)));
    }

    [Theory]
    [InlineData(-120, TimerPhase.Waiting)]
    [InlineData(-61, TimerPhase.Waiting)]
    [InlineData(-60, TimerPhase.Warning)]
    [InlineData(-1, TimerPhase.Warning)]
    [InlineData(0, TimerPhase.Up)]
    [InlineData(9, TimerPhase.Up)]
    [InlineData(10, TimerPhase.Missed)]
    [InlineData(8 * 3600, TimerPhase.Missed)]
    public void The_phase_follows_the_warning_and_a_late_end_is_missed(int secondsFromTarget, TimerPhase expected)
    {
        var schedule = Schedule.Create(PowerAction.ShutDown, Now, Now.AddHours(1));

        Assert.Equal(expected, schedule.PhaseAt(schedule.Target.AddSeconds(secondsFromTarget), TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Remaining_time_stops_at_zero()
    {
        var schedule = Schedule.Create(PowerAction.Sleep, Now, Now.AddMinutes(10));

        Assert.Equal(TimeSpan.FromMinutes(4), schedule.Remaining(Now.AddMinutes(6)));
        Assert.Equal(TimeSpan.Zero, schedule.Remaining(Now.AddMinutes(11)));
    }

    [Fact]
    public void Postponing_adds_to_what_was_left_and_fills_the_ring_again()
    {
        var schedule = Schedule.Create(PowerAction.Restart, Now, Now.AddMinutes(30));
        var later = Now.AddMinutes(29);

        var postponed = schedule.Postpone(TimeSpan.FromMinutes(15), later);

        Assert.Equal(schedule.Id, postponed.Id);
        Assert.Equal(Now.AddMinutes(45), postponed.Target);
        Assert.Equal(1, postponed.Left(later));
    }

    [Fact]
    public void Postponing_a_timer_that_ran_out_counts_from_now()
    {
        var schedule = Schedule.Create(PowerAction.Restart, Now, Now.AddMinutes(1));

        Assert.Equal(Now.AddMinutes(17), schedule.Postpone(TimeSpan.FromMinutes(15), Now.AddMinutes(2)).Target);
    }

    [Fact]
    public void Every_action_has_its_own_name_and_glyph_in_enum_order()
    {
        Assert.Equal(Enum.GetValues<PowerAction>(), Actions.All.Select(info => info.Action));
        Assert.Equal(Actions.All.Count, Actions.All.Select(info => info.Name).Distinct().Count());
        Assert.Equal(Actions.All.Count, Actions.All.Select(info => info.GlyphCode).Distinct().Count());
        Assert.All(Actions.All, info => Assert.InRange(info.GlyphCode, 0xE000, 0xF8FF));
    }

    [Fact]
    public void Only_shut_down_restart_and_sign_out_close_apps() =>
        Assert.Equal([PowerAction.ShutDown, PowerAction.Restart, PowerAction.SignOut], Actions.All.Where(info => info.ClosesApps).Select(info => info.Action));
}
