using Pwrschdlr.Core.Conditions;

namespace Pwrschdlr.Core.Tests;

public class ConditionTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 12, 0, 0, TimeSpan.FromHours(2));

    private static DateTimeOffset At(double minutes) => Start.AddMinutes(minutes);

    private static RunningApp Steam => new(@"C:\Program Files\Steam\steam.exe", "Steam", "steam.exe");

    [Fact]
    public void Downloads_that_stay_quiet_for_the_whole_period_finish()
    {
        var network = new FakeNetwork { Rate = 0 };
        var condition = new QuietDownloadCondition(network, 100 * 1024, TimeSpan.FromMinutes(5));

        var first = condition.Sample(Start);
        Assert.False(first.Met);
        Assert.Equal("Waiting for downloads to finish", first.Waiting);
        Assert.Equal("Downloading now: nothing", first.Reading);
        Assert.Equal("Quiet for 0 of 5 minutes", first.Progress);

        Assert.False(condition.Sample(At(4.9)).Met);
        Assert.True(condition.Sample(At(5)).Met);
        Assert.Equal("Quiet for 5 of 5 minutes", condition.Sample(At(5)).Progress);
    }

    [Fact]
    public void A_download_that_comes_back_starts_the_quiet_period_over()
    {
        var network = new FakeNetwork { Rate = 0 };
        var condition = new QuietDownloadCondition(network, 100 * 1024, TimeSpan.FromMinutes(5));

        condition.Sample(Start);
        Assert.Equal("Quiet for 4 of 5 minutes", condition.Sample(At(4)).Progress);

        network.Rate = 12.4 * 1024 * 1024;
        var busy = condition.Sample(At(5));
        Assert.False(busy.Met);
        Assert.Equal("Downloading now: 12.4 MB/s", busy.Reading);
        Assert.Equal("Quiet for 0 of 5 minutes", busy.Progress);

        network.Rate = 0;
        Assert.False(condition.Sample(At(9)).Met);
        Assert.True(condition.Sample(At(14)).Met);
    }

    [Fact]
    public void The_threshold_itself_counts_as_quiet_and_a_hair_more_does_not()
    {
        var network = new FakeNetwork { Rate = 1024 };
        var condition = new QuietDownloadCondition(network, 1024, TimeSpan.FromMinutes(2));

        Assert.False(condition.Sample(Start).Met);
        Assert.True(condition.Sample(At(2)).Met);

        network.Rate = 1025;
        Assert.False(condition.Sample(At(3)).Met);
        Assert.False(condition.Sample(At(4)).Met);

        network.Rate = 1024;
        Assert.False(condition.Sample(At(5)).Met);
        Assert.True(condition.Sample(At(7)).Met);
    }

    [Fact]
    public void The_download_reading_says_what_is_coming_in() =>
        Assert.Equal("Downloading now: 512.0 KB/s", ConditionWords.DownloadReading(512 * 1024));

    [Fact]
    public void An_app_is_met_when_no_process_with_its_path_is_left()
    {
        // Installers and launchers start themselves again under the same path: two processes, one file.
        var apps = new FakeApps { Apps = [Steam, Steam] };
        var condition = new AppClosedCondition(apps, Steam);

        var both = condition.Sample(Start);
        Assert.False(both.Met);
        Assert.Equal("Waiting for Steam to close", both.Waiting);
        Assert.Equal("Steam is still running", both.Reading);
        Assert.Null(both.Progress);

        apps.Apps = [Steam];
        Assert.False(condition.Sample(At(1)).Met);

        apps.Apps = [];
        var closed = condition.Sample(At(2));
        Assert.True(closed.Met);
        Assert.Equal("Steam has closed", closed.Reading);
        Assert.Equal("Steam closes", condition.Clause);
    }

    [Fact]
    public void An_app_with_another_path_is_a_different_app()
    {
        var elsewhere = Steam with { Path = @"D:\Steam\steam.exe" };
        var condition = new AppClosedCondition(new FakeApps { Apps = [elsewhere] }, Steam);

        Assert.True(condition.Sample(Start).Met);
    }

    [Fact]
    public void Nobody_using_the_pc_is_met_after_the_chosen_time()
    {
        var input = new FakeInput { Idle = TimeSpan.FromMinutes(30) };
        var condition = new IdleInputCondition(input, TimeSpan.FromMinutes(30));

        var met = condition.Sample(Start);
        Assert.True(met.Met);
        Assert.Equal("Waiting until nobody uses the PC", met.Waiting);
        Assert.Equal("Last input 30 minutes ago", met.Reading);
        Assert.Equal("Quiet for 30 of 30 minutes", met.Progress);
        Assert.Equal("nobody uses the PC", condition.Clause);

        input.Idle = TimeSpan.FromSeconds(20);
        var early = condition.Sample(At(1));
        Assert.False(early.Met);
        Assert.Equal("Last input less than a minute ago", early.Reading);
        Assert.Equal("Quiet for 0 of 30 minutes", early.Progress);
    }

    [Fact]
    public void The_last_input_tick_count_is_read_across_the_49_day_wrap()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), InputIdle.IdleFor(tickCount: 5_000, lastInputTick: 3_000));
        // The tick count wrapped after the last input: the low 32 bits still give the idle time.
        Assert.Equal(TimeSpan.FromSeconds(9), InputIdle.IdleFor(tickCount: 4_294_967_296L + 1_000, lastInputTick: 4_294_959_296u));
    }
}
