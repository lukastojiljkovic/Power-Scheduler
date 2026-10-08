using Microsoft.UI.Dispatching;
using Pwrschdlr.Core;
using Pwrschdlr.Core.Conditions;

namespace Pwrschdlr.Services;

/// <summary>
/// Watches one condition while the window is open, taking a reading a second. Pwrschdlr has no background process by
/// design, so a condition is never saved and only exists as long as the window does.
/// </summary>
internal sealed class ConditionService
{
    private readonly DispatcherQueueTimer _clock;

    public ConditionService(DispatcherQueue queue)
    {
        _clock = queue.CreateTimer();
        _clock.Interval = TimeSpan.FromSeconds(1);
        _clock.Tick += (_, _) => Sample();
    }

    /// <summary>Started, stopped, or the reading changed.</summary>
    public event EventHandler? Changed;

    public ICondition? Condition { get; private set; }

    /// <summary>What happens when the condition is met, chosen when the wait started.</summary>
    public PowerAction Action { get; private set; }

    public ConditionStatus? Status { get; private set; }

    public bool Running => Condition is not null;

    /// <summary>True once the thing being waited for has happened; the ordinary timer starts from there.</summary>
    public bool Met => Status is { Met: true };

    /// <summary>What closing the window would stop, as the close warning puts it: "your downloads finish".</summary>
    public string Clause => Condition?.Clause ?? string.Empty;

    public void Start(ICondition condition, PowerAction action)
    {
        Condition = condition;
        Action = action;
        Status = condition.Sample(DateTimeOffset.Now);
        _clock.Start();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (Condition is null)
            return;

        Condition = null;
        Status = null;
        _clock.Stop();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Sample()
    {
        if (Condition is not { } condition)
            return;

        Status = condition.Sample(DateTimeOffset.Now);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
