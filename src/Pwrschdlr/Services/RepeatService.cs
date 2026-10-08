using System.Security.Principal;
using Pwrschdlr.Core;

namespace Pwrschdlr.Services;

/// <summary>
/// The one repeating schedule. Like the timer it lives in the registry and in a scheduled task, so it survives
/// closing the window, but it stays until it is removed: every occurrence is a separate ordinary timer.
/// </summary>
internal sealed class RepeatService
{
    public static readonly string TaskName = RepeatTask.NameFor(WindowsIdentity.GetCurrent().User!);

    private static readonly RepeatStore Store = new(AppSettings.KeyPath + @"\Repeat");

    public RepeatService() => Current = Store.Load();

    /// <summary>Saved, removed, or replaced.</summary>
    public event EventHandler? Changed;

    public static Repeat? Saved => Store.Load();

    public Repeat? Current { get; private set; }

    /// <returns><see langword="false"/> if Task Scheduler didn't accept the schedule.</returns>
    public async Task<bool> SaveAsync(Repeat repeat)
    {
        var warning = TimerService.Warning;
        var first = repeat.NextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local) - warning;
        if (await RepeatTask.RegisterAsync(TaskName, Environment.ProcessPath!, repeat, first, warning, WindowsIdentity.GetCurrent().User!) != 0)
            return false;

        Store.Save(repeat);
        Current = repeat;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public async Task RemoveAsync()
    {
        Current = null;
        Store.Clear();
        await RepeatTask.DeleteAsync(TaskName);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Moves the task when the warning changes, since the task opens the window that much earlier.</summary>
    public async Task<bool> RescheduleAsync() => Current is not { } repeat || await SaveAsync(repeat);
}
