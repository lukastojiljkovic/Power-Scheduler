using Microsoft.Win32;

namespace Pwrschdlr.Core.Tests;

public sealed class ScheduleStoreTests : IDisposable
{
    private const string TestsKeyPath = @"Software\Pwrschdlr.Tests";

    private readonly string _keyPath = $@"{TestsKeyPath}\{Guid.NewGuid():N}";

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);
        using var parent = Registry.CurrentUser.OpenSubKey(TestsKeyPath);
        if (parent?.SubKeyCount == 0)
            Registry.CurrentUser.DeleteSubKey(TestsKeyPath, throwOnMissingSubKey: false);
    }

    [Fact]
    public void A_saved_timer_loads_back()
    {
        var store = new ScheduleStore(_keyPath);
        var schedule = new Schedule(Guid.NewGuid(), PowerAction.Hibernate,
            DateTimeOffset.FromUnixTimeMilliseconds(1_790_000_000_123), DateTimeOffset.FromUnixTimeMilliseconds(1_790_003_600_456));

        store.Save(schedule);

        Assert.Equal(schedule, store.Load());
    }

    [Fact]
    public void Nothing_saved_loads_as_no_timer() => Assert.Null(new ScheduleStore(_keyPath).Load());

    [Fact]
    public void Clearing_removes_the_timer_and_can_be_repeated()
    {
        var store = new ScheduleStore(_keyPath);
        store.Save(Schedule.Create(PowerAction.ShutDown, DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1)));

        store.Clear();
        store.Clear();

        Assert.Null(store.Load());
    }

    [Theory]
    [InlineData("Id", "not a guid")]
    [InlineData("Action", 99)]
    [InlineData("Target", "tomorrow")]
    public void A_damaged_value_loads_as_no_timer(string name, object value)
    {
        var store = new ScheduleStore(_keyPath);
        store.Save(Schedule.Create(PowerAction.ShutDown, DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1)));
        using (var key = Registry.CurrentUser.CreateSubKey(_keyPath))
            key.SetValue(name, value);

        Assert.Null(store.Load());
    }
}
