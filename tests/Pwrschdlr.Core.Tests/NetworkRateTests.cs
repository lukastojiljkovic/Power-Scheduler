using Pwrschdlr.Core.Conditions;

namespace Pwrschdlr.Core.Tests;

public class NetworkRateTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private static NetworkSample Sample(double seconds, params (string Id, long Bytes)[] counters) =>
        new(Start.AddSeconds(seconds), counters.ToDictionary(counter => counter.Id, counter => counter.Bytes));

    [Fact]
    public void The_first_reading_has_no_rate() => Assert.Null(NetworkRate.Between(null, Sample(0, ("a", 1000))));

    [Fact]
    public void The_rate_is_what_came_in_between_the_two_readings() =>
        Assert.Equal(1500.0, NetworkRate.Between(Sample(0, ("a", 1000)), Sample(2, ("a", 4000))));

    [Fact]
    public void The_rates_of_several_interfaces_add_up() =>
        Assert.Equal(300.0, NetworkRate.Between(Sample(0, ("a", 0), ("b", 0)), Sample(1, ("a", 100), ("b", 200))));

    [Fact]
    public void An_interface_that_went_away_is_left_out() =>
        Assert.Equal(500.0, NetworkRate.Between(Sample(0, ("a", 1000), ("b", 9000)), Sample(1, ("a", 1500))));

    [Fact]
    public void An_interface_that_appeared_is_left_out() =>
        Assert.Equal(500.0, NetworkRate.Between(Sample(0, ("a", 1000)), Sample(1, ("a", 1500), ("b", 9000))));

    [Fact]
    public void A_counter_that_started_over_is_no_traffic() =>
        Assert.Equal(0.0, NetworkRate.Between(Sample(0, ("a", 5000)), Sample(1, ("a", 100))));

    [Fact]
    public void Two_readings_at_the_same_time_have_no_rate() =>
        Assert.Null(NetworkRate.Between(Sample(0, ("a", 1)), Sample(0, ("a", 2))));
}
