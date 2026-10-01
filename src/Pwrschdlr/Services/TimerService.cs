using System.Security.Principal;
using Microsoft.UI.Dispatching;
using Pwrschdlr.Core;

namespace Pwrschdlr.Services;

/// <summary>
/// The one timer. It lives in the registry and in a scheduled task, so it keeps running when Pwrschdlr is closed, and
/// the task opens Pwrschdlr again for the warning. While the window is open, it ticks several times a second.
/// </summary>
internal sealed class TimerService
{
    /// <summary>What <see cref="RunAsync"/> returns in Debug builds, which never shut down the PC they're tested on.</summary>
    public const int DryRun = -1;

    public static readonly string TaskName = TimerTask.NameFor(WindowsIdentity.GetCurrent().User!);

    private static readonly ScheduleStore Store = new(AppSettings.KeyPath + @"\Timer");

    private readonly DispatcherQueueTimer _clock;

    public TimerService(DispatcherQueue queue)
    {
        Current = Store.Load();
        _clock = queue.CreateTimer();
        _clock.Interval = TimeSpan.FromMilliseconds(250);
        _clock.Tick += (_, _) => Ticked?.Invoke(this, EventArgs.Empty);
        _clock.Start();
    }

    /// <summary>Started, postponed, cancelled or ended.</summary>
    public event EventHandler? Changed;

    public event EventHandler? Ticked;

    public static Schedule? Saved => Store.Load();

    public static TimeSpan Warning => TimeSpan.FromSeconds(AppSettings.WarningSeconds);

    public Schedule? Current { get; private set; }

    /// <returns><see langword="false"/> if Task Scheduler didn't accept the timer.</returns>
    public Task<bool> StartAsync(PowerAction action, DateTimeOffset target) => SetAsync(Schedule.Create(action, DateTimeOffset.Now, target));

    public Task<bool> PostponeAsync(TimeSpan by) => SetAsync(Current!.Postpone(by, DateTimeOffset.Now));

    /// <summary>Moves the task when the warning changes.</summary>
    public async Task<bool> RescheduleAsync() => Current is not { } schedule || await SetAsync(schedule);

    public async Task CancelAsync()
    {
        Current = null;
        Store.Clear();
        await TimerTask.DeleteAsync(TaskName);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ends the timer and does what it was set to do.</summary>
    /// <param name="window">The app's window, which turns off the display to sleep on Modern Standby PCs.</param>
    /// <returns>0, the Win32 error code of the failure, or <see cref="DryRun"/>.</returns>
    public async Task<int> RunAsync(PowerAction action, nint window)
    {
        await CancelAsync();
#if DEBUG
        return DryRun;
#else
        return await Power.RunAsync(action, AppSettings.CloseApps, window);
#endif
    }

    /// <summary>
    /// The task starts the warning. One whose start has passed never runs; the window, which is open then, shows the
    /// warning right away instead.
    /// </summary>
    private async Task<bool> SetAsync(Schedule schedule)
    {
        if (await TimerTask.RegisterAsync(TaskName, Environment.ProcessPath!, schedule.Id, schedule.Target - Warning, WindowsIdentity.GetCurrent().User!) != 0)
            return false;

        Store.Save(schedule);
        Current = schedule;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
