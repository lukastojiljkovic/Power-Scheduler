namespace Pwrschdlr.Core.Tests;

public class PowerTests
{
    [Theory]
    [InlineData(PowerAction.ShutDown, false, "/s /t 0")]
    [InlineData(PowerAction.ShutDown, true, "/s /f /t 0")]
    [InlineData(PowerAction.Restart, false, "/r /t 0")]
    [InlineData(PowerAction.Restart, true, "/r /f /t 0")]
    [InlineData(PowerAction.SignOut, false, "/l")]
    [InlineData(PowerAction.SignOut, true, "/l /f")]
    [InlineData(PowerAction.Hibernate, false, "/h")]
    [InlineData(PowerAction.Hibernate, true, "/h")]
    public void Each_action_maps_to_its_shutdown_command(PowerAction action, bool closeApps, string expected) =>
        Assert.Equal(expected, Power.ShutdownArguments(action, closeApps));

    [Fact]
    public void Sleep_isnt_a_shutdown_command() => Assert.Null(Power.ShutdownArguments(PowerAction.Sleep, closeApps: true));

    [Fact]
    public void Only_sleep_and_hibernate_can_be_unavailable_and_they_say_why()
    {
        Assert.True(Power.IsAvailable(PowerAction.ShutDown));
        Assert.True(Power.IsAvailable(PowerAction.Restart));
        Assert.True(Power.IsAvailable(PowerAction.SignOut));
        foreach (var action in Enum.GetValues<PowerAction>())
            Assert.Equal(Power.IsAvailable(action), Power.WhyUnavailable(action) is null);
    }
}
