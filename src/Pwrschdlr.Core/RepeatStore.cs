using Microsoft.Win32;

namespace Pwrschdlr.Core;

/// <summary>
/// Keeps the one repeating schedule under HKCU, next to the timer, so the window and the scheduled task's launch
/// find it. The time of day is stored as minutes, and the days as a bit per <see cref="DayOfWeek"/>.
/// </summary>
public sealed class RepeatStore(string keyPath)
{
    public Repeat? Load()
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        if (key?.GetValue(nameof(Repeat.Id)) is not string id || !Guid.TryParse(id, out var guid)
            || key.GetValue(nameof(Repeat.Action)) is not int action || !Enum.IsDefined((PowerAction)action)
            || key.GetValue(nameof(Repeat.Time)) is not int minutes || minutes < 0 || minutes >= 24 * 60
            || key.GetValue(nameof(Repeat.Days)) is not int days || days == 0 || (days & ~0b1111111) != 0)
        {
            return null;
        }

        return new Repeat(guid, (PowerAction)action, new TimeOnly(minutes / 60, minutes % 60), Mask(days));
    }

    public void Save(Repeat repeat)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        key.SetValue(nameof(Repeat.Id), repeat.Id.ToString("D"), RegistryValueKind.String);
        key.SetValue(nameof(Repeat.Action), (int)repeat.Action, RegistryValueKind.DWord);
        key.SetValue(nameof(Repeat.Time), repeat.Time.Hour * 60 + repeat.Time.Minute, RegistryValueKind.DWord);
        key.SetValue(nameof(Repeat.Days), repeat.Days.Aggregate(0, (mask, day) => mask | 1 << (int)day), RegistryValueKind.DWord);
    }

    public void Clear() => Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);

    private static IReadOnlySet<DayOfWeek> Mask(int days) =>
        Enum.GetValues<DayOfWeek>().Where(day => (days & 1 << (int)day) != 0).ToHashSet();
}
