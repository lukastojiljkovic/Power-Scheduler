using Microsoft.Win32;

namespace Pwrschdlr.Core.Tests;

public sealed class RepeatStoreTests : IDisposable
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
    public void A_saved_schedule_loads_back()
    {
        var store = new RepeatStore(_keyPath);
        var repeat = new Repeat(Guid.NewGuid(), PowerAction.Restart, new TimeOnly(7, 30), new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Friday });

        store.Save(repeat);

        var loaded = store.Load();
        Assert.Equal(repeat.Id, loaded!.Id);
        Assert.Equal(repeat.Action, loaded.Action);
        Assert.Equal(repeat.Time, loaded.Time);
        Assert.True(repeat.Days.SetEquals(loaded.Days));
    }

    [Fact]
    public void Nothing_saved_loads_as_no_schedule() => Assert.Null(new RepeatStore(_keyPath).Load());

    [Fact]
    public void Clearing_removes_the_schedule_and_can_be_repeated()
    {
        var store = new RepeatStore(_keyPath);
        store.Save(Repeat.Create(PowerAction.Sleep, new TimeOnly(23, 0), [DayOfWeek.Monday]));

        store.Clear();
        store.Clear();

        Assert.Null(store.Load());
    }

    [Theory]
    [InlineData("Id", "not a guid")]
    [InlineData("Action", 99)]
    [InlineData("Time", 24 * 60)]
    [InlineData("Days", 0)]
    [InlineData("Days", 128)]
    public void A_damaged_value_loads_as_no_schedule(string name, object value)
    {
        var store = new RepeatStore(_keyPath);
        store.Save(Repeat.Create(PowerAction.ShutDown, new TimeOnly(23, 30), [DayOfWeek.Monday]));
        using (var key = Registry.CurrentUser.CreateSubKey(_keyPath))
            key.SetValue(name, value);

        Assert.Null(store.Load());
    }
}
