using Microsoft.Win32;

namespace Pwrschdlr.Core;

/// <summary>
/// Keeps the running timer under HKCU, where the window, the scheduled task's launch and the uninstaller all find it.
/// Times are QWORDs of Unix milliseconds.
/// </summary>
public sealed class ScheduleStore(string keyPath)
{
    public Schedule? Load()
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        if (key?.GetValue(nameof(Schedule.Id)) is not string id || !Guid.TryParse(id, out var guid)
            || key.GetValue(nameof(Schedule.Action)) is not int action || !Enum.IsDefined((PowerAction)action)
            || key.GetValue(nameof(Schedule.Start)) is not long start
            || key.GetValue(nameof(Schedule.Target)) is not long target)
        {
            return null;
        }

        return new Schedule(guid, (PowerAction)action, DateTimeOffset.FromUnixTimeMilliseconds(start), DateTimeOffset.FromUnixTimeMilliseconds(target));
    }

    public void Save(Schedule schedule)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        key.SetValue(nameof(Schedule.Id), schedule.Id.ToString("D"), RegistryValueKind.String);
        key.SetValue(nameof(Schedule.Action), (int)schedule.Action, RegistryValueKind.DWord);
        key.SetValue(nameof(Schedule.Start), schedule.Start.ToUnixTimeMilliseconds(), RegistryValueKind.QWord);
        key.SetValue(nameof(Schedule.Target), schedule.Target.ToUnixTimeMilliseconds(), RegistryValueKind.QWord);
    }

    public void Clear() => Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
}
